using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;

using PeachFarm.Common.Mongo;
using PeachFarm.ReportGenerator;
using PeachFarm.ReportGenerator.Configuration;
namespace PeachFarm.Test
{
	[TestFixture]
	class ReportGeneratorTests
	{
		#region setup
		ReportGeneratorSection config = null;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			config = (ReportGeneratorSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.reportgenerator");
			DatabaseHelper.TestConnection(config.MongoDb.ConnectionString);
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
		}

		[SetUp]
		public void SetUp()
		{
		}

		[TearDown]
		public void TearDown()
		{
		}
		#endregion


		[Test]
		public void GenerateReport()
		{
			var rg = new PeachFarm.ReportGenerator.ReportGenerator();

			var request = new PeachFarm.Common.Messages.GenerateReportRequest();
			request.JobID = "AAA3C3DFD952";
			request.ReportFormat = Common.Messages.ReportFormat.PDF;

			rg.GenerateReportCompleted += (o, e) =>
			{
				Assert.IsTrue(e.Result.Success);
				var job = DatabaseHelper.GetJob(request.JobID, config.MongoDb.ConnectionString);
				Debug.WriteLine(job.ReportLocation);
				//DatabaseHelper.DownloadFromGridFS(job.ReportLocation, job.ReportLocation, config.MongoDb.ConnectionString);
				Assert.IsTrue(DatabaseHelper.GridFSFileExists(job.ReportLocation, config.MongoDb.ConnectionString));
			};

			rg.GenerateReport(request);
		}
	}
}
