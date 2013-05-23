using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using PeachFarm.Common.Mongo;
using PeachFarm.ReportGenerator;
using PeachFarm.Common.Messages;

namespace PeachFarm.Test
{
	[TestFixture]
	public class ReportGeneratorTests
	{
		#region setup
		private static PeachFarm.ReportGenerator.Configuration.ReportGeneratorSection config;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			config = (ReportGenerator.Configuration.ReportGeneratorSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.reportgenerator");
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
		public void Test()
		{
			var rg = new PeachFarm.ReportGenerator.ReportGenerator();
			GenerateReportRequest request = new GenerateReportRequest();
			request.JobID = "D1AD3C7CE24A";
			request.ReportFormat = ReportFormat.PDF;

			rg.GenerateReportCompleted += (o, e) =>
			{
				Assert.IsTrue(e.Result.Success);
				Assert.AreEqual(ReportGenerationStatus.Complete, e.Result.Status);
				var job = DatabaseHelper.GetJob(request.JobID, config.MongoDb.ConnectionString);
				Assert.IsTrue(DatabaseHelper.GridFSFileExists(job.ReportLocation, config.MongoDb.ConnectionString));
			};

			rg.GenerateReport(request);
		}
	}
}
