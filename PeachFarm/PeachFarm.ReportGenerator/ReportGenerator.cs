using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PeachFarm.ReportGenerator.Configuration;
using PeachFarm.Common.Messages;
using System.IO;
using PeachFarm.Common.Mongo;
using System.ComponentModel;

namespace PeachFarm.ReportGenerator
{
	public class ReportGenerator
	{
		ReportGeneratorSection config = null;

		public ReportGenerator()
		{
			config = (ReportGeneratorSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.reportgenerator");
			if (config.Monitor.BaseURL.EndsWith("/") == false)
			{
				config.Monitor.BaseURL = config.Monitor.BaseURL + "/";
			}
		}

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




		//public void GenerateReport(GenerateReportRequest request)
		//{
		//  BackgroundWorker worker = new BackgroundWorker();
		//  worker.DoWork += new DoWorkEventHandler(worker_DoWork);

		//}

		//void worker_DoWork(object sender, DoWorkEventArgs e)
		//{
		//  GenerateReportWork((GenerateReportRequest)e.Argument);
		//}

		public void GenerateReport(GenerateReportRequest request)
		{
			GenerateReportResponse response = new GenerateReportResponse();
			response.JobID = request.JobID;

			var job = DatabaseHelper.GetJob(request.JobID, config.MongoDb.ConnectionString);
			if (job == null)
			{
				response.Status = ReportGenerationStatus.Error;
				response.ErrorMessage = "Job ID does not exist: " + request.JobID;
				response.Success = false;
				OnGenerateReportCompleted(response);
				return;
			}

			if (String.IsNullOrEmpty(job.ReportLocation))
			{
				job.ReportLocation = Path.Combine(job.JobFolder, job.JobFolder + ".pdf");
			}

			if (DatabaseHelper.GridFSFileExists(job.ReportLocation, config.MongoDb.ConnectionString))
			{
				response.Status = ReportGenerationStatus.Complete;
				OnGenerateReportCompleted(response);
				return;
			}

			Telerik.Reporting.Processing.ReportProcessor reportProcessor = new Telerik.Reporting.Processing.ReportProcessor();

			System.Collections.Hashtable deviceInfo = new System.Collections.Hashtable();

			Telerik.Reporting.InstanceReportSource irs = new Telerik.Reporting.InstanceReportSource();

			irs.ReportDocument = new JobDetailReport();

			irs.Parameters.Add("connectionString", config.MongoDb.ConnectionString);
			irs.Parameters.Add("jobID", request.JobID);
			irs.Parameters.Add("hostURL", config.Monitor.BaseURL);

			//string documentName = String.Empty;
			//bool success = reportProcessor.RenderReport("PDF", irs, deviceInfo, new Telerik.Reporting.Processing.CreateStream(CreateStream), out documentName);
			var result = reportProcessor.RenderReport("PDF", irs, deviceInfo);
			DatabaseHelper.SaveToGridFS(result.DocumentBytes, job.ReportLocation, config.MongoDb.ConnectionString);
			job.SaveToDatabase(config.MongoDb.ConnectionString);
			response.Status = ReportGenerationStatus.Complete;
			OnGenerateReportCompleted(response);

		}

		private Stream CreateStream(string name, string extension, Encoding encoding, string mimeType)
		{
			return DatabaseHelper.GetGridFSStream(name, config.MongoDb.ConnectionString);
		}
	}
}
