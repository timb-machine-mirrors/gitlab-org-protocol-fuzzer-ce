using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PeachFarm.Common;

namespace PeachFarm.Test
{
	class TestRabbitMqHelper : RabbitMqHelper
	{

		public static bool __test_shouldCallBase_OpenConnection = false;

		public TestRabbitMqHelper(string hostName, int port = -1, string userName = "guest", string password = "guest", bool ssl = false)
			: base(hostName, port, userName, password, ssl)
		{
			// don't call the base constructor
		}

		public override void PublishToExchange(string exchangeName, string body, string action)
		{
		}

		protected override void OpenConnection()
		{
			// calling the base method here requires the actual machinery of the rabbitmq
			// server to be available. do not want during testing
			if(__test_shouldCallBase_OpenConnection) base.OpenConnection();
		}
	}
}
