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
		private static RabbitMqHelper rabbit;
		private static Admin.Configuration.AdminSection config;

		private static Admin.Admin admin;
		private static Controller.PeachFarmController controller;
		private static Node.PeachFarmNode node;
		private const string testName = "localhost";

		#region setup
		
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			config = (Admin.Configuration.AdminSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.admin");
			rabbit = new RabbitMqHelper(config.RabbitMq.HostName, config.RabbitMq.Port, config.RabbitMq.UserName, config.RabbitMq.Password, config.RabbitMq.SSL);
			DatabaseHelper.TestConnection(config.MongoDb.ConnectionString);

			controller = new Controller.PeachFarmController(testName);
			while (controller.IsListening == false) { }
			node = new Node.PeachFarmNode(testName);
			while (node.IsListening == false) { }
			Debug.WriteLine("Waiting for node to register with controller...");
			WaitForNode(30);
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
			admin = new Admin.Admin(testName);
		}

		[TearDown]
		public void TearDown()
		{
			admin = null;
			DatabaseHelper.TruncateAllCollections(config.MongoDb.ConnectionString);
			Sleep(2000);
		}
		#endregion

		#region tests
		[Test]
		public void ListNodes()
		{
			bool done = false;
			PeachFarm.Admin.Admin.ListNodesCompletedEventHandler handler = (o, e) =>
			{
				Assert.AreEqual(1, e.Result.Nodes.Count);
				done = true;
			};

			admin.ListNodesCompleted += handler;
			admin.ListNodesAsync();
			while (!done)	{	}
			admin.ListNodesCompleted -= handler;
		}

		[Test]
		public void HelloWorld()
		{
			bool done = false;
			string jobid = String.Empty;
			PeachFarm.Admin.Admin.StartPeachCompletedEventHandler startHandler = (o, e) =>
			{
				Assert.AreEqual(true, e.Result.Success);
				jobid = e.Result.JobID;
				Assert.IsNotNullOrEmpty(jobid);
				done = true;
			};
			admin.StartPeachCompleted += startHandler;
			admin.StartPeachAsync("HelloWorld.xml", null, 1, null, null);
			while (!done) { }
			admin.StartPeachCompleted -= startHandler;

			while (WaitForNode().Status == Common.Messages.Status.Running)
			{
				Sleep(1000);
			}
			Assert.AreEqual(Common.Messages.Status.Alive, WaitForNode().Status);
			Assert.AreEqual(0, DatabaseHelper.GetAllErrors(config.MongoDb.ConnectionString).Count);
		}
		#endregion

		#region private functions
		private void Sleep(int milliseconds)
		{
			System.Threading.Thread.Sleep(milliseconds);
		}

		private PeachFarm.Common.Messages.Heartbeat WaitForNode(int tries = 0)
		{
			bool limittries = (tries != 0);
			List<PeachFarm.Common.Messages.Heartbeat> nodes = null;
			while ((nodes = DatabaseHelper.GetAllNodes(config.MongoDb.ConnectionString)).Count == 0)
			{
				if (limittries)
				{
					if (tries > 0)
						tries--;
					else
						throw new TimeoutException("WaitForNode tries exceeded.");
				}
				Sleep(1000);
			}
			return nodes[0];
		}
		#endregion
	}
}
