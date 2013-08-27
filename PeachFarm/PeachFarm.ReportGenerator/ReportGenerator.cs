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

namespace PeachFarm.Reporting
{
	public class ReportGenerator
	{
		ReportGeneratorSection config = null;
		RabbitMqHelper rabbit = null;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		public ReportGenerator()
		{
			config = (ReportGeneratorSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.reporting");
			config.Validate();

			DatabaseHelper.TestConnection(config.MongoDb.ConnectionString);

			rabbit = GetRabbitMqHelper();
			rabbit.MessageReceived += new EventHandler<RabbitMqHelper.MessageReceivedEventArgs>(rabbit_MessageReceived);
			rabbit.StartListener(QueueNames.QUEUE_REPORTGENERATOR, 1000, false, true);
		}

		protected RabbitMqHelper GetRabbitMqHelper()
		{
			return new RabbitMqHelper(config.RabbitMq.HostName, config.RabbitMq.Port, config.RabbitMq.UserName, config.RabbitMq.Password, config.RabbitMq.SSL);
		}

		public void Close()
		{
			if (rabbit.IsListening)
			{
				rabbit.StopListener(false);
			}
		}

		void rabbit_MessageReceived(object sender, RabbitMqHelper.MessageReceivedEventArgs e)
		{
			if(e.Action != Actions.GenerateReport)
				return;

			GenerateReportRequest request = GenerateReportRequest.Deserialize(e.Body);

			GenerateReportResponse response = new GenerateReportResponse();

			logger.Info("Report generator: Received report request " + request.JobID);

			try
			{
				response = GenerateReport(request);
			}
			catch (Exception ex)
			{
				response = ErrorResponse(request.JobID, ex.Message);
			}

			rabbit.PublishToQueue(e.ReplyQueue, response.Serialize(), e.Action);

			rabbit.ResumeListening();
		}

		public GenerateReportResponse GenerateReport(GenerateReportRequest request)
		{
			GenerateReportResponse response = new GenerateReportResponse();
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
				response.Status = ReportGenerationStatus.Complete;
				return response;
			}
			#endregion

			Telerik.Reporting.Processing.ReportProcessor reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
			System.Collections.Hashtable deviceInfo = new System.Collections.Hashtable();
			Telerik.Reporting.InstanceReportSource irs = new Telerik.Reporting.InstanceReportSource();

			try
			{
				irs.ReportDocument = new PeachFarm.Reporting.Reports.JobDetailReport();
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

		private void ConfigureInstanceReportSource(GenerateReportRequest request, Telerik.Reporting.InstanceReportSource irs)
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

		private GenerateReportResponse ErrorResponse(string jobid, string message)
		{
			GenerateReportResponse response = new GenerateReportResponse();
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

	}
}
