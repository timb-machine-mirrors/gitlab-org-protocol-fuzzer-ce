using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

using PeachFarm.Common;
using DB = PeachFarm.Common.Mongo;

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

		[SetUp]
		public void SetUp()
		{
			config = (Admin.Configuration.AdminSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.admin");
			rabbit = new RabbitMqHelper(config.RabbitMq.HostName, config.RabbitMq.Port, config.RabbitMq.UserName, config.RabbitMq.Password, config.RabbitMq.SSL);
			DB.DatabaseHelper.TestConnection(config.MongoDb.ConnectionString);

			admin = new Admin.Admin("peachfarm.admin.localhost");
			controller = new Controller.PeachFarmController("peachfarm.controller.localhost");
			while (controller.IsListening == false) { }
			node = new Node.PeachFarmNode("peachfarm.node.localhost");
			while (node.IsListening == false) { }
		}

		[TearDown]
		public void TearDown()
		{
			if(node != null)
				node.Close();

			if(controller != null)
				controller.Close();
		}

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

	}
}
