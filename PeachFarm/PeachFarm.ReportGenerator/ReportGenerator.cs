using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using PeachFarm.Reporting.Configuration;
using PeachFarm.Common.Messages;
using System.IO;
using PeachFarm.Common.Mongo;
using System.ComponentModel;
using PeachFarm.Common;
using Telerik.Reporting.Drawing;
using Telerik.Reporting.Processing;
using MySql.Data.MySqlClient;

namespace PeachFarm.Reporting
{
	public class ReportGenerator
	{
		ReportGeneratorSection config = null;
		RabbitMqHelper rabbitProcessOne = null;
		RabbitMqHelper rabbit = null;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		public ReportGenerator()
		{
			config = (ReportGeneratorSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.reporting");
			config.Validate();

			DatabaseHelper.TestConnection(config.MongoDb.ConnectionString);

			rabbitProcessOne = GetRabbitMqHelper();
			rabbitProcessOne.MessageReceived += new EventHandler<RabbitMqHelper.MessageReceivedEventArgs>(rabbitProcessOne_MessageReceived);
			rabbitProcessOne.StartListener(QueueNames.QUEUE_REPORTGENERATOR_PROCESSONE, 1000, false, true);

			rabbit = GetRabbitMqHelper();
			rabbit.MessageReceived += new EventHandler<RabbitMqHelper.MessageReceivedEventArgs>(rabbit_MessageReceived);
			rabbit.StartListener(QueueNames.QUEUE_REPORTGENERATOR, 100, false, false);
		}

		protected RabbitMqHelper GetRabbitMqHelper()
		{
			return new RabbitMqHelper(config.RabbitMq.HostName, config.RabbitMq.Port, config.RabbitMq.UserName, config.RabbitMq.Password, config.RabbitMq.SSL);
		}

		public void Close()
		{
			if (rabbitProcessOne.IsListening)
			{
				rabbitProcessOne.StopListener(false);
			}
		}

		void rabbitProcessOne_MessageReceived(object sender, RabbitMqHelper.MessageReceivedEventArgs e)
		{
			rabbit_MessageReceived(sender, e);
			rabbitProcessOne.ResumeListening();
		}

		void rabbit_MessageReceived(object sender, RabbitMqHelper.MessageReceivedEventArgs e)
		{

			ResponseBase response = null;

			try
			{
				response = ProcessAction(e.Action, e.Body);
			}
			catch (Exception ex)
			{
				//TODO
				response = ErrorResponse("", ex.Message);
			}

			if (response != null)
			{
				rabbitProcessOne.PublishToQueue(e.ReplyQueue, response.Serialize(), e.Action);
			}
		}

		private ResponseBase ProcessAction(string action, string body)
		{
			switch(action)
			{
				case Actions.GenerateJobReport:
					return GenerateJobReport(GenerateJobReportRequest.Deserialize(body));
				case Actions.NotifyJobProgress:
					LogJobProgress(JobProgressNotification.Deserialize(body));
					break;
			}

			return null;
		}

		#region GenerateJobReport
		public GenerateJobReportResponse GenerateJobReport(GenerateJobReportRequest request)
		{
			logger.Info("Report generator: Received report request " + request.JobID);

			GenerateJobReportResponse response = new GenerateJobReportResponse();
			response.JobID = request.JobID;

			#region validate
			if (String.IsNullOrEmpty(request.JobID))
			{
				return ErrorResponse(request.JobID, "Job ID is null");
			}
			var job = DatabaseHelper.GetJob(request.JobID, config.MongoDb.ConnectionString);
			if (job == null)
			{
				return ErrorResponse(request.JobID, "Job with given ID does not exist: " + (string)request.JobID);
			}
			#endregion

			#region have a stashed report
			if (String.IsNullOrEmpty(job.ReportLocation))
			{
				job.ReportLocation = Path.Combine(job.JobFolder, job.JobFolder + ".pdf");
			}
			if (DatabaseHelper.GridFSFileExists(job.ReportLocation, config.MongoDb.ConnectionString))
			{
				if (request.Reprocess)
				{
					DatabaseHelper.DeleteGridFSFile(job.ReportLocation, config.MongoDb.ConnectionString);
				}
				else
				{
					response.Status = ReportGenerationStatus.Complete;
					return response;
				}
			}
			#endregion

			Telerik.Reporting.Processing.ReportProcessor reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
			System.Collections.Hashtable deviceInfo = new System.Collections.Hashtable();
			Telerik.Reporting.InstanceReportSource irs = new Telerik.Reporting.InstanceReportSource();

			try
			{
				irs.ReportDocument = new PeachFarm.Reporting.Reports.JobDetailReport()
				{
					LogoBoxMimeType = "image/jpeg",
					LogoBoxValue = GetEmbeddedImage("dejavulogo.jpg")
				};
			}
			catch(Exception ex)
			{
				return ErrorResponse(request.JobID, "Error while loading report file: " + ex.Message);
			}

			ConfigureInstanceReportSource(request, irs);
			
			reportProcessor.Error += new Telerik.Reporting.ErrorEventHandler(reportProcessor_Error);

			logger.Info("Report Generator: Starting report generation " + job.JobID);
			RenderingResult result = null;
			try
			{
				result = reportProcessor.RenderReport("PDF", irs, deviceInfo);
			}
			catch (Exception ex)
			{
				return ErrorResponse(request.JobID, "Error while processing report. Could not render report to PDF:\n" + ex.Message);
			}

			if (result != null)
			{
				DatabaseHelper.SaveToGridFS(result.DocumentBytes, job.ReportLocation, config.MongoDb.ConnectionString);
				job.SaveToDatabase(config.MongoDb.ConnectionString);
				response.Status = ReportGenerationStatus.Complete;
				logger.Info("Report Generator: Completed report generation " + job.JobID);
			}

			return response;

		}

		private void ConfigureInstanceReportSource(GenerateJobReportRequest request, Telerik.Reporting.InstanceReportSource irs)
		{
			Unit margin = new Unit(0.5, UnitType.Inch);
			irs.ReportDocument.PageSettings = new PageSettings();
			irs.ReportDocument.PageSettings.Margins.Bottom = margin;
			irs.ReportDocument.PageSettings.Margins.Left = margin;
			irs.ReportDocument.PageSettings.Margins.Right = margin;
			irs.ReportDocument.PageSettings.Margins.Top = margin;


			irs.Parameters.Add("connectionString", config.MongoDb.ConnectionString);
			irs.Parameters.Add("jobID", request.JobID);
			irs.Parameters.Add("hostURL", config.Monitor.BaseURL);
		}

		private GenerateJobReportResponse ErrorResponse(string jobid, string message)
		{
			GenerateJobReportResponse response = new GenerateJobReportResponse();
			response.JobID = jobid;
			response.ErrorMessage = message;
			response.Success = false;
			response.Status = ReportGenerationStatus.Error;
			logger.Error("Report Generator: Error\n" + message);
			return response;
		}

		void reportProcessor_Error(object sender, Telerik.Reporting.ErrorEventArgs eventArgs)
		{
			eventArgs.Cancel = false;
		}
		#endregion

		#region LogJobProgress
		private void LogJobProgress(JobProgressNotification notification)
		{
			var mongoJob = DatabaseHelper.GetJob(notification.JobID, config.MongoDb.ConnectionString);

			MySqlCommand cmd;

			using (var conn = new MySqlConnection(config.MySql.ConnectionString))
			{
				conn.Open();

				cmd = conn.CreateCommand();
				cmd.Connection = conn;
				cmd.CommandText = "jobs_insert";
				cmd.CommandType = System.Data.CommandType.StoredProcedure;
				cmd.Parameters.AddWithValue("jobid", notification.JobID);
				cmd.Parameters.AddWithValue("target", mongoJob.Target);
				cmd.Parameters.AddWithValue("startdate", mongoJob.StartDate);
				cmd.Parameters.AddWithValue("mongoid", mongoJob.ID);
				cmd.Parameters.AddWithValue("pitfilename", mongoJob.Pit.FileName);
				cmd.Parameters.Add("rowid", MySqlDbType.UInt32);
				cmd.Parameters["rowid"].Direction = System.Data.ParameterDirection.Output;

				cmd.ExecuteNonQuery();
				var mysqljobid = (uint)cmd.Parameters["rowid"].Value;

				var transaction = conn.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
				try
				{
					foreach (var i in notification.IterationMetrics)
					{
						cmd = conn.CreateCommand();
						cmd.Connection = conn;
						cmd.Transaction = transaction;
						cmd.CommandText = "metrics_iterations_insert";
						cmd.Parameters.AddWithValue("jobs_id", mysqljobid);
						cmd.Parameters.AddWithValue("state", i.State);
						cmd.Parameters.AddWithValue("actionname", i.Action);
						cmd.Parameters.AddWithValue("parameter", i.Parameter);
						cmd.Parameters.AddWithValue("dataelement", i.DataElement);
						cmd.Parameters.AddWithValue("mutator", i.Mutator);
						cmd.Parameters.AddWithValue("dataset", i.DataSet);
						cmd.Parameters.AddWithValue("iterationcount", i.IterationCount);
						cmd.CommandType = System.Data.CommandType.StoredProcedure;
						cmd.ExecuteNonQuery();
					}

					foreach (var f in notification.FaultMetrics)
					{
						cmd = conn.CreateCommand();
						cmd.Connection = conn;
						cmd.Transaction = transaction;
						cmd.CommandText = "metrics_faults_insert";
						cmd.Parameters.AddWithValue("jobs_id", mysqljobid);
						cmd.Parameters.AddWithValue("bucket", f.Bucket);
						cmd.Parameters.AddWithValue("iteration", f.Iteration);
						cmd.Parameters.AddWithValue("state", f.State);
						cmd.Parameters.AddWithValue("actionname", f.Action);
						cmd.Parameters.AddWithValue("dataelement", f.DataElement);
						cmd.Parameters.AddWithValue("mutator", f.Mutator);
						cmd.Parameters.AddWithValue("dataset", f.DataSet);
						cmd.Parameters.AddWithValue("parameter", f.Parameter);
						cmd.Parameters.AddWithValue("datamodel", f.DataModel);
						cmd.Parameters.AddWithValue("mongoid", f.MongoID);

						cmd.CommandType = System.Data.CommandType.StoredProcedure;
						cmd.ExecuteNonQuery();
					}

					foreach (var s in notification.StateMetrics)
					{
						cmd = conn.CreateCommand();
						cmd.Connection = conn;
						cmd.Transaction = transaction;
						cmd.CommandText = "metrics_states_insert";
						cmd.CommandType = System.Data.CommandType.StoredProcedure;
						cmd.Parameters.AddWithValue("jobs_id", mysqljobid);
						cmd.Parameters.AddWithValue("state", s.State);
						cmd.Parameters.AddWithValue("executioncount", s.ExecutionCount);
						cmd.ExecuteNonQuery();
					}
					transaction.Commit();
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex.ToString());
					transaction.Rollback();
				}

				conn.Close();
			}
		}
		#endregion

		private System.Drawing.Bitmap GetEmbeddedImage(string p)
		{
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			var file = assembly.GetManifestResourceStream("PeachFarm.Reporting.Reports." + p);
			if (file == null)
			{
				return null;
			}
			else
			{
				return new System.Drawing.Bitmap(file);
			}
		}

	}
}
