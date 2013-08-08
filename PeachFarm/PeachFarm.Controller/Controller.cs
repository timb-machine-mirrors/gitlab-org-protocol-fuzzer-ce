<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PeachFarm.Common.Messages;
using System.Net;
using PeachFarm.Common;
using System.Threading;
using System.ComponentModel;
using NLog;
using System.Diagnostics;
using System.Security.Cryptography;
using PeachFarm.Common.Mongo;
using System.Xml.Linq;
 
namespace PeachFarm.Controller
{
	public class PeachFarmController
	{

		private UTF8Encoding encoding = new UTF8Encoding();
		private static string ipaddress;
		protected Configuration.ControllerSection config;

		protected static Logger logger = LogManager.GetCurrentClassLogger();

		private static Timer statusCheck = null;

		RabbitMqHelper rabbit = null;

		private string controllerQueueName;

		#region testing
		// these are here for testing purposes and shouldn't be mucked with in prod
		// everything _should_ function normally if left alone
		protected static bool __test_should_rabbitmq_init  = true;
		protected static bool __test_should_mongodb_init = true;
		protected static Configuration.ControllerSection __test_config = null;
=======
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PeachFarm.Common.Messages;
using System.Net;
using PeachFarm.Common;
using System.Threading;
using System.ComponentModel;
using NLog;
using System.Diagnostics;
using System.Security.Cryptography;
using PeachFarm.Common.Mongo;
using System.Xml.Linq;
 
namespace PeachFarm.Controller
{
	public class PeachFarmController
	{

		private UTF8Encoding encoding = new UTF8Encoding();
		private static string ipaddress;
		private Configuration.ControllerSection config;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		private static Timer statusCheck = null;

		RabbitMqHelper rabbit = null;

		private string controllerQueueName;

		#region testing
		// these are here for testing purposes and shouldn't be mucked with in prod
		// everything _should_ function normally if left alone
		public static bool __test_should_rabbitmq_init  = true;
		public static bool __test_should_mongodb_init = true;
		public static Configuration.ControllerSection __test_config = null;
		public static IPAddress[] __test_LocalIPs = null;
>>>>>>> 7e22574bd6bccee96fac271956c4d4f531abdbf0
		//-------------------------------------------------------------------------
		#endregion testing

		public PeachFarmController()
		{
			// Startup as application
			IPAddress[] ipaddresses = LocalIPs();
			ipaddress = (from i in ipaddresses where i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork select i).First().ToString();

			this.config = this.ControllerConfigSection();
			this.config.Validate();

			RabbitInitializer();
<<<<<<< HEAD
			MongoDBInitializer();

			if (statusCheck == null)
			{
				statusCheck = new Timer(new TimerCallback(StatusCheck), null, TimeSpan.FromMilliseconds(0), TimeSpan.FromMinutes(1));
			}
		}

		void rabbit_MessageReceived(object sender, RabbitMqHelper.MessageReceivedEventArgs e)
		{
			try
			{
				ProcessAction(e.Action, e.Body, e.ReplyQueue);
			}
			catch (Exception ex)
			{
				ProcessException(ex, e.Action, e.ReplyQueue);
			}
		}

		#region Properties
		public bool IsListening
		{
			get { return rabbit.IsListening; }
		}

		public string QueueName
		{
			get
			{
				return controllerQueueName;
			}
		}
		#endregion

		public void Close()
		{
			rabbit.StopListener();
			//rabbit.CloseConnection();
		}

		protected void StatusCheck(object state)
		{
			//Catching all exceptions because StatusCheck is called often
			List<Heartbeat> nodes = NodeList(config);

			try
			{
				foreach (var node in nodes)
				{
					if (node.Stamp.AddMinutes(20) < DateTime.Now)
					{
						logger.Warn("{0}\t{1}\t{2}", node.NodeName, "Node Expired", node.Stamp);
						node.RemoveFromDatabase(config.MongoDb.ConnectionString);
						node.ErrorMessage = "Node Expired";
						node.Stamp = DateTime.Now;
						node.SaveToErrors(config.MongoDb.ConnectionString);
					}
					else
					{
						if (node.Stamp.AddMinutes(10) < DateTime.Now)
						{
							node.Status = Status.Late;
							logger.Info("{0}\t{1}\t{2}", node.NodeName, node.Status.ToString(), node.Stamp);
							node.SaveToDatabase(config.MongoDb.ConnectionString);
						}
					}
				}
			}
			catch { }
		}

		#region MQ functions
		private void PublishToClients(string body, string action)
		{
			rabbit.PublishToExchange(QueueNames.EXCHANGE_NODE, body, action);
			logger.Trace("Sent Action to Clients: {0}\nBody:\n{1}", action, body);
		}

		protected virtual bool PublishToJob(string jobID, string body, string action)
		{
			bool result = true;
			string exchangename = String.Format(QueueNames.EXCHANGE_JOB, jobID);

			try
			{
				rabbit.PublishToExchange(exchangename, body, action);
				logger.Trace("Sent Action to Job {0}: {1}\nBody:\n{2}", jobID, action, body);
			}
			catch (RabbitMqException rex)
			{
				logger.Error("Failed to publish to job: " + rex.Message);
				result = false;
			}

			return result;
		}

		protected virtual void DeclareJobExchange(string jobID, List<string> queueNames)
		{
			string exchangeName = String.Format(QueueNames.EXCHANGE_JOB, jobID);
			rabbit.DeclareExchange(exchangeName, queueNames, jobID);
		}

		private void DeleteJobExchange(string jobID)
		{
			string exchangeName = String.Format(QueueNames.EXCHANGE_JOB, jobID);
			rabbit.DeleteExchange(exchangeName);
		}

		protected virtual void Reply(string body, string action, string replyQueue)
		{
			rabbit.PublishToQueue(replyQueue, body, action);
			logger.Trace("Sent Action to {2}: {0}\nBody:\n{1}", action, body, replyQueue);
		}

		private void ProcessAction(string action, string body, string replyQueue)
		{
			Debug.WriteLine("action: " + action);
			Debug.WriteLine("reply: " + replyQueue);
			Debug.WriteLine(body);
			switch (action)
			{
				case Actions.StartPeach:
					StartPeach(StartPeachRequest.Deserialize(body), replyQueue);
					break;
				case Actions.StopPeach:
					StopPeach(StopPeachRequest.Deserialize(body), replyQueue);
					break;
				case Actions.Heartbeat:
					HeartbeatReceived(Heartbeat.Deserialize(body));
					break;
				case Actions.ListNodes:
					ListNodes(ListNodesRequest.Deserialize(body), replyQueue);
					break;
				case Actions.ListErrors:
					ListErrors(ListErrorsRequest.Deserialize(body), replyQueue);
					break;
				case Actions.JobInfo:
					JobInfo(JobInfoRequest.Deserialize(body), replyQueue);
					break;
				case Actions.Monitor:
					Monitor(MonitorRequest.Deserialize(body), replyQueue);
					break;
				case Actions.GenerateReport:
					GenerateReportComplete(GenerateReportResponse.Deserialize(body), replyQueue);
					break;
				default:
					string error = String.Format("Received unknown action {0}", action);
					logger.Error(error);
					break;
			}
		}


		protected void ProcessException(Exception ex, string action, string replyQueue)
		{
			ResponseBase response = null;
			switch (action)
			{
				case Actions.StartPeach:
					response = new StartPeachResponse();
					break;
				case Actions.StopPeach:
					response = new StopPeachResponse();
					break;
				case Actions.ListNodes:
					response = new ListNodesResponse();
					break;
				case Actions.ListErrors:
					response = new ListErrorsResponse();
					break;
				case Actions.JobInfo:
					response = new JobInfoResponse();
					break;
				case Actions.Monitor:
					response = new MonitorResponse();
					break;
				default:
					string error = String.Format("Error while processing {0} message from {1}:\n{2}", action, replyQueue, ex.Message);
					logger.Error(error);
					return;
			}

			if (response != null)
			{
				response.Success = false;
				response.ErrorMessage = ex.Message;
				Reply(response.ToString(), action, replyQueue);
			}
		}

		#endregion

		#region Receives
		protected void StopPeach(StopPeachRequest request, string replyQueue)
		{
			StopPeachResponse response = new StopPeachResponse(request);

			var job = GetJob(request);
			if (job == null)
			{
				response.Success = false;
				response.ErrorMessage = String.Format("Job {0} does not exist.", request.JobID);
			}
			else
			{
				var nodes = NodeList(config);
				var jobnodes = (from n in nodes where n.Status == Status.Running && n.JobID == request.JobID select n).Count();
				if (jobnodes == 0)
				{
					response.Success = false;
					response.ErrorMessage = String.Format("Job {0} is not running.", request.JobID);
				}
				else
				{
					bool result = PublishToJob(request.JobID, request.Serialize(), "StopPeach");

					if (result == false)
					{
						response.Success = false;
						response.ErrorMessage = String.Format("Cannot stop job {0}", request.JobID);
					}
				}
			}
			Reply(response.Serialize(), Actions.StopPeach, replyQueue);
		}

		protected virtual Common.Mongo.Job GetJob(StopPeachRequest request)
		{
			return Common.Mongo.DatabaseHelper.GetJob(request.JobID, config.MongoDb.ConnectionString);
		}

		protected virtual void StartPeach(StartPeachRequest request, string replyQueue)
		{
			string action = Actions.StartPeach;
			request.MongoDbConnectionString = config.MongoDb.ConnectionString;
			StartPeachResponse response = new StartPeachResponse(request);

			#region SingleSpecificNodeChosen
			// is a single specific node chosen?
			if ((request.ClientCount == 1) && (request.IPAddress.Length > 0))
			{
				var node = GetNodeByName(request.IPAddress, config.MongoDb.ConnectionString);

				if ((node != null) && (node.Status == Status.Alive))
				{
					// run job with this node's IP on 1 box (command line start with --clientCount=1)
					DeclareJobExchange(request.JobID, new List<string>() { node.QueueName });
					CommitJobToMongo(request, new List<Heartbeat>(){ node });
					PublishToJob(request.JobID, request.Serialize(), action);
					Reply(response.Serialize(), action, replyQueue);
				}
				else
				{
					// null node returned, OR node is not alive
					response.JobID = String.Empty;
					response.Success = false;
					response.ErrorMessage = String.Format("No Alive Node running at IP address {0}\n", request.IPAddress);
					Reply(response.Serialize(), action, replyQueue);
				}
			}
			#endregion
			else
			{
				// either both ipaddress and clientcount not given, or clientcount nonzero w/ no ips
				var nodes = NodeList(config);
				var jobNodes = new List<Heartbeat>();

				#region DoOrDontCareAboutTags
				if (String.IsNullOrEmpty(request.Tags))
				{
					// grab all nodes
					jobNodes = (from Heartbeat h in nodes.OrderByDescending(h => h.Stamp) where h.Status == Status.Alive select h).ToList();
				}
				else
				{
					// grab all nodes that match the tags given
					var aliveNodes = (from Heartbeat h in nodes.OrderByDescending(h => h.Stamp) where h.Status == Status.Alive select h).ToList();
					var ts = request.Tags.Split(',').ToList();
					foreach (Heartbeat node in aliveNodes)
					{
						var matches = node.Tags.Split(',').Intersect(ts).ToList();
						if (matches.Count == ts.Count)
						{
							jobNodes.Add(node);
						}
					}
				}
				#endregion
				if ((jobNodes.Count > 0) && (jobNodes.Count >= request.ClientCount))
				{
					if (request.ClientCount <= 0)
					{
						request.ClientCount = jobNodes.Count;
					}
					jobNodes = jobNodes.Take(request.ClientCount).ToList();
					var jobQueues = (from Heartbeat h in jobNodes select h.QueueName).ToList();
					DeclareJobExchange(request.JobID, jobQueues);
					CommitJobToMongo(request, jobNodes);
					uint baseseed = (uint)DateTime.Now.Ticks & 0x0000FFFF;
					for(int i=0;i<jobQueues.Count;i++)
					{
						request.Seed = baseseed + Convert.ToUInt32(i);
						rabbit.PublishToQueue(jobQueues[i], request.Serialize(), action);
					}
					Reply(response.Serialize(), action, replyQueue);
				}
				else
				{
					response.JobID = String.Empty;
					response.Success = false;
					if (String.IsNullOrEmpty(request.Tags))
					{
						response.ErrorMessage = String.Format("Not enough Alive nodes available, current available: {0}\n", jobNodes.Count);
					}
					else
					{
						response.ErrorMessage = String.Format("Not enough Alive nodes matching tags ({0}), current available: {1}\n", request.Tags, jobNodes.Count);
					}
					Reply(response.Serialize(), action, replyQueue);
				}
			}
		}

		private void HeartbeatReceived(Heartbeat heartbeat)
		{
			Heartbeat lastHeartbeat = DatabaseHelper.GetNodeByName(heartbeat.NodeName, config.MongoDb.ConnectionString);

			if (heartbeat.Status == Status.Stopping)
			{
				RemoveNode(heartbeat);
			}
			else
			{
				UpdateNode(heartbeat);
			}

			if ((heartbeat.Status == Status.Alive) && (lastHeartbeat.Status == Status.Running) && (String.IsNullOrEmpty(lastHeartbeat.JobID) == false))
			{
				bool jobFinished = (from n in NodeList(config) where n.JobID == lastHeartbeat.JobID select n).Count() == 0;
				if (jobFinished)
				{
					GenerateReportRequest grr = new GenerateReportRequest();
					grr.JobID = lastHeartbeat.JobID;
					grr.ReportFormat = ReportFormat.PDF;
					rabbit.PublishToQueue(QueueNames.QUEUE_REPORTGENERATOR, grr.Serialize(), Actions.GenerateReport, this.controllerQueueName);
				}
			}


			if (heartbeat.Status == Status.Error)
			{
				//errors.Add(heartbeat);
				heartbeat.SaveToErrors(config.MongoDb.ConnectionString);
				logger.Warn("{0} errored at {1}\n{2}", heartbeat.NodeName, heartbeat.Stamp, heartbeat.ErrorMessage);
			}
		}

		private void ListNodes(ListNodesRequest request, string replyQueue)
		{
			ListNodesResponse response = new ListNodesResponse();
			response.Nodes = NodeList(config).ToList();
			Reply(response.Serialize(), Actions.ListNodes, replyQueue);
		}

		private void ListErrors(ListErrorsRequest listErrorsRequest, string replyQueue)
		{
			ListErrorsResponse response = new ListErrorsResponse();
			//response.Nodes = errors;
			if (String.IsNullOrEmpty(listErrorsRequest.JobID))
			{
				response.Errors = DatabaseHelper.GetAllErrors(config.MongoDb.ConnectionString);
			}
			else
			{
				response.Errors = DatabaseHelper.GetErrors(listErrorsRequest.JobID, config.MongoDb.ConnectionString);
			}
			Reply(response.Serialize(), Actions.ListErrors, replyQueue);
		}

		private void JobInfo(JobInfoRequest request, string replyQueue)
		{
			JobInfoResponse response = new JobInfoResponse();
			Common.Mongo.Job mongoJob = DatabaseHelper.GetJob(request.JobID, config.MongoDb.ConnectionString);
			if (mongoJob == null)
			{
				response.Success = false;
				response.ErrorMessage = String.Format("Job {0} does not exist.", request.JobID);
			}
			else
			{
				response.Success = true;
				response.Job = new Common.Messages.Job(mongoJob);
				var nodes = NodeList(config);
				response.Nodes = (from Heartbeat h in nodes where (h.Status == Status.Running) && (h.JobID == request.JobID) select h).ToList();
			}

			Reply(response.Serialize(), Actions.JobInfo, replyQueue);
		}

		private void Monitor(MonitorRequest request, string replyQueue)
		{
			MonitorResponse response = new MonitorResponse();
			response.MongoDbConnectionString = config.MongoDb.ConnectionString;

			response.Nodes = NodeList(config);
			var activeJobs = response.Nodes.GetJobs(config.MongoDb.ConnectionString);
			var allJobs = DatabaseHelper.GetAllJobs(config.MongoDb.ConnectionString);
			response.ActiveJobs = activeJobs.ToMessagesJobs();
			response.InactiveJobs = allJobs.Except(activeJobs, new JobComparer()).ToMessagesJobs();

			response.Errors = DatabaseHelper.GetAllErrors(config.MongoDb.ConnectionString);

			Reply(response.Serialize(), Actions.Monitor, replyQueue);
		}

		private void GenerateReportComplete(GenerateReportResponse generateReportResponse, string replyQueue)
		{
			
		}
		#endregion

		protected virtual void CommitJobToMongo(StartPeachRequest request, List<Heartbeat> nodes)
		{

			#region Create Job record in Mongo

			PeachFarm.Common.Mongo.Job mongoJob = new PeachFarm.Common.Mongo.Job();
			mongoJob.JobID = request.JobID;
			mongoJob.UserName = request.UserName;
			mongoJob.Pit.FileName = request.PitFileName;
			//mongoJob.Pit.FullText = request.Pit;
			mongoJob.ZipFile = request.ZipFile;

			//string text = request.Pit;
			//if (text.StartsWith("<!"))
			//{
			//  text = text.Substring("<![CDATA[".Length);
			//  text = text.Substring(0, text.Length - 2);
			//}

			mongoJob.Pit.Version = request.PitVersion;

			//try
			//{
			//  XDocument xdoc = XDocument.Parse(text);
			//  var versionAttrib = xdoc.Root.Attribute("version");
			//  if (versionAttrib != null)
			//    mongoJob.Pit.Version = versionAttrib.Value;
			//}
			//catch
			//{

			//}

			mongoJob.StartDate = DateTime.Now;
			mongoJob.Tags = request.Tags;
			
			//TODO Peach Version

			mongoJob = mongoJob.SaveToDatabase(request.MongoDbConnectionString);

			mongoJob.Nodes = new List<Node>();

			foreach (var n in nodes)
			{
				Node node = new Node();
				node.Name = n.NodeName;
				node.Tags = n.Tags;
				node.JobID = request.JobID;
				node.SaveToDatabase(config.MongoDb.ConnectionString);
				mongoJob.Nodes.Add(node);
			}

			#endregion
		}

		private void UpdateNode(Heartbeat heartbeat)
		{
			heartbeat.SaveToDatabase(config.MongoDb.ConnectionString);
			logger.Debug("{0}\t{1}\t{2}", heartbeat.NodeName, heartbeat.Status.ToString(), heartbeat.Stamp);

			if (heartbeat.Status == Status.Running)
			{
				try
				{
					rabbit.BindQueueToExchange(String.Format(QueueNames.EXCHANGE_JOB, heartbeat.JobID), heartbeat.QueueName, heartbeat.JobID);
				}
				catch { }
			}
		}

		private void RemoveNode(Heartbeat heartbeat)
		{

			heartbeat.RemoveFromDatabase(config.MongoDb.ConnectionString);
			logger.Info("{0}\t{1}\t{2}", heartbeat.NodeName, heartbeat.Status.ToString(), heartbeat.Stamp);

=======
			MongoDBInitializer();

			if (statusCheck == null)
			{
				statusCheck = new Timer(new TimerCallback(StatusCheck), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
			}
		}

		void rabbit_MessageReceived(object sender, RabbitMqHelper.MessageReceivedEventArgs e)
		{
			try
			{
				ProcessAction(e.Action, e.Body, e.ReplyQueue);
			}
			catch (Exception ex)
			{
				ProcessException(ex, e.Action, e.ReplyQueue);
			}
		}

		#region Properties
		public bool IsListening
		{
			get { return rabbit.IsListening; }
		}

		public string QueueName
		{
			get
			{
				return controllerQueueName;
			}
		}
		#endregion

		public void Close()
		{
			rabbit.StopListener();
			//rabbit.CloseConnection();
		}

		private void StatusCheck(object state)
		{
			//Catching all exceptions because StatusCheck is called often
			List<Heartbeat> nodes = DatabaseHelper.GetAllNodes(config.MongoDb.ConnectionString);

			try
			{
				foreach (var node in nodes)
				{
					if (node.Stamp.AddMinutes(20) < DateTime.Now)
					{
						logger.Warn("{0}\t{1}\t{2}", node.NodeName, "Node Expired", node.Stamp);
						node.RemoveFromDatabase(config.MongoDb.ConnectionString);
						node.ErrorMessage = "Node Expired";
						node.Stamp = DateTime.Now;
						node.SaveToErrors(config.MongoDb.ConnectionString);
					}
					else
					{
						if (node.Stamp.AddMinutes(10) < DateTime.Now)
						{
							node.Status = Status.Late;
							logger.Info("{0}\t{1}\t{2}", node.NodeName, node.Status.ToString(), node.Stamp);
							node.SaveToDatabase(config.MongoDb.ConnectionString);
						}
					}
				}
			}
			catch { }
		}

		#region MQ functions
		private void PublishToClients(string body, string action)
		{
			rabbit.PublishToExchange(QueueNames.EXCHANGE_NODE, body, action);
			logger.Trace("Sent Action to Clients: {0}\nBody:\n{1}", action, body);
		}

		private bool PublishToJob(string jobID, string body, string action)
		{
			bool result = false;
			string exchangename = String.Format(QueueNames.EXCHANGE_JOB, jobID);

			rabbit.PublishToExchange(exchangename, body, action);
			logger.Trace("Sent Action to Job {0}: {1}\nBody:\n{2}", jobID, action, body);
			result = true;

			return result;
		}

		private void DeclareJobExchange(string jobID, List<string> queueNames)
		{
			string exchangeName = String.Format(QueueNames.EXCHANGE_JOB, jobID);
			rabbit.DeclareExchange(exchangeName, queueNames, jobID);
		}

		private void DeleteJobExchange(string jobID)
		{
			string exchangeName = String.Format(QueueNames.EXCHANGE_JOB, jobID);
			rabbit.DeleteExchange(exchangeName);
		}

		private void Reply(string body, string action, string replyQueue)
		{
			rabbit.PublishToQueue(replyQueue, body, action);
			logger.Trace("Sent Action to {2}: {0}\nBody:\n{1}", action, body, replyQueue);
		}

		private void ProcessAction(string action, string body, string replyQueue)
		{
			Debug.WriteLine("action: " + action);
			Debug.WriteLine("reply: " + replyQueue);
			Debug.WriteLine(body);
			switch (action)
			{
				case Actions.StartPeach:
					StartPeach(StartPeachRequest.Deserialize(body), replyQueue);
					break;
				case Actions.StopPeach:
					StopPeach(StopPeachRequest.Deserialize(body), replyQueue);
					break;
				case Actions.Heartbeat:
					HeartbeatReceived(Heartbeat.Deserialize(body));
					break;
				case Actions.GenerateReport:
					GenerateReportComplete(GenerateReportResponse.Deserialize(body), replyQueue);
					break;
				#region deprecated
				/*
				case Actions.ListNodes:
					ListNodes(ListNodesRequest.Deserialize(body), replyQueue);
					break;
				case Actions.ListErrors:
					ListErrors(ListErrorsRequest.Deserialize(body), replyQueue);
					break;
				case Actions.JobInfo:
					JobInfo(JobInfoRequest.Deserialize(body), replyQueue);
					break;
				case Actions.Monitor:
					Monitor(MonitorRequest.Deserialize(body), replyQueue);
					break;
				//*/
				#endregion
				default:
					string error = String.Format("Received unknown action {0}", action);
					logger.Error(error);
					break;
			}
		}


		private void ProcessException(Exception ex, string action, string replyQueue)
		{
			ResponseBase response = null;
			switch (action)
			{
				case Actions.StartPeach:
					response = new StartPeachResponse();
					break;
				case Actions.StopPeach:
					response = new StopPeachResponse();
					break;
				case Actions.ListNodes:
					response = new ListNodesResponse();
					break;
				case Actions.ListErrors:
					response = new ListErrorsResponse();
					break;
				case Actions.JobInfo:
					response = new JobInfoResponse();
					break;
				case Actions.Monitor:
					response = new MonitorResponse();
					break;
				default:
					string error = String.Format("Error while processing {0} message from {1}:\n{2}", action, replyQueue, ex.Message);
					logger.Error(error);
					return;
			}

			if (response != null)
			{
				response.Success = false;
				response.ErrorMessage = ex.Message;
				Reply(response.ToString(), action, replyQueue);
			}
		}

		#endregion

		#region Receives

		/// <summary>
		/// For Stopping a Job
		/// </summary>
		/// <param name="request"></param>
		/// <param name="replyQueue"></param>
		private void StopPeach(StopPeachRequest request, string replyQueue)
		{
			StopPeachResponse response = new StopPeachResponse(request);

			// Try to find job
			var job = Common.Mongo.DatabaseHelper.GetJob(request.JobID, config.MongoDb.ConnectionString);

			if (job == null)
			{	// Job does not exist
				response.Success = false;
				response.ErrorMessage = String.Format("Job {0} does not exist.", request.JobID);
			}
			else
			{ // Job exists

				// get a list of all the online nodes
				var nodes = Common.Mongo.DatabaseHelper.GetAllNodes(config.MongoDb.ConnectionString);

				// pare down the nodes to those running the Job
				var jobnodes = (from n in nodes where n.Status == Status.Running && n.JobID == request.JobID select n).Count();

				if (jobnodes == 0)
				{	// there's no nodes running the job, so no point in stopping
					response.Success = false;
					response.ErrorMessage = String.Format("Job {0} is not running.", request.JobID);
				}
				else
				{	// there's nodes running the job.

					// forward the StopPeach message to all the nodes running the job.
					bool result = PublishToJob(request.JobID, request.Serialize(), "StopPeach");

					if (result == false)
					{	// if somehow the publish to rabbit fails, notify the admin that the request failed.
						response.Success = false;
						response.ErrorMessage = String.Format("Cannot stop job {0}", request.JobID);
					}
				}
			}

			// After building the response to the Admin, send it.
			Reply(response.Serialize(), Actions.StopPeach, replyQueue);
		}

		private void StartPeach(StartPeachRequest request, string replyQueue)
		{
			string action = Actions.StartPeach;
			request.MongoDbConnectionString = config.MongoDb.ConnectionString;


			StartPeachResponse response = new StartPeachResponse(request);
			
			/*
			Explanation of the StartPeachRequest, looking at the properties that the Controller uses:
					ClientCount
					IPAddress
					Tags
			
			These three properties are used to select Alive nodes for running a job.
					Case 1: ClientCount is 1 (default), IPAddress is specified
						Run the Peach Job on a specific IPAddress
			
					Case 2: Tags are specified, ClientCount is not
						Run the Peach Job on nodes with tags that match *all* the tags specified in the request
						Examples- 5 Alive nodes with tags:
							Windows, ApplicationA
							Windows, ApplicationA
							Linux, ApplicationA
							Linux, ApplicationA
							Windows
			 
						--If Tags in the request is "Windows", the 3 nodes with the Windows tag will be chosen.
						--If Tags in the request is "ApplicationA", the 4 nodes with the ApplicationA tag will be chosen.
			  		--If Tags in the request is "Linux,ApplicationA" the 2 nodes with both the Linux tag and ApplicationA tag will be chosen.
			  		
			  		Summary: All tags specified in the request must match within the set of tags for each individual node
			   
					Case 3: Tags are specified, so is ClientCount
			  		Same as Case 2, but the total nodes selected are limited by ClientCount
			  		Example, consider the 5 Alive nodes from Case 2. If Tags is "ApplicationA" and ClientCount is 3, while in Case 2 four nodes
			  			would be selected, in this case the first three available would be selected.
			 
			  	Case 4: ClientCount is specified, but Tags is not
			  		In this case, the total set of Alive nodes will be taken from based on ClientCount. If the total Alive nodes is 10, and
			  		ClientCount is 4, then the first 4 available nodes will be selected to run the Job.
			  		
			 
			*/

			if ((request.ClientCount == 1) && (request.IPAddress.Length > 0))
			{	// Case 1

				// try to find the node by its name or "ipaddress"
				var node = DatabaseHelper.GetNodeByName(request.IPAddress, config.MongoDb.ConnectionString);
				
				// make certain the node was found and it is Alive
				if ((node != null) && (node.Status == Status.Alive))
				{	// found and alive

					// create an exchange in RabbitMQ for the job with the single node
					DeclareJobExchange(request.JobID, new List<string>() { node.QueueName });

					// write the Job to Mongo
					CommitJobToMongo(request, new List<Heartbeat>(){ node });

					// publish the StartPeach message to the nodes
					PublishToJob(request.JobID, request.Serialize(), action);

					// reply success to the Admin
					Reply(response.Serialize(), action, replyQueue);
				}
				else
				{	// node was either not found or wasn't alive

					response.JobID = String.Empty;
					response.Success = false;
					response.ErrorMessage = String.Format("No Alive Node running at IP address {0}\n", request.IPAddress);
					Reply(response.Serialize(), action, replyQueue);
				}
			}
			else
			{
				// Get all the online nodes
				var nodes = DatabaseHelper.GetAllNodes(config.MongoDb.ConnectionString);
				var jobNodes = new List<Heartbeat>();

				#region Handle Tags, 
				if (String.IsNullOrEmpty(request.Tags))
				{	// Tags is empty, so we're going to use all Alive nodes, ordering by newest Stamp date
					// Case 3
					jobNodes = (from Heartbeat h in nodes.OrderByDescending(h => h.Stamp) where h.Status == Status.Alive select h).ToList();
				}
				else
				{	// Case 2 or Case 3

					// get all of our Alive nodes
					var aliveNodes = (from Heartbeat h in nodes.OrderByDescending(h => h.Stamp) where h.Status == Status.Alive select h).ToList();

					// split up the Tags string into a list
					var ts = request.Tags.Split(',').ToList();

					#region match each node against *all* the tags specified in the request
					// note: the Intersect function
					foreach (Heartbeat node in aliveNodes)
					{
						var matches = node.Tags.Split(',').Intersect(ts).ToList();
						if (matches.Count == ts.Count)
						{
							jobNodes.Add(node);
						}
					}
					#endregion
				}
				#endregion

				#region Handle ClientCount
				// In the Handle Tags region we've selected the maximum set of nodes we can use.
				// Now, in Handle ClientCount we're going to limit the count of nodes to the number specified in ClientCount

				if ((jobNodes.Count > 0) && (jobNodes.Count >= request.ClientCount))
				{	// our total count of selected nodes is greater than or equal to the ClientCount, good.

					// we're interpreting a ClientCount of 0 or less as meaning "all nodes"
					if (request.ClientCount <= 0)
					{
						request.ClientCount = jobNodes.Count;
					}

					// here's where we take the first n nodes where n=ClientCount, note the Take LINQ function
					jobNodes = jobNodes.Take(request.ClientCount).ToList();

					// get all the queue names for our selected, limited nodes
					var jobQueues = (from Heartbeat h in jobNodes select h.QueueName).ToList();

					// make the exchange in RabbitMQ for the job
					DeclareJobExchange(request.JobID, jobQueues);

					// write the job to Mongo
					CommitJobToMongo(request, jobNodes);

					#region Peach Seed
					/* 
					 * Important: to make certain that each Node uses a different Seed for initializing Peach,
					 *		we're determining the seed here in the Controller and setting it for each Node
					 */		
					

					uint baseseed = (uint)DateTime.Now.Ticks & 0x0000FFFF;
					for(int i=0;i<jobQueues.Count;i++)
					{
						request.Seed = baseseed + Convert.ToUInt32(i);

						// Here's the slightly odd bit. Because the Seed is different for each node,
						// we're sending StartPeach requests to each node individually instead of publishing
						// to the exchange
						rabbit.PublishToQueue(jobQueues[i], request.Serialize(), action);
					}
					#endregion

					// Replying to the Admin that the StartPeach messages have been sent out to the Nodes a-okay.
					Reply(response.Serialize(), action, replyQueue);
				}
				else
				{	// The user has specified a ClientCount that's greater than the number of selected/all Alive nodes
					// so send back an error
					response.JobID = String.Empty;
					response.Success = false;
					if (String.IsNullOrEmpty(request.Tags))
					{
						response.ErrorMessage = String.Format("Not enough Alive nodes available, current available: {0}\n", jobNodes.Count);
					}
					else
					{
						response.ErrorMessage = String.Format("Not enough Alive nodes matching tags ({0}), current available: {1}\n", request.Tags, jobNodes.Count);
					}
					Reply(response.Serialize(), action, replyQueue);
				}
				#endregion
			}
		}

		private void HeartbeatReceived(Heartbeat heartbeat)
		{
			/* The last received Heartbeat for every online Node is stored in MongoDB.
			 * When a new Heartbeat is received, that record is overwritten.
			 * If there are 4 online nodes, there will be only 4 records in the Nodes collection in MongoDB
			 */
			  
			// find the Heartbeat in the Nodes collection that matches the Node in the incoming heartbeat 
			Heartbeat lastHeartbeat = DatabaseHelper.GetNodeByName(heartbeat.NodeName, config.MongoDb.ConnectionString);

			if (heartbeat.Status == Status.Stopping)
			{	// if the incoming heartbeat is Stopping, then remove the Node from the database since it's going offline.
				RemoveNode(heartbeat);
			}
			else
			{	// if the incoming heartbeat is any other status, update it.
				UpdateNode(heartbeat);
			}

			#region Stuff that gets executed when the Controller determines that a Node has completed a Peach Run
			if ((heartbeat.Status == Status.Alive) && (lastHeartbeat.Status == Status.Running) && (String.IsNullOrEmpty(lastHeartbeat.JobID) == false))
			{
				#region Stuff that gets executed when the Controller determines that all Nodes running a Job have completed
				/* The important thing here is that the Nodes don't have any visibility to each other, they don't talk to each other.
				 * So while each individual Node knows that it's completed a Peach run, they individually have no idea if the others have completed.
				 * The Controller has to ask two questions to know if a Job has been completed:
				 *	1) Did this Node we're updating just complete Peach? This is answered by the if statement above
				 *	2) Are there any Nodes still running the Job? If no, then the Job is complete. Answered by the LINQ query and if statement below.
				 */	

				// Are there 0 nodes still running this Job?
				bool jobFinished = (from n in DatabaseHelper.GetAllNodes(config.MongoDb.ConnectionString) where n.JobID == lastHeartbeat.JobID select n).Count() == 0;
				if (jobFinished)
				{	// There are no nodes running the Job

					#region Generate Report, send request to Reporting Service.
					GenerateReportRequest grr = new GenerateReportRequest();
					grr.JobID = lastHeartbeat.JobID;
					grr.ReportFormat = ReportFormat.PDF;
					rabbit.PublishToQueue(QueueNames.QUEUE_REPORTGENERATOR, grr.Serialize(), Actions.GenerateReport, this.controllerQueueName);
					#endregion
				}
				#endregion
			}
			#endregion

			#region Log all Error Heartbeats to the Errors collection in MongoDB

			/* Heartbeats have two important properties: Status and ErrorMessage.
			 * Heartbeats are used to report Status updates from the Nodes, but also to report Errors.
			 * Nodes may encounter an Error, but in most (ideally, all) cases the Node should be able to recover
			 *		and become available for new work-- instead of crashing.
			 * So a Node won't really sit in an Error state. It'll send an Error heartbeat, which gets logged here.
			 * Then the Node will go to an Alive state so that it's available for new work. This all happens within
			 * a fraction of a second, which is why you should never see a Node sitting in an Error state.
			 */


			if (heartbeat.Status == Status.Error)
			{
				heartbeat.SaveToErrors(config.MongoDb.ConnectionString);
				logger.Warn("{0} errored at {1}\n{2}", heartbeat.NodeName, heartbeat.Stamp, heartbeat.ErrorMessage);
			}
			#endregion
		}

		#region deprecated
		/*
		private void ListNodes(ListNodesRequest request, string replyQueue)
		{
			ListNodesResponse response = new ListNodesResponse();
			response.Nodes = DatabaseHelper.GetAllNodes(config.MongoDb.ConnectionString).ToList();
			Reply(response.Serialize(), Actions.ListNodes, replyQueue);
		}

		private void ListErrors(ListErrorsRequest listErrorsRequest, string replyQueue)
		{
			ListErrorsResponse response = new ListErrorsResponse();
			//response.Nodes = errors;
			if (String.IsNullOrEmpty(listErrorsRequest.JobID))
			{
				response.Errors = DatabaseHelper.GetAllErrors(config.MongoDb.ConnectionString);
			}
			else
			{
				response.Errors = DatabaseHelper.GetErrors(listErrorsRequest.JobID, config.MongoDb.ConnectionString);
			}
			Reply(response.Serialize(), Actions.ListErrors, replyQueue);
		}

		private void JobInfo(JobInfoRequest request, string replyQueue)
		{
			JobInfoResponse response = new JobInfoResponse();
			Common.Mongo.Job mongoJob = DatabaseHelper.GetJob(request.JobID, config.MongoDb.ConnectionString);
			if (mongoJob == null)
			{
				response.Success = false;
				response.ErrorMessage = String.Format("Job {0} does not exist.", request.JobID);
			}
			else
			{
				response.Success = true;
				response.Job = new Common.Messages.Job(mongoJob);
				var nodes = DatabaseHelper.GetAllNodes(config.MongoDb.ConnectionString);
				response.Nodes = (from Heartbeat h in nodes where (h.Status == Status.Running) && (h.JobID == request.JobID) select h).ToList();
			}

			Reply(response.Serialize(), Actions.JobInfo, replyQueue);
		}

		private void Monitor(MonitorRequest request, string replyQueue)
		{
			MonitorResponse response = new MonitorResponse();
			response.MongoDbConnectionString = config.MongoDb.ConnectionString;

			response.Nodes = DatabaseHelper.GetAllNodes(config.MongoDb.ConnectionString);
			var activeJobs = response.Nodes.GetJobs(config.MongoDb.ConnectionString);
			var allJobs = DatabaseHelper.GetAllJobs(config.MongoDb.ConnectionString);
			response.ActiveJobs = activeJobs.ToMessagesJobs();
			response.InactiveJobs = allJobs.Except(activeJobs, new JobComparer()).ToMessagesJobs();

			response.Errors = DatabaseHelper.GetAllErrors(config.MongoDb.ConnectionString);

			Reply(response.Serialize(), Actions.Monitor, replyQueue);
		}
		//*/
		#endregion

		private void GenerateReportComplete(GenerateReportResponse generateReportResponse, string replyQueue)
		{
			// do nothing when report complete response is received
		}
		#endregion

		private void CommitJobToMongo(StartPeachRequest request, List<Heartbeat> nodes)
		{

			#region Create Job record in Mongo

			PeachFarm.Common.Mongo.Job mongoJob = new PeachFarm.Common.Mongo.Job();
			mongoJob.JobID = request.JobID;
			mongoJob.UserName = request.UserName;
			mongoJob.Pit.FileName = request.PitFileName;
			//mongoJob.Pit.FullText = request.Pit;
			mongoJob.ZipFile = request.ZipFile;

			//string text = request.Pit;
			//if (text.StartsWith("<!"))
			//{
			//  text = text.Substring("<![CDATA[".Length);
			//  text = text.Substring(0, text.Length - 2);
			//}

			mongoJob.Pit.Version = request.PitVersion;

			//try
			//{
			//  XDocument xdoc = XDocument.Parse(text);
			//  var versionAttrib = xdoc.Root.Attribute("version");
			//  if (versionAttrib != null)
			//    mongoJob.Pit.Version = versionAttrib.Value;
			//}
			//catch
			//{

			//}

			mongoJob.StartDate = DateTime.Now;
			mongoJob.Tags = request.Tags;
			
			//TODO Peach Version

			mongoJob = mongoJob.SaveToDatabase(request.MongoDbConnectionString);

			mongoJob.Nodes = new List<Node>();

			foreach (var n in nodes)
			{
				Node node = new Node();
				node.Name = n.NodeName;
				node.Tags = n.Tags;
				node.JobID = request.JobID;
				node.SaveToDatabase(config.MongoDb.ConnectionString);
				mongoJob.Nodes.Add(node);
			}

			#endregion
		}

		private void UpdateNode(Heartbeat heartbeat)
		{
			heartbeat.SaveToDatabase(config.MongoDb.ConnectionString);
			logger.Debug("{0}\t{1}\t{2}", heartbeat.NodeName, heartbeat.Status.ToString(), heartbeat.Stamp);

			if (heartbeat.Status == Status.Running)
			{
				try
				{
					rabbit.BindQueueToExchange(String.Format(QueueNames.EXCHANGE_JOB, heartbeat.JobID), heartbeat.QueueName, heartbeat.JobID);
				}
				catch { }
			}
		}

		private void RemoveNode(Heartbeat heartbeat)
		{

			heartbeat.RemoveFromDatabase(config.MongoDb.ConnectionString);
			logger.Info("{0}\t{1}\t{2}", heartbeat.NodeName, heartbeat.Status.ToString(), heartbeat.Stamp);

>>>>>>> 7e22574bd6bccee96fac271956c4d4f531abdbf0
		}

		// this and the mongodb initializer might need to be refactored into classes later...
		private void RabbitInitializer() {
			if (__test_should_rabbitmq_init == false) return;

			rabbit = new RabbitMqHelper(config.RabbitMq.HostName, config.RabbitMq.Port, config.RabbitMq.UserName, config.RabbitMq.Password, config.RabbitMq.SSL);
			if ((config.Controller == null) || (String.IsNullOrEmpty(config.Controller.Name)))
			{
				this.controllerQueueName = String.Format(QueueNames.QUEUE_CONTROLLER, ipaddress);
			}
			else
			{
				this.controllerQueueName = String.Format(QueueNames.QUEUE_CONTROLLER, config.Controller.Name);
			}
			rabbit.MessageReceived += new EventHandler<RabbitMqHelper.MessageReceivedEventArgs>(rabbit_MessageReceived);
			rabbit.StartListener(this.controllerQueueName);
		}

		private void MongoDBInitializer() {
			if (__test_should_mongodb_init == false) return;

			if (Common.Mongo.DatabaseHelper.TestConnection(config.MongoDb.ConnectionString) == false)
			{
				string error = String.Format("No connection can be made to MongoDB at:\n{0}", config.MongoDb.ConnectionString);
				throw new ApplicationException(error);
			}
<<<<<<< HEAD
		}

		protected virtual IPAddress[] LocalIPs()
		{
			return System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
		}

		private Configuration.ControllerSection ControllerConfigSection()
		{
			if (PeachFarmController.__test_config != null) return PeachFarmController.__test_config;
			else return (Configuration.ControllerSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.controller");	
		}

		protected  virtual List<Heartbeat> NodeList(PeachFarm.Controller.Configuration.ControllerSection config)
		{
			/* This could be static, but we need to be able to override it for testing purposes */
			return DatabaseHelper.GetAllNodes(config.MongoDb.ConnectionString);
		}

		protected virtual Heartbeat GetNodeByName(string ipaddress, string mongoConnectionString)
		{
			return DatabaseHelper.GetNodeByName(ipaddress, mongoConnectionString);
		}
	}
}
=======
		}

		private IPAddress[] LocalIPs()
		{
			if (__test_LocalIPs != null) return __test_LocalIPs;
			else return System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
		}

		private Configuration.ControllerSection ControllerConfigSection()
		{
			if (PeachFarmController.__test_config != null) return PeachFarmController.__test_config;
			else return (Configuration.ControllerSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.controller");	
		}

	}
}
>>>>>>> 7e22574bd6bccee96fac271956c4d4f531abdbf0
