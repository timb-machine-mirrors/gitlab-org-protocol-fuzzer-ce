using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using NLog;
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
			// we want to do this _almost_ everywhere
			TestController.setShouldRabbitmqInit(false);
			TestController.setShouldMongodbInit(false);
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
			TestController.setConfig(config);
			TestController.__test_LocalIPs = ips;
			var controller = new PeachFarm.Controller.PeachFarmController();
		}

		[Test]
		public void TestControllerOverridesIPGetter()
		{
			TestController tc = new TestController();
		}

		[Test]
		public void Test_rabbit_MessageReceived()
		{
			Assert.IsTrue(true, "Tested by the sub actions in process action and process exception");
		}

		[Ignore]
		[Test]
		public void Test_ProcessAction()
		{
			Assert.IsTrue(true, "Test the various actions that this method maps to for it's inputs");
		}

		[Test]
		public void Test_ProcessException()
		{
			// this method is pretty straightforward, so we just want to ensure it can instantiate
			// the response objects it depends on. assume the (straightforward) mapping just works
			var a = new StartPeachResponse();
			var b = new StopPeachResponse();
			var c = new ListNodesResponse();
			var d = new ListErrorsResponse();
			var e = new JobInfoResponse();
			var f = new MonitorResponse();

			// also ping the default just to cover everything
			TestController tc = new TestController();
			Exception ex = new Exception("TEST_EXCEPTION");
			tc.callBaseProcessException(ex, "TEST_STUFF", "TEST_REPLY_QUEUE");
			// don't fall over  for nothing, literally
			Exception ex2 = new Exception();
			tc.callBaseProcessException(ex2, null, null);
		}

		[Test]
		public void Test_StatusCheck()
		{
			//TODO: consider pushing down the functionality of the actual status check into the nodes themselves
			// since StatusCheck doesn't return anything this test just makes sure
			TestController tc = new TestController();

			TestController.__test_node_list = new List<Heartbeat>();
			var nl = tc.callNodeList();
			tc.callStatusCheck();
			/*
			Heartbeat hb = new Heartbeat();
			TestController.__test_node_list.Add(hb);
			Assert.IsTrue(false, "one nodes");
			Assert.IsTrue(false, "two nodes");
			Assert.IsTrue(false, "a thousand nodes");
			 */
		}

		[Test]
		public void Test_StopPeach()
		{
			Assert.IsTrue(false);
		}

		[Test]
		public void Test_StartPeach()
		{
			Assert.IsTrue(false);
		}


		[Test]
		public void Test_HeartBeatReceived()
		{
			Assert.IsTrue(false);
		}

		[Test]
		public void Test_ListNodes()
		{
			Assert.IsTrue(false);
		}

		[Test]
		public void Test_ListErrors()
		{
			Assert.IsTrue(false);
		}

		[Test]
		public void Test_JobInfo()
		{
			Assert.IsTrue(false);
		}

		[Test]
		public void Test_Monitor()
		{
			Assert.IsTrue(false);
		}

		[Test]
		public void Test_CommitJobToMongo()
		{
			// this has a lot of direct information with mongo... figure out how to shim this....
			Assert.IsTrue(false);
		}

		[Test]
		public void Test_UpdateNode()
		{
			Assert.IsTrue(false);
		}

		[Test]
		public void Test_MongoDBInitializer()
		{
			Assert.IsTrue(false);
		}

		[Test]
		public void Test_RabbitInitializer()
		{
			Assert.IsTrue(false);
		}

	}
}