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

		public static bool __test_use_test_job = false;
		public static Common.Mongo.Job __test_job = null;
		public static List<string> __test_reply_queues_hit = new List<string>();
		public static List<string> __test_reply_bodies     = new List<string>();
		public static bool __test_should_override_PublishToJob = false;
		public static bool __test_PublishToJob_Response = false;

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

		public static void resetReplyInfo()
		{
			__test_reply_bodies = new List<string>();
			__test_reply_queues_hit = new List<string>();
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

		public void callStopPeach(StopPeachRequest request, string replyQueue)
		{
			// wrapper around the protected StopPeach()
			this.StopPeach(request, replyQueue);
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

		protected override Common.Mongo.Job GetJob(StopPeachRequest request)
		{
			// this may have to work with a dictionary mapping of id's to
			// jobs later for multiple jobs
			if (__test_use_test_job) return __test_job;
			else                    return base.GetJob(request);
		}

		protected override void Reply(string body, string action, string replyQueue)
		{
			__test_reply_queues_hit.Add(replyQueue);
			__test_reply_bodies.Add(body);
			// NOP for now at the what/how boundary
		}

		protected override bool PublishToJob(string JobID, string RequestBody, string action)
		{
			if (__test_should_override_PublishToJob) return __test_PublishToJob_Response;
			else                                     return base.PublishToJob(JobID, RequestBody, action);
		}
	}
}
