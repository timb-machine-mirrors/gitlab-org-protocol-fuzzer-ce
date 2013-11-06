﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using PeachFarm.Common.Mongo;
using PeachFarm.Reporting;
using PeachFarm.Common.Messages;

namespace PeachFarm.Test
{
	[TestFixture]
	public class ReportGeneratorTests
	{
		#region setup
		private static PeachFarm.Reporting.Configuration.ReportGeneratorSection config;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			config = (Reporting.Configuration.ReportGeneratorSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.reporting");
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
			var rg = new PeachFarm.Reporting.ReportGenerator();
			GenerateJobReportRequest request = new GenerateJobReportRequest();
			request.JobID = "9CB41D020E39";
			request.ReportFormat = ReportFormat.PDF;

			var response = rg.GenerateJobReport(request);

			Assert.IsTrue(response.Success);
		}
	}
}
