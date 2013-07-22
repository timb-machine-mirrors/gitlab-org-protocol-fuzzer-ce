﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PeachFarm.Reporting.Configuration;
using PeachFarm.Common.Messages;
using System.IO;
using PeachFarm.Common.Mongo;
using System.ComponentModel;
using PeachFarm.Common;
using Telerik.Reporting.Processing;

namespace PeachFarm.Reporting
{
	public class ReportGenerator
	{
		ReportGeneratorSection config = null;
		RabbitMqHelper rabbit = null;

		public ReportGenerator()
		{
			config = (ReportGeneratorSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.reporting");
			if (config.Monitor.BaseURL.EndsWith("/") == false)
			{
				config.Monitor.BaseURL = config.Monitor.BaseURL + "/";
			}

			DatabaseHelper.TestConnection(config.MongoDb.ConnectionString);

			rabbit = new RabbitMqHelper(config.RabbitMq.HostName, config.RabbitMq.Port, config.RabbitMq.UserName, config.RabbitMq.Password, config.RabbitMq.SSL);


			rabbit.MessageReceived += new EventHandler<RabbitMqHelper.MessageReceivedEventArgs>(rabbit_MessageReceived);
			rabbit.StartListener(QueueNames.QUEUE_REPORTGENERATOR);
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

			rabbit.StopListener(false);

			GenerateReportRequest request = GenerateReportRequest.Deserialize(e.Body);

			GenerateReportResponse response = new GenerateReportResponse();

			try
			{
				response = GenerateReport(request);
			}
			catch (Exception ex)
			{
				response.Success = false;
				response.ErrorMessage = ex.Message;
				response.Status = ReportGenerationStatus.Error;
				response.JobID = request.JobID;
			}

			rabbit.PublishToQueue(e.ReplyQueue, response.Serialize(), e.Action);

			rabbit.StartListener(QueueNames.QUEUE_REPORTGENERATOR);
		}

		#region GenerateReportCompleted
		/*
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
		//*/
		#endregion


		public GenerateReportResponse GenerateReport(GenerateReportRequest request)
		{
			GenerateReportResponse response = new GenerateReportResponse();

			if (String.IsNullOrEmpty(request.JobID))
			{
				response.Status = ReportGenerationStatus.Error;
				response.ErrorMessage = "Job ID is null";
				response.Success = false;
				return response;
			}

			response.JobID = request.JobID;

			var job = DatabaseHelper.GetJob(request.JobID, config.MongoDb.ConnectionString);
			if (job == null)
			{
				response.Status = ReportGenerationStatus.Error;
				response.ErrorMessage = "Job ID does not exist: " + request.JobID;
				response.Success = false;
				return response;
			}

			if (String.IsNullOrEmpty(job.ReportLocation))
			{
				job.ReportLocation = Path.Combine(job.JobFolder, job.JobFolder + ".pdf");
			}

			if (DatabaseHelper.GridFSFileExists(job.ReportLocation, config.MongoDb.ConnectionString))
			{
				response.Status = ReportGenerationStatus.Complete;
				return response;
			}

			Telerik.Reporting.Processing.ReportProcessor reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();

			System.Collections.Hashtable deviceInfo = new System.Collections.Hashtable();

			Telerik.Reporting.InstanceReportSource irs = new Telerik.Reporting.InstanceReportSource();

			try
			{
				irs.ReportDocument = new PeachFarm.Reporting.Reports.JobDetailReport();
			}
			catch(Exception ex)
			{
				return ReturnError(request.JobID, "Error while loading report file: " + ex.Message);
			}

			irs.Parameters.Add("connectionString", config.MongoDb.ConnectionString);
			irs.Parameters.Add("jobID", request.JobID);
			
			//irs.Parameters.Add("hostURL", config.Monitor.BaseURL);
			reportProcessor.Error += new Telerik.Reporting.ErrorEventHandler(reportProcessor_Error);
			//string documentName = String.Empty;
			//bool success = reportProcessor.RenderReport("PDF", irs, deviceInfo, new Telerik.Reporting.Processing.CreateStream(CreateStream), out documentName);
			RenderingResult result = null;
			try
			{
				result = reportProcessor.RenderReport("PDF", irs, deviceInfo);
			}
			catch (Exception ex)
			{
				response.Status = ReportGenerationStatus.Error;
				response.ErrorMessage = "Error while processing report:\n" + ex.Message;
				response.Success = false;
				return response;
			}

			if (result != null)
			{
				DatabaseHelper.SaveToGridFS(result.DocumentBytes, job.ReportLocation, config.MongoDb.ConnectionString);
				job.SaveToDatabase(config.MongoDb.ConnectionString);
				response.Status = ReportGenerationStatus.Complete;
			}

			return response;

		}

		private GenerateReportResponse ReturnError(string jobid, string message)
		{
			GenerateReportResponse response = new GenerateReportResponse();
			response.JobID = jobid;
			response.ErrorMessage = message;
			response.Success = false;
			response.Status = ReportGenerationStatus.Error;
			return response;
		}

		void reportProcessor_Error(object sender, Telerik.Reporting.ErrorEventArgs eventArgs)
		{
			eventArgs.Cancel = false;
		}
	}
}
