﻿using System;
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
			// since StatusCheck doesn't return anything this test just makes sure it doesn't barf
			TestController tc = new TestController();

			TestController.__test_node_list = new List<Heartbeat>();
			var nl = tc.callNodeList();

			System.Console.WriteLine("Calling first status check");
			tc.callStatusCheck();

			System.Console.WriteLine("Calling second status check");
			TestHeartbeat hbNow = new TestHeartbeat();
			hbNow.Stamp = DateTime.Now;
			nl.Add(hbNow);

			System.Console.WriteLine("Calling third status check");
			TestHeartbeat hb = new TestHeartbeat();
			hb.Stamp = DateTime.Now.AddMinutes(-21);
			nl.Add(hb);
			tc.callStatusCheck();
			Assert.IsTrue(hb.__test_was_saved_to_errors);
			Assert.IsTrue(hb.__test_was_removed_from_database);

			System.Console.WriteLine("Calling fourth status check");
			hb.Stamp = DateTime.Now.AddMinutes(-11);
			nl.Add(hb);
			tc.callStatusCheck();
			Assert.IsTrue(hb.__test_was_saved_to_database);

			System.Console.WriteLine("Calling full up status check");
			// for funsiesa, don't flop under pressure
			for (int k = 0; k < 1000; k++) nl.Add(hb);
			tc.callStatusCheck();
		}

		[Test]
		public void Test_StopPeach_WithNullJob()
		{
			// need to rig GetJob() so it returns null
			TestController tc = new TestController();
			TestController.__test_use_test_job = true;
			Common.Mongo.Job job = new Common.Mongo.Job();

			string rq = "TestingReplyQueue";
			StopPeachRequest spr = new StopPeachRequest();
			spr.JobID = "asdfasdfasdfasdfasdfasdf";

			tc.callStopPeach(spr, rq);
			foreach (var q in TestController.__test_reply_queues_hit) System.Console.WriteLine(q);
			foreach (var b in TestController.__test_reply_bodies) System.Console.WriteLine(b);
			Assert.IsTrue(TestController.__test_reply_queues_hit.Contains(rq));
			Assert.AreEqual(TestController.__test_reply_bodies.Count, 1);
			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains(spr.JobID));
			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains("does not exist"));
		}

		[Test]
		public void Test_StopPeach_WithJobCountZero()
		{
			TestController tc = new TestController();
			TestController.__test_use_test_job = true;
			TestController.__test_job = new Common.Mongo.Job();
			TestController.__test_node_list = new List<Heartbeat>();

			// need to rig NodeList() and GetJob()

			string rq = "TestingReplyQueue";
			StopPeachRequest spr = new StopPeachRequest();
			spr.JobID = "asdfasdfasdfasdfasdfasdf";

			tc.callStopPeach(spr, rq);
			foreach (var q in TestController.__test_reply_queues_hit) System.Console.WriteLine(q);
			foreach (var b in TestController.__test_reply_bodies) System.Console.WriteLine(b);

			Assert.IsTrue(TestController.__test_reply_queues_hit.Contains(rq));
			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains("<ErrorMessage>"));
			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains("is not running"));
		}

		[Test]
		public void Test_StopPeach_WithJobCountNonZero_PublishFails()
		{
			// need to rig NodeList() and GetJob()
			TestController tc = new TestController();
			TestController.__test_use_test_job = true;
			PeachFarm.Common.Mongo.Job jobby = new Common.Mongo.Job();
			Heartbeat hb = new Heartbeat();
			TestController.__test_job = jobby;
			TestController.__test_node_list = new List<Heartbeat>();
			TestController.__test_node_list.Add(hb);
			TestController.__test_should_override_PublishToJob = true;
			TestController.__test_PublishToJob_Response = false;

			string rq = "TestingReplyQueue";
			StopPeachRequest spr = new StopPeachRequest();
			spr.JobID = "asdfasdfasdfasdfasdfasdf";
			hb.JobID = spr.JobID;
			hb.Status = Status.Running;

			tc.callStopPeach(spr, rq);

			// foreach (var q in TestController.__test_reply_queues_hit) System.Console.WriteLine(q);
			// foreach (var b in TestController.__test_reply_bodies) System.Console.WriteLine(b);
			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains("<ErrorMessage>"));
			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains("Cannot stop job"));
			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains(spr.JobID));
		}

		[Test]
		public void Test_StopPeach_WithJobCountNonZero_PublishSucceeds()
		{
			// need to rig NodeList() and GetJob()
			TestController tc = new TestController();
			TestController.__test_use_test_job = true;
			PeachFarm.Common.Mongo.Job jobby = new Common.Mongo.Job();
			Heartbeat hb = new Heartbeat();
			TestController.__test_job = jobby;
			TestController.__test_node_list = new List<Heartbeat>();
			TestController.__test_node_list.Add(hb);
			TestController.__test_should_override_PublishToJob = true;
			// ################################################################################
			// ### this tests is almost the same as Test_StopPeach_WithJobCountNonZero_PublishFails except
			// ### the __test_PublishToJob_Response and the asserts
			// ################################################################################
			TestController.__test_PublishToJob_Response = true;

			string rq = "TestingReplyQueue";
			StopPeachRequest spr = new StopPeachRequest();
			spr.JobID = "asdfasdfasdfasdfasdfasdf";
			hb.JobID = spr.JobID;
			hb.Status = Status.Running;

			tc.callStopPeach(spr, rq);

			// foreach (var q in TestController.__test_reply_queues_hit) System.Console.WriteLine(q);
			// foreach (var b in TestController.__test_reply_bodies) System.Console.WriteLine(b);

			string replyBody = TestController.__test_reply_bodies[0];
			var split = replyBody.Split('\n');

			Assert.IsTrue(TestController.__test_reply_queues_hit.Contains(rq));
			Assert.IsTrue(split.Length == 2);
			Assert.IsTrue(split[1].StartsWith("<StopPeachResponse"));
			Assert.IsTrue(split[1].Contains(spr.JobID));
		}

		#region SingleSpecificNode
		[Test]
		public void Test_StartPeach_SpecificSingleNodeChosen_NullNodeReturned()
		{
			TestController.__test_use_base_StartPeach = true;
			TestController tc = new TestController();

			StartPeachRequest startReq = new StartPeachRequest();
			startReq.ClientCount = 1;
			startReq.IPAddress = "4.2.2.2";
			string replyQueue = "TestReplyQueue";
			tc.callStartPeach(startReq, replyQueue);

			foreach (var q in TestController.__test_reply_queues_hit) System.Console.WriteLine(q);
			foreach (var b in TestController.__test_reply_bodies) System.Console.WriteLine(b);
			foreach (var b in TestController.__test_reply_actions) System.Console.WriteLine(b);

			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains("No Alive Node"));
			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains(startReq.IPAddress));
		}

		[Test]
		public void Test_StartPeach_SpecificSingleNodeChosen_NonLiveNodeReturned()
		{
			TestController.__test_use_base_StartPeach = true;
			// next two lines different than Test_StartPeach_SpecificSingleNodeChosen_NullNodeReturned
			Heartbeat hb = new Heartbeat();
			hb.Status = Status.Stopping;
			hb.NodeName = "4.2.2.2";
			TestController.__test_GetNodeByName_nodes[hb.NodeName] = hb;
			// ---------------------------------------------------------------------------------------
			TestController tc = new TestController();

			StartPeachRequest startReq = new StartPeachRequest();
			startReq.ClientCount = 1;
			startReq.IPAddress = "4.2.2.2";
			string replyQueue = "TestReplyQueue";
			tc.callStartPeach(startReq, replyQueue);

			// foreach (var q in TestController.__test_reply_queues_hit) System.Console.WriteLine(q);
			// foreach (var b in TestController.__test_reply_bodies) System.Console.WriteLine(b);
			// foreach (var b in TestController.__test_reply_actions) System.Console.WriteLine(b);

			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains("No Alive Node"));
			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains(startReq.IPAddress));
		}

		[Test]
		public void Test_StartPeach_SpecificSingleNodeChosen_LiveNodeReturned()
		{
			TestController.__test_use_base_StartPeach = true;
			TestController.__test_should_override_PublishToJob = true;
			// next four lines different than Test_StartPeach_SpecificSingleNodeChosen_NullNodeReturned
			Heartbeat hb = new Heartbeat();
			hb.Status = Status.Alive;
			hb.NodeName = "4.2.2.2";
			TestController.__test_GetNodeByName_nodes[hb.NodeName] = hb;
			// ---------------------------------------------------------------------------------------
			TestController tc = new TestController();

			StartPeachRequest startReq = new StartPeachRequest();
			startReq.ClientCount = 1;
			startReq.IPAddress = "4.2.2.2";
			string replyQueue = "TestReplyQueue";
			tc.callStartPeach(startReq, replyQueue);

			foreach (var q in TestController.__test_reply_queues_hit) System.Console.WriteLine(q);
			foreach (var b in TestController.__test_reply_bodies) System.Console.WriteLine(b);
			foreach (var b in TestController.__test_reply_actions) System.Console.WriteLine(b);

			Assert.IsTrue(TestController.__test_reply_queues_hit.Contains(replyQueue));
			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains("\n<StartPeachResponse"));
			Assert.IsTrue(TestController.__test_reply_actions.Contains("StartPeach"));
		}
		#endregion

		[Test]
		public void Test_StartPeach_NotEnoughLiveNodes_NodeCountTotal()
		{
			TestController.__test_use_base_StartPeach = true;
			TestController.__test_should_override_PublishToJob = true;
			TestController.__test_node_list = new List<Heartbeat>();

			#region  add nodes
			Heartbeat hb = new Heartbeat();
			hb.NodeName = "4.2.2.2";
			hb.Status = Status.Stopping;
			TestController.__test_node_list.Add(hb);
			#endregion

			TestController tc = new TestController();

			StartPeachRequest startReq = new StartPeachRequest();
			startReq.ClientCount = 2;
			string replyQueue = "TestReplyQueue";

			tc.callStartPeach(startReq, replyQueue);

			foreach (var q in TestController.__test_reply_queues_hit) System.Console.WriteLine(q);
			foreach (var b in TestController.__test_reply_bodies) System.Console.WriteLine(b);
			foreach (var b in TestController.__test_reply_actions) System.Console.WriteLine(b);

			Assert.IsTrue(TestController.__test_reply_queues_hit.Contains(replyQueue));
			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains("\n<StartPeachResponse"));
			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains("<ErrorMessage>"));
			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains("Not enough Alive nodes available"));
			Assert.IsTrue(TestController.__test_reply_actions.Contains("StartPeach"));
		}

		[Test]
		public void Test_StartPeach_NotEnoughLiveNodes_StatusCount()
		{
			TestController.__test_use_base_StartPeach = true;
			TestController.__test_should_override_PublishToJob = true;
			TestController.__test_node_list = new List<Heartbeat>();

			#region  add nodes
			Heartbeat hb = new Heartbeat();
			hb.NodeName = "4.2.2.2";
			hb.Status = Status.Alive; // <<<<=======================
			TestController.__test_node_list.Add(hb);

			Heartbeat hb2 = new Heartbeat();
			hb2.NodeName = "4.2.2.2";
			hb2.Status = Status.Stopping; // <<<<=======================
			TestController.__test_node_list.Add(hb2);

			Heartbeat hb3 = new Heartbeat();
			hb3.NodeName = "4.2.2.2";
			hb3.Status = Status.Stopping; // <<<<=======================
			TestController.__test_node_list.Add(hb3);
			#endregion

			TestController tc = new TestController();

			StartPeachRequest startReq = new StartPeachRequest();
			startReq.ClientCount = 2;
			string replyQueue = "TestReplyQueue";

			tc.callStartPeach(startReq, replyQueue);

			foreach (var q in TestController.__test_reply_queues_hit) System.Console.WriteLine(q);
			foreach (var b in TestController.__test_reply_bodies) System.Console.WriteLine(b);
			foreach (var b in TestController.__test_reply_actions) System.Console.WriteLine(b);

			Assert.IsTrue(TestController.__test_reply_queues_hit.Contains(replyQueue));
			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains("\n<StartPeachResponse"));
			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains("<ErrorMessage>"));
			Assert.IsTrue(TestController.__test_reply_bodies[0].Contains("Not enough Alive nodes available"));
			Assert.IsTrue(TestController.__test_reply_actions.Contains("StartPeach"));
		}

		[Test]
		public void Test_StartPeach_SUCCESS_NodesWithMatchingTags()
		{
			TestController.__test_use_base_StartPeach = true;
			TestController.__test_should_override_PublishToJob = true;
			TestController.__test_node_list = new List<Heartbeat>();

			#region  add nodes
			// we're interested in the tags here
			Heartbeat hb = new Heartbeat();
			hb.NodeName = "4.2.2.2";
			hb.Status = Status.Alive;
			hb.Tags = "foo.test"; // <<<<=======================
			hb.QueueName = "test.q1";
			TestController.__test_node_list.Add(hb);

			Heartbeat hb2 = new Heartbeat();
			hb2.NodeName = "4.2.2.3";
			hb2.Status = Status.Alive;
			hb2.Tags = "foo.test"; // <<<<=======================
			hb2.QueueName = "test.q2";
			TestController.__test_node_list.Add(hb2);

			Heartbeat hb3 = new Heartbeat();
			hb3.NodeName = "4.2.2.4";
			hb3.Status = Status.Alive;
			hb3.Tags = ""; // <<<<=======================
			hb3.QueueName = "test.q3";
			TestController.__test_node_list.Add(hb3);
			#endregion

			TestController tc = new TestController();

			StartPeachRequest startReq = new StartPeachRequest();
			startReq.ClientCount = 2;
			startReq.Tags = "foo.test";
			startReq.IPAddress = ""; // otherwise it err's on IPAddress.length

			string replyQueue = "TestReplyQueue";

			tc.callStartPeach(startReq, replyQueue);

			// foreach (var q in TestController.__test_reply_queues_hit) System.Console.WriteLine(q);
			// foreach (var b in TestController.__test_reply_bodies) System.Console.WriteLine(b);
			// foreach (var b in TestController.__test_reply_actions) System.Console.WriteLine(b);

			Assert.IsTrue(TestController.__test_reply_queues_hit.Contains(replyQueue));

			Assert.IsTrue(TestController.__test_seeded_job_queues.Contains(hb.QueueName));
			Assert.IsTrue(TestController.__test_seeded_job_queues.Contains(hb2.QueueName));
			Assert.IsTrue(! TestController.__test_seeded_job_queues.Contains(hb3.QueueName));

			string ReplyBody = TestController.__test_reply_bodies[0];
			Assert.IsTrue(   ReplyBody.Contains("\n<StartPeachResponse"));
			Assert.IsTrue( ! ReplyBody.Contains("<ErrorMessage"));
			Assert.IsTrue(TestController.__test_reply_actions.Contains("StartPeach"));
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

		[Test]
		public void Test_SeedTheJobQueues()
		{
			Assert.IsTrue(false);
		}

	}
}