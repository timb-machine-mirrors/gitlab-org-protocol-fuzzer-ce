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
		public static List<string> __test_reply_actions    = new List<string>();
		public static bool __test_should_override_PublishToJob = false;
		public static bool __test_PublishToJob_Response = false;
		public static bool __test_use_base_StartPeach = false;
		public static bool __test_use_base_GetNodeByName = false;
		public static bool __test_use_base_SeedTheJobQueues = false;
		public static List<Heartbeat> __test_seeded_job_queues = new List<Heartbeat>();
		public static bool __test_use_base_RemoveNode = false;
		public static bool __test_use_base_UpdateNode = false;
		public static List<Heartbeat> __test_removed_nodes = new List<Heartbeat>();
		public static List<Heartbeat> __test_updated_nodes = new List<Heartbeat>();
		public static List<GenerateJobReportRequest> __test_pushed_out_reports = new List<GenerateJobReportRequest>();

		// map ip addresses (as strings) to HeartBeats (aka nodes)
		public static Dictionary<string, Heartbeat> __test_GetNodeByName_nodes = new Dictionary<string,Heartbeat>();

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

		public void callStartPeach(StartPeachRequest request, string replyQueue)
		{
			if (__test_use_base_StartPeach) base.StartPeach(request, replyQueue);
			else this.StartPeach(request, replyQueue);
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
			__test_reply_actions.Add(action);
			// NOP for now at the what/how boundary
		}

		protected override bool PublishToJob(string JobID, string RequestBody, string action)
		{
			if (__test_should_override_PublishToJob) return __test_PublishToJob_Response;
			else                                     return base.PublishToJob(JobID, RequestBody, action);
		}

		protected override Heartbeat GetNodeByName(string ipaddress, string mongoConnectionString)
		{
			if (!__test_use_base_GetNodeByName)
			{
				if (!__test_GetNodeByName_nodes.ContainsKey(ipaddress)) return null;
				else return __test_GetNodeByName_nodes[ipaddress];
			}
			else return base.GetNodeByName(ipaddress, mongoConnectionString);
		}

		protected override void DeclareJobExchange(string JobId, List<string> queueNames)
		{
			// NOP, always for now
			// base.DeclareJobExchange(JobId, queueNames);
		}

		protected override void CommitJobToMongo(StartPeachRequest request, List<Heartbeat> nodes)
		{
			// NOP always for now
			// base.CommitJobToMongo(request, nodes);
		}

		//protected override void SendToJobQueues(List<Heartbeat> nodes, StartPeachRequest request)
		//{
		//    if (!__test_use_base_SeedTheJobQueues) __test_seeded_job_queues = nodes;
		//    else base.SendToJobQueues(nodes, request);
		//}

		protected override void RemoveNode(Heartbeat heartbeat)
		{
			if (__test_use_base_RemoveNode)
			{
				base.RemoveNode(heartbeat);
			}
			else
			{
				__test_removed_nodes.Add(heartbeat);
			}
		}

		protected override void UpdateNode(Heartbeat heartbeat)
		{
			if (__test_use_base_UpdateNode)
			{
				base.UpdateNode(heartbeat);
			}
			else
			{
				__test_updated_nodes.Add(heartbeat);
			}
		}

		public void callHeartBeatReceived(Heartbeat hb)
		{
			base.HeartbeatReceived(hb);
		}

		protected override void PushReportToQueue(GenerateJobReportRequest grr)
		{
			__test_pushed_out_reports.Add(grr);
		}

		public static void ResetStaticTestingVariables()
		{
			// This is basically copied from the stuff at the top
			TestController.__test_LocalIPs = null;
			TestController.__test_node_list = null;
			TestController.__test_use_test_job = false;
			TestController.__test_job = null;
			TestController.__test_reply_queues_hit = new List<string>();
			TestController.__test_reply_bodies     = new List<string>();
			TestController.__test_reply_actions    = new List<string>();
			TestController.__test_should_override_PublishToJob = false;
			TestController.__test_PublishToJob_Response = false;
			TestController.__test_use_base_StartPeach = false;
			TestController.__test_use_base_GetNodeByName = false;
			TestController.__test_use_base_SeedTheJobQueues = false;
			TestController.__test_seeded_job_queues = new List<Heartbeat>();
			TestController.__test_use_base_RemoveNode = false;
			TestController.__test_use_base_UpdateNode = false;
			TestController.__test_removed_nodes = new List<Heartbeat>();
			TestController.__test_updated_nodes = new List<Heartbeat>();
			TestController.__test_pushed_out_reports = new List<GenerateJobReportRequest>();

			// map ip addresses (as strings) to HeartBeats (aka nodes)
			TestController.__test_GetNodeByName_nodes = new Dictionary<string,Heartbeat>();
		}
	}
}
