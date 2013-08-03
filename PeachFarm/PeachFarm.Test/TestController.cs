using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

using PeachFarm.Controller;
using PeachFarm.Common.Messages;

namespace PeachFarm.Test
{
	class TestController : PeachFarmController
	{

		#region staticTestModifiers
		// fields then methods
		public static IPAddress[] __test_LocalIPs = null;
		public static List<Heartbeat> __test_node_list = null;

		public static void setShouldRabbitmqInit(bool should)
		{
			PeachFarmController.__test_should_rabbitmq_init = should;
		}

		public static void setShouldMongodbInit(bool should)
		{
			PeachFarmController.__test_should_mongodb_init = should;
		}

		public static void setConfig(PeachFarm.Controller.Configuration.ControllerSection config)
		{
			PeachFarmController.__test_config = config;
		}
		#endregion

		public TestController() : base()
		{
		}

		public void callBaseProcessException(Exception ex, string action, string replyQueue)
		{
			base.ProcessException(ex, action, replyQueue);
		}

		public void callStatusCheck()
		{
			Object foo = new Object();
			base.StatusCheck(foo);
		}
		public List<Heartbeat> callNodeList()
		{
			return NodeList(new PeachFarm.Controller.Configuration.ControllerSection());
		}

		public NLog.Logger getLogger() { return PeachFarmController.logger; }

		protected override IPAddress[] LocalIPs()
		{
			if (__test_LocalIPs != null) return __test_LocalIPs;
			else                         return base.LocalIPs();
		}

		protected override List<Common.Messages.Heartbeat> NodeList(Controller.Configuration.ControllerSection config)
		{
			if (TestController.__test_node_list != null) return TestController.__test_node_list; 
			else                                         return base.NodeList(config);
		}
	}
}
