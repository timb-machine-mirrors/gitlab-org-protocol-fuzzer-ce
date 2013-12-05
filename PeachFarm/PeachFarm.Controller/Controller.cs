using System;
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
	public class PeachFarmController : IDisposable
	{

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
		//-------------------------------------------------------------------------
		#endregion testing

		public PeachFarmController()
		{

			this.config = this.ControllerConfigSection();
			this.config.Validate();

			RabbitInitializer();
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
			get 
			{
				if (rabbit != null)
					return rabbit.IsListening;
				else
					return false;
			}
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
			if (rabbit != null)
			{
				rabbit.StopListener();
				rabbit = null;
			}

			//if (hosts != null)
			//{
			//  foreach (var host in hosts)
			//  {
			//    host.Stop();
			//  }
			//}
		}

		protected void StatusCheck(object state)
		{
			//Catching all exceptions because StatusCheck is called often
			List<Heartbeat> nodes = NodeList(config);

			try
			{
				foreach (var node in nodes)
				{
					if (node.Stamp.AddMinutes(config.NodeExpirationRules.Expired) < DateTime.Now)
					{
						logger.Warn("{0}\t{1}\t{2}", node.NodeName, "Node Expired", node.Stamp);
						node.ErrorMessage = "Node Expired";
						node.Stamp = DateTime.Now;
						node.Status = Status.Error;
						node.SaveToErrors(config.MongoDb.ConnectionString);

						node.Status = Status.Stopping;
						HeartbeatReceived(node);
					}
					else
					{
						if (node.Stamp.AddMinutes(config.NodeExpirationRules.Late) < DateTime.Now)
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
			if (replyQueue == null)
				return;

			rabbit.PublishToQueue(replyQueue, body, action);
			logger.Trace("Sent Action to {2}: {0}\nBody:\n{1}", action, body, replyQueue);
		}

		private void ProcessAction(string action, string body, string replyQueue)
		{
			Debug.WriteLine("action: " + action);
			Debug.WriteLine("reply: " + replyQueue);
			Debug.WriteLine(body);

			ResponseBase response = null;
			switch (action)
			{
				case Actions.CreateJob:
					response = CreateJob(CreateJobRequest.Deserialize(body));
					break;
				case Actions.StartPeach:
					response = StartPeach(StartPeachRequest.Deserialize(body));
					break;
				case Actions.StopPeach:
					response = StopPeach(StopPeachRequest.Deserialize(body));
					break;
				case Actions.Heartbeat:
					HeartbeatReceived(Heartbeat.Deserialize(body));
					break;
				case Actions.GenerateJobReport:
					GenerateReportComplete(GenerateJobReportResponse.Deserialize(body));
					break;
				case Actions.Register:
					response = Register(RegisterRequest.Deserialize(body));
					break;
				case Actions.DeleteData:
					response = DeleteData(DeleteDataRequest.Deserialize(body));
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

			if (response != null)
			{
				Reply(response.Serialize(), action, replyQueue);
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
				case Actions.Register:
					response = new RegisterResponse();
					break;
				case Actions.DeleteData:
					response = new DeleteDataResponse();
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

		#region Process Action functions
		protected virtual RegisterResponse Register(RegisterRequest request)
		{
			//TODO Add permissions support

			var response = new RegisterResponse();
			response.MongoDbConnectionString = config.MongoDb.ConnectionString;
			response.MySqlConnectionString = String.Empty;

			response.Success = true;
			return response;
		}

		internal virtual CreateJobResponse CreateJob(CreateJobRequest request)
		{
			PeachFarm.Common.Mongo.Job mongojob = new Common.Mongo.Job();
			mongojob.JobID = DatabaseHelper.GetJobID(config.MongoDb.ConnectionString);

			string jobfolder = String.Format(Formats.JobFolder, mongojob.JobID);
			mongojob.Pit.FileName = request.PitFileName;

			mongojob.Tags = request.Tags;

			string jobzip = String.Format("{0}/{1}.zip", jobfolder, request.PitFileName);
			mongojob.ZipFile = jobzip;

			mongojob.UserName = request.UserName;

			DatabaseHelper.SaveToGridFS(request.ZipFile, jobzip, config.MongoDb.ConnectionString);

			var response = new CreateJobResponse();
			response.JobID = mongojob.JobID;
			return response;
		}

		protected virtual void StopPeach(StopPeachRequest request, string replyQueue)
		{
			var response = StopPeach(request);
			Reply(response.Serialize(), Actions.StopPeach, replyQueue);
		}

		/// <summary>
		/// For Stopping a Job
		/// </summary>
		/// <param name="request"></param>
		internal virtual StopPeachResponse StopPeach(StopPeachRequest request)
		{
			StopPeachResponse response = new StopPeachResponse(request);

			// Try to find job
			var job = GetJob(request);
			if (job == null)
			{	// Job does not exist
				response.Success = false;
				response.ErrorMessage = String.Format("Job {0} does not exist.", request.JobID);
			}
			else
			{ // Job exists

				// get a list of all the online nodes

				// pare down the nodes to those running the Job
				var nodes = NodeList(config);
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

			return response;
		}

		protected virtual void StartPeach(StartPeachRequest request, string replyQueue)
		{
			var response = StartPeach(request);
			System.Diagnostics.Debug.Assert(response != null);
			Reply(request.Serialize(), Actions.StartPeach, replyQueue);
		}

		internal virtual StartPeachResponse StartPeach(StartPeachRequest request)
		{
			#region Method Explanation
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
			#endregion

			string action = Actions.StartPeach;
			request.MongoDbConnectionString = config.MongoDb.ConnectionString;
			StartPeachResponse response = new StartPeachResponse(request);

			#region SingleSpecificNodeChosen
			if ((request.ClientCount == 1) && (request.IPAddress.Length > 0))
			{	// Case 1

				// try to find the node by its name or "ipaddress", then make sure it was found and is Alive
				var node = GetNodeByName(request.IPAddress, config.MongoDb.ConnectionString);
				if ((node != null) && (node.Status == Status.Alive))
				{
					DeclareJobExchange(request.JobID, new List<string>() { node.QueueName });
					CommitJobToMongo(request, new List<Heartbeat>() { node });
					PublishToJob(request.JobID, request.Serialize(), action);
					//Reply(response.Serialize(), action, replyQueue);
				}
				else
				{
					response.JobID = String.Empty;
					response.Success = false;
					response.ErrorMessage = String.Format("No Alive Node running at IP address {0}\n", request.IPAddress);
					//Reply(response.Serialize(), action, replyQueue);
				}
			}
			#endregion
			else
			{
				// Get all the online nodes
				var nodes = NodeList(config);
				var jobNodes = NodesMatchingTags(nodes, request.Tags);

				#region Handle ClientCount
				// In the Handle Tags region we've selected the maximum set of nodes we can use.
				// Now, in Handle ClientCount we're going to limit the count of nodes to the number specified in ClientCount

				if ((jobNodes.Count > 0) && (jobNodes.Count >= request.ClientCount))
				{
					// we're interpreting a ClientCount of 0 or less as meaning "all nodes"
					if (request.ClientCount <= 0)
					{
						request.ClientCount = jobNodes.Count;
					}

					// note the Take LINQ function
					jobNodes = jobNodes.Take(request.ClientCount).ToList();
					var jobQueues = (from Heartbeat h in jobNodes select h.QueueName).ToList();
					DeclareJobExchange(request.JobID, jobQueues);
					jobNodes = SeedNodes(jobNodes);
					CommitJobToMongo(request, jobNodes);
					SendToJobQueues(jobNodes, request);
					//Reply(response.Serialize(), action, replyQueue);
				}
				else
				{
					// not enough live client nodes -> send back an error
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
					//Reply(response.Serialize(), action, replyQueue);
				}
				#endregion
			}

			return response;
		}

		protected void HeartbeatReceived(Heartbeat heartbeat)
		{
			/* The last received Heartbeat for every online Node is stored in MongoDB.
			 * When a new Heartbeat is received, that record is overwritten.
			 * If there are 4 online nodes, there will be only 4 records in the Nodes collection in MongoDB
			 */

			// this is to account for nodes that have their time set incorrectly
			heartbeat.Stamp = DateTime.Now;

			// find the Heartbeat in the Nodes collection that matches the Node in the incoming heartbeat 
			Heartbeat lastHeartbeat = GetNodeByName(heartbeat.NodeName, config.MongoDb.ConnectionString);

			if (heartbeat.Status == Status.Stopping)
			{
				RemoveNode(heartbeat);
			}
			else
			{
				UpdateNode(heartbeat);
			}

			//&& lastHeartbeat.Status == Status.Running
			bool isNodeFinished = false;
			if(heartbeat.Status != Status.Running)
			{
				if ((lastHeartbeat != null) && (String.IsNullOrEmpty(lastHeartbeat.JobID) == false))
				{
					isNodeFinished = true;
				}
			}

			if (isNodeFinished)
			{
				#region Stuff that gets executed when the Controller determines that all Nodes running a Job have completed
				/* The important thing here is that the Nodes don't have any visibility to each other, they don't talk to each other.
				 * So while each individual Node knows that it's completed a Peach run, they individually have no idea if the others have completed.
				 * The Controller has to ask two questions to know if a Job has been completed:
				 *	1) Did this Node we're updating just complete Peach? This is answered by the if statement above
				 *	2) Are there any Nodes still running the Job? If no, then the Job is complete. Answered by the LINQ query and if statement below.
				 */	

				// Are there 0 nodes still running this Job?
				bool isJobFinished = (from n in NodeList(config)
				                    where n.JobID == lastHeartbeat.JobID select n
				                   ).Count() == 0;
				if (isJobFinished)
				{
					#region Generate Report, send request to Reporting Service.
					GenerateJobReportRequest grr = new GenerateJobReportRequest();
					grr.JobID = lastHeartbeat.JobID;
					grr.ReportFormat = ReportFormat.PDF;
					PushReportToQueue(grr);
					#endregion
				}
				#endregion
			}

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
				//errors.Add(heartbeat);
				heartbeat.SaveToErrors(config.MongoDb.ConnectionString);
				logger.Warn("{0} errored at {1}\n{2}", heartbeat.NodeName, heartbeat.Stamp, heartbeat.ErrorMessage);
			}
			#endregion
		}

		private void GenerateReportComplete(GenerateJobReportResponse response)
		{
			// do nothing when report complete response is received
		}

		protected virtual DeleteDataResponse DeleteData(DeleteDataRequest request)
		{
			var response = new DeleteDataResponse();

			//TODO: Check permissions

			var nodes = DatabaseHelper.GetAllNodes(config.MongoDb.ConnectionString);
			var runningjobs = (from n in nodes where n.Status == Status.Running select n.JobID).Distinct();

			if (runningjobs.Count() > 0)
			{
				response.Success = false;
				response.ErrorMessage = "The following Running jobs must be stopped before deleting data:\n";
				foreach (var job in runningjobs)
				{
					response.ErrorMessage += job + "\n";
				}
			}
			else
			{
				try
				{
					switch (request.Type)
					{
						case DeleteDataType.All:
							DatabaseHelper.TruncateAllCollections(config.MongoDb.ConnectionString);
							DatabaseHelper.TruncateAllMetrics(config.MySql.ConnectionString);
							break;
						case DeleteDataType.Job:
							DatabaseHelper.DeleteFaultsForJob(request.Parameter, config.MongoDb.ConnectionString);
							break;
						case DeleteDataType.Target:
							DatabaseHelper.DeleteFaultsForTarget(request.Parameter, config.MongoDb.ConnectionString);
							break;
						default:
							throw new NotSupportedException(String.Format("DeleteData Type {0} not supported.", request.Type.ToString()));
					}
					response.Success = true;
				}
				catch (Exception ex)
				{
					response.Success = false;
					response.ErrorMessage = ex.ToString();
				}
			}

			return response;
		}
		#endregion

		protected virtual Common.Mongo.Job GetJob(StopPeachRequest request)
		{
			return Common.Mongo.DatabaseHelper.GetJob(request.JobID, config.MongoDb.ConnectionString);
		}

		private static List<Heartbeat> NodesMatchingTags(List<Heartbeat> nodes, string tags)
		{
			var jobNodes = new List<Heartbeat>();
			bool shouldUseAllNodes = String.IsNullOrEmpty(tags);
			if (shouldUseAllNodes)
			{
				jobNodes = (from Heartbeat h in nodes.OrderByDescending(h => h.Stamp) where h.Status == Status.Alive select h).ToList();
			}
			else
			{
				var aliveNodes = (from Heartbeat h in nodes.OrderByDescending(h => h.Stamp) where h.Status == Status.Alive select h).ToList();
				var ts = tags.Split(',').ToList();

				// grab nodes that have *all* the tags specified in the request
				foreach (Heartbeat node in aliveNodes)
				{
					var matches = node.Tags.Split(',').Intersect(ts).ToList(); // note: the Intersect function
					if (matches.Count == ts.Count)
					{
						jobNodes.Add(node);
					}
				}
			}
			return jobNodes;
		}

		protected virtual List<Heartbeat> SeedNodes(List<Heartbeat> nodes)
		{
			uint baseseed = (uint)DateTime.Now.Ticks & 0x0000FFFF;
			for (int i = 0; i < nodes.Count; i++)
			{
				nodes[i].Seed = baseseed + Convert.ToUInt32(i);
			}
			return nodes;
		}

		protected virtual void SendToJobQueues(List<Heartbeat> nodes, StartPeachRequest request)
		{
			/*
			 * Important: to make certain that each Node uses a different Seed for initializing Peach,
			 * we're determining the seed here in the Controller and setting it for each Node
			 * 
			 * Here's the slightly odd bit. Because the Seed is different for each node,
			 * we're sending StartPeach requests to each node individually instead of publishing
			 * to the exchange
			 */
			foreach(var node in nodes)
			{
				request.Seed = node.Seed;
				rabbit.PublishToQueue(node.QueueName, request.Serialize(), Actions.StartPeach);
			}
		}

		protected virtual void PushReportToQueue(GenerateJobReportRequest grr)
		{
			rabbit.PublishToQueue(QueueNames.QUEUE_REPORTGENERATOR_PROCESSONE, grr.Serialize(), Actions.GenerateJobReport, this.controllerQueueName);
		}

		protected virtual void RemoveNode(Heartbeat heartbeat)
		{

			heartbeat.RemoveFromDatabase(config.MongoDb.ConnectionString);
			logger.Info("{0}\t{1}\t{2}", heartbeat.NodeName, heartbeat.Status.ToString(), heartbeat.Stamp);

		}

		protected virtual void CommitJobToMongo(StartPeachRequest request, List<Heartbeat> nodes)
		{
			PeachFarm.Common.Mongo.Job mongoJob = new PeachFarm.Common.Mongo.Job();
			mongoJob.JobID = request.JobID;
			mongoJob.UserName = request.UserName;
			mongoJob.Pit.FileName = request.PitFileName;
			//mongoJob.Pit.FullText = request.Pit;
			mongoJob.ZipFile = request.ZipFile;

			mongoJob.Pit.Version = request.PitVersion;

			mongoJob.StartDate = DateTime.Now;
			mongoJob.Tags = request.Tags;

			if (String.IsNullOrEmpty(mongoJob.Target))
			{
				mongoJob.Target = mongoJob.Pit.FileName;
			}

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
		}

		protected virtual void UpdateNode(Heartbeat heartbeat)
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


		// this and the mongodb initializer might need to be refactored into classes later...
		private void RabbitInitializer() {
			if (__test_should_rabbitmq_init == false) return;

			rabbit = new RabbitMqHelper(config.RabbitMq.HostName, config.RabbitMq.Port, config.RabbitMq.UserName, config.RabbitMq.Password, config.RabbitMq.SSL);
			if (String.IsNullOrEmpty(config.Controller.Name))
			{
				this.controllerQueueName = String.Format(QueueNames.QUEUE_CONTROLLER, rabbit.LocalIP);
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

		protected virtual List<Heartbeat> NodeList(PeachFarm.Controller.Configuration.ControllerSection config)
		{
			/* This could be static, but we need to be able to override it for testing purposes */
			return DatabaseHelper.GetAllNodes(config.MongoDb.ConnectionString);
		}

		protected virtual Heartbeat GetNodeByName(string ipaddress, string mongoConnectionString)
		{
			return DatabaseHelper.GetNodeByName(ipaddress, mongoConnectionString);
		}

		public void Dispose()
		{
			Close();
		}
	}
}
