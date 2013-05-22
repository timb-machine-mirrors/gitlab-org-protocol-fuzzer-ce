using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using PeachFarm.Common;
using PeachFarm.Common.Mongo;
using System.Collections;

namespace PeachFarm.ReportGenerator
{
	public class ReportGenerator
	{
		private RabbitMqHelper rabbit;
		private Configuration.ReportGeneratorSection config;
		private string reportGeneratorQueueName;
		private string controllerQueueName;

		public ReportGenerator(string reportGeneratorName = "")
		{
			config = (Configuration.ReportGeneratorSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.reportgenerator");
			
			System.Net.IPAddress[] ipaddresses = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
			string ipaddress = (from i in ipaddresses where i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork select i).First().ToString();

			if (Common.Mongo.DatabaseHelper.TestConnection(config.MongoDb.ConnectionString) == false)
			{
				string error = String.Format("No connection can be made to MongoDB at:\n{0}", config.MongoDb.ConnectionString);
				throw new ApplicationException(error);
			}

			//rabbit = new RabbitMqHelper(config.RabbitMq.HostName, config.RabbitMq.Port, config.RabbitMq.UserName, config.RabbitMq.Password, config.RabbitMq.SSL);
			if (String.IsNullOrEmpty(reportGeneratorName))
			{
				this.reportGeneratorQueueName = String.Format(QueueNames.QUEUE_REPORTGENERATOR, ipaddress);
			}
			else
			{
				this.reportGeneratorQueueName = String.Format(QueueNames.QUEUE_REPORTGENERATOR, reportGeneratorName);
			}

			controllerQueueName = String.Format(QueueNames.QUEUE_CONTROLLER, config.Controller.IpAddress);

			//rabbit.MessageReceived += new EventHandler<RabbitMqHelper.MessageReceivedEventArgs>(rabbit_MessageReceived);
			//rabbit.StartListener(this.reportGeneratorQueueName);

		}

		public Status Status { get; private set; }

		#region GenerateReportCompleted
		public event EventHandler<GenerateReportCompletedEventArgs> GenerateReportCompleted;

		private void OnGenerateReportCompleted(PeachFarm.Common.Messages.GenerateReportResponse result)
		{
			if (GenerateReportCompleted != null)
			{
				GenerateReportCompleted(this, new GenerateReportCompletedEventArgs(result));
			}
		}

		public class GenerateReportCompletedEventArgs : EventArgs
		{
			public GenerateReportCompletedEventArgs(PeachFarm.Common.Messages.GenerateReportResponse result)
			{
				this.Result = result;
			}

			public PeachFarm.Common.Messages.GenerateReportResponse Result { get; private set; }
		}
		#endregion




		public void Close()
		{

		}

		private void rabbit_MessageReceived(object sender, RabbitMqHelper.MessageReceivedEventArgs e)
		{
			rabbit.StopListener(false);

			try
			{
				ProcessAction(e.Action, e.Body, e.ReplyQueue);
			}
			catch (Exception ex)
			{
				ProcessException(ex, e.Action, e.ReplyQueue);
			}
		}

		private void ProcessAction(string action, string body, string replyQueue)
		{
			switch (action)
			{
				case "GenerateReport":
					GenerateReport(Common.Messages.GenerateReportRequest.Deserialize(body));
					break;
				default:
					//TODO
					break;
			}
		}

		private void ProcessException(Exception ex, string action, string replyQueue)
		{
			//throw new NotImplementedException();
		}

		public void GenerateReport(Common.Messages.GenerateReportRequest request)
		{
			PeachFarm.Common.Messages.GenerateReportResponse response = new Common.Messages.GenerateReportResponse();
			response.JobID = request.JobID;

			var job = Common.Mongo.DatabaseHelper.GetJob(request.JobID, config.MongoDb.ConnectionString);

			if(job == null)
			{
				throw new ApplicationException("Job not found: " + request.JobID);
			}

			if(String.IsNullOrEmpty(job.ReportLocation))
			{
				job.ReportLocation = System.IO.Path.Combine(job.JobFolder, job.JobFolder + ".pdf");
			}


			if(DatabaseHelper.GridFSFileExists(job.ReportLocation, config.MongoDb.ConnectionString))
			{
				response.Status = Common.Messages.ReportGenerationStatus.Complete;
				OnGenerateReportCompleted(response);
				return;
			}

			Hashtable deviceInfoSettings = new Hashtable();
			switch(request.ReportFormat)
			{
				case Common.Messages.ReportFormat.PDF:
					deviceInfoSettings.Add("FontEmbedding", "None");
					deviceInfoSettings.Add("JavaScript", "");
					deviceInfoSettings.Add("StartPage", 0);
					deviceInfoSettings.Add("EndPage", 0);
					deviceInfoSettings.Add("DpiX", 300);
					deviceInfoSettings.Add("DpiY", 300);
					deviceInfoSettings.Add("DocumentTitle", job.JobFolder);
					deviceInfoSettings.Add("DocumentKeywords", job.JobID);
					break;
				default:
					throw new ApplicationException(String.Format("Report Format {0} not supported", request.ReportFormat.ToString()));
			}

			this.Status = PeachFarm.ReportGenerator.Status.Running;

			//BackgroundWorker worker = new BackgroundWorker();
			//worker.DoWork += new DoWorkEventHandler(worker_DoWork);
			//worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
			//worker.RunWorkerAsync(request);

			string format = request.ReportFormat.ToString();

			Telerik.Reporting.Processing.ReportProcessor reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
			Telerik.Reporting.InstanceReportSource instanceReportSource = new Telerik.Reporting.InstanceReportSource();

			instanceReportSource.ReportDocument = new PeachFarm.ReportGenerator.JobDetailReport();
			instanceReportSource.Parameters.Add("connectionString", config.MongoDb.ConnectionString);

			instanceReportSource.Parameters.Add("jobID", request.JobID);

			string hostURL = "http://dejaapps/pfmonitor/";
			hostURL = hostURL.Substring(0, hostURL.LastIndexOf('/') + 1);
			instanceReportSource.Parameters.Add("hostURL", hostURL);

			Telerik.Reporting.Processing.RenderingResult result = null;
			try
			{
				result = reportProcessor.RenderReport(format, instanceReportSource, deviceInfoSettings);
			}
			catch (Exception ex)
			{
				response.Success = false;
				response.ErrorMessage = ex.ToString();
				response.Status = Common.Messages.ReportGenerationStatus.Error;
				OnGenerateReportCompleted(response);
				return;
			}

			Common.Mongo.DatabaseHelper.SaveToGridFS(result.DocumentBytes, job.ReportLocation, config.MongoDb.ConnectionString);

			job.SaveToDatabase(config.MongoDb.ConnectionString);

			response.Status = Common.Messages.ReportGenerationStatus.Complete;
			OnGenerateReportCompleted(response);
		}

		void worker_DoWork(object sender, DoWorkEventArgs e)
		{
			var request = e.Argument as PeachFarm.Common.Messages.GenerateReportRequest;
			var job = Common.Mongo.DatabaseHelper.GetJob(request.JobID, config.MongoDb.ConnectionString);

			string format = request.ReportFormat.ToString();

			Telerik.Reporting.Processing.ReportProcessor reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();
			Telerik.Reporting.InstanceReportSource instanceReportSource = new Telerik.Reporting.InstanceReportSource();

			instanceReportSource.ReportDocument = new PeachFarm.ReportGenerator.JobDetailReport();
			instanceReportSource.Parameters.Add("connectionString", config.MongoDb.ConnectionString);

			instanceReportSource.Parameters.Add("jobID", request.JobID);

			string hostURL = "http://dejaapps/pfmonitor/";
			hostURL = hostURL.Substring(0, hostURL.LastIndexOf('/') + 1);
			instanceReportSource.Parameters.Add("hostURL", hostURL);

			var result = reportProcessor.RenderReport(format, instanceReportSource, new System.Collections.Hashtable());

			string remoteFileName = System.IO.Path.Combine(job.JobFolder, job.JobFolder + ".pdf");

			Common.Mongo.DatabaseHelper.SaveToGridFS(result.DocumentBytes, remoteFileName, config.MongoDb.ConnectionString);

			job.ReportLocation = remoteFileName;
			job.SaveToDatabase(config.MongoDb.ConnectionString);
		}

		void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			var response = new Common.Messages.GenerateReportResponse();
			if (e.Error == null)
			{
				response.Status = Common.Messages.ReportGenerationStatus.Complete;
			}
			else
			{
				response.Success = false;
				response.ErrorMessage = e.Error.ToString();
			}

			OnGenerateReportCompleted(response);
			
			//rabbit.PublishToQueue(this.controllerQueueName, response.Serialize(), "GenerateReport");

			//this.Status = PeachFarm.ReportGenerator.Status.Alive;
			//rabbit.StartListener(this.reportGeneratorQueueName);
		}

	}

	public enum Status
	{
		Alive,
		Running,
		Stopping
	}
}
