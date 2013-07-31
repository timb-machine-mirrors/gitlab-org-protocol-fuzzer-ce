using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using PeachFarm.Common.Mongo;
using PeachFarm.Reporting;
using PeachFarm.Common.Messages;
using PeachFarm.Controller;

namespace PeachFarm.Test
{
	[TestFixture]
	public class ControllerTests
	{
		#region setup
		private static PeachFarm.Reporting.Configuration.ReportGeneratorSection config;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			
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
		public void RabbitMQConfigTakesSecondIPv4()
		{
			// if the controller instantiates everything is fine
			IPAddress[] ips = {IPAddress.Parse("192.0.2.2"), IPAddress.Parse("192.0.2.3")};
			PeachFarm.Controller.Configuration.ControllerSection config = new Controller.Configuration.ControllerSection();
			config.MongoDb.ConnectionString = "http://localhost:27017";
			config.RabbitMq.HostName = "192.0.2.3";
			PeachFarmController.__test_config = config;
			PeachFarm.Controller.PeachFarmController.__test_LocalIPs = ips;
			PeachFarm.Controller.PeachFarmController.__test_should_mongodb_init  = false;
			PeachFarm.Controller.PeachFarmController.__test_should_rabbitmq_init = false;
			var controller = new PeachFarm.Controller.PeachFarmController();
		}
	}
}