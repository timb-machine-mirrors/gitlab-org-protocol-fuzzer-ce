#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

using PeachFarm.Common;
using PeachFarm.Common.Mongo;
using System.Diagnostics;

namespace PeachFarm.Test
{
	[TestFixture]
	public class IntegrationTests
	{
		private static RabbitMqHelper rabbit { get; set; }
		private static Admin.Configuration.AdminSection config;

		private static Admin.PeachFarmAdmin admin;
		private static Controller.PeachFarmController controller;
		private static Node.PeachFarmNode node;
		private const string testName = "localhost";

		private const string MongoDbConnectionString = "";

		#region setup
		
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			

			config = (Admin.Configuration.AdminSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.admin");
			rabbit = new RabbitMqHelper(config.RabbitMq.HostName, config.RabbitMq.Port, config.RabbitMq.UserName, config.RabbitMq.Password, config.RabbitMq.SSL);
			DatabaseHelper.TestConnection(MongoDbConnectionString);

			DatabaseHelper.TruncateAllCollections(MongoDbConnectionString);

			controller = new Controller.PeachFarmController();
			while (controller.IsListening == false) { }
			node = new Node.PeachFarmNode();
			while (node.IsListening == false) { }
			Debug.WriteLine("Waiting for node to register with controller...");
			WaitForNode(200);
			Debug.WriteLine("Node online");
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
			if(node != null)
				node.Close();

			if(controller != null)
				controller.Close();

		}

		[SetUp]
		public void SetUp()
		{
			admin = new Admin.PeachFarmAdmin(testName);
		}

		[TearDown]
		public void TearDown()
		{
			admin = null;
			DatabaseHelper.TruncateAllCollections(MongoDbConnectionString);
			Sleep(2000);
		}
		#endregion

		#region tests
		[Test]
		public void ListNodes()
		{
			//bool done = false;
			//PeachFarm.Admin.PeachFarmAdmin.ListNodesCompletedEventHandler handler = (o, e) =>
			//{
			//  Assert.AreEqual(1, e.Result.Nodes.Count);
			//  done = true;
			//};

			//admin.ListNodesCompleted += handler;
			//admin.ListNodesAsync();
			//while (!done)	{	}
			//admin.ListNodesCompleted -= handler;
		}

		[Test]
		public void HelloWorld()
		{
			bool done = false;
			string jobid = String.Empty;
			PeachFarm.Admin.PeachFarmAdmin.StartPeachCompletedEventHandler startHandler = (o, e) =>
			{
				Assert.AreEqual(true, e.Result.Success);
				jobid = e.Result.JobID;
				Assert.IsNotNullOrEmpty(jobid);
				done = true;
			};
			admin.StartPeachCompleted += startHandler;
			admin.StartPeachAsync("HelloWorld.xml", null, 1, null, null, null);
			while (!done) { }
			admin.StartPeachCompleted -= startHandler;

			WaitForNode(100, n => n.Status == Common.Messages.Status.Running);
			WaitForNode(100, n => n.Status == Common.Messages.Status.Alive);

			Assert.AreEqual(Common.Messages.Status.Alive, node.Status);
			Assert.AreEqual(0, DatabaseHelper.GetAllErrors(MongoDbConnectionString).Count);
		}


		public void DebuggerWindows()
		{
			bool done = false;
			string jobid = String.Empty;
			PeachFarm.Admin.PeachFarmAdmin.StartPeachCompletedEventHandler startHandler = (o, e) =>
			{
				Assert.AreEqual(true, e.Result.Success);
				jobid = e.Result.JobID;
				Assert.IsNotNullOrEmpty(jobid);
				done = true;
			};
			admin.StartPeachCompleted += startHandler;
			admin.StartPeachAsync("DebuggerWindows.xml", null, 1, null, null, null);
			while (!done) { }
			admin.StartPeachCompleted -= startHandler;

			WaitForNode(30, n => n.Status == Common.Messages.Status.Running);

			done = false;
			PeachFarm.Admin.PeachFarmAdmin.StopPeachCompletedEventHandler stopHandler = (o, e) =>
			{
				Assert.AreEqual(true, e.Result.Success);
				done = true;
			};
			admin.StopPeachCompleted += stopHandler;
			admin.StopPeachAsync(jobid);
			while (!done) { }
			admin.StopPeachCompleted -= stopHandler;

			WaitForNode(30, n => n.Status == Common.Messages.Status.Alive);

			var job = DatabaseHelper.GetJob(jobid, MongoDbConnectionString);
			job.FillNodes(MongoDbConnectionString);
			Assert.AreEqual(1, job.Nodes.Count);
			Assert.Less(0, job.Nodes[0].IterationCount);
			Assert.AreEqual(0, DatabaseHelper.GetAllErrors(MongoDbConnectionString).Count);
		}
		#endregion

		#region private functions
		private void Sleep(int milliseconds)
		{
			System.Threading.Thread.Sleep(milliseconds);
		}

		private PeachFarm.Common.Messages.Heartbeat WaitForNode(int tries = 0, Func<PeachFarm.Common.Messages.Heartbeat, bool> condition = null)
		{
			bool limittries = (tries > 0);
			PeachFarm.Common.Messages.Heartbeat node = null;
			if(condition == null)
			{
				node = DatabaseHelper.GetAllNodes(MongoDbConnectionString).FirstOrDefault();
			}
			else
			{
				node = DatabaseHelper.GetAllNodes(MongoDbConnectionString).FirstOrDefault(condition);
			}
			while (node == null)
			{
				if (limittries)
				{
					if (tries > 0)
						tries--;
					else
						throw new TimeoutException("WaitForNode tries exceeded.");
				}
				Sleep(100);
				if (condition == null)
				{
					node = DatabaseHelper.GetAllNodes(MongoDbConnectionString).FirstOrDefault();
				}
				else
				{
					node = DatabaseHelper.GetAllNodes(MongoDbConnectionString).FirstOrDefault(condition);
				}
			}
			return node;
		}
		#endregion
	}
}
#endif