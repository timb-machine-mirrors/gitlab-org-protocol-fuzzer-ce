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
	public class PeachFarmController
	{

		private UTF8Encoding encoding = new UTF8Encoding();
		private static string ipaddress;
		private Configuration.ControllerSection config;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		private static Timer statusCheck = null;

		RabbitMqHelper rabbit = null;

		private string serverQueueName;

		public PeachFarmController()
		{
			// Startup as application
			IPAddress[] ipaddresses = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
			ipaddress = (from i in ipaddresses where i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork select i).First().ToString();

			config = (Configuration.ControllerSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.controller");

			//ipaddress = config.ServerHost.IpAddress;

			if (Common.Mongo.DatabaseHelper.TestConnection(config.MongoDb.ConnectionString) == false)
			{
				string error = String.Format("No connection can be made to MongoDB at:\n{0}", config.MongoDb.ConnectionString);
				throw new ApplicationException(error);
			}

			rabbit = new RabbitMqHelper(config.RabbitMq.HostName, config.RabbitMq.Port, config.RabbitMq.UserName, config.RabbitMq.Password, config.RabbitMq.SSL);
			serverQueueName = String.Format(QueueNames.QUEUE_CONTROLLER, ipaddress);
			rabbit.MessageReceived += new EventHandler<RabbitMqHelper.MessageReceivedEventArgs>(rabbit_MessageReceived);
			rabbit.StartListener(serverQueueName);

			if (statusCheck == null)
			{
				statusCheck = new Timer(new TimerCallback(StatusCheck), null, TimeSpan.FromMilliseconds(0), TimeSpan.FromMinutes(1));
			}

			IsOpen = true;
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
		public bool IsOpen { get; set; }

		public string QueueName
		{
			get
			{
				return serverQueueName;
			}
		}
		#endregion

		public void Close()
		{
			IsOpen = false;
			rabbit.StopListener();
			rabbit.CloseConnection();
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
				case "StartPeach":
					StartPeach(StartPeachRequest.Deserialize(body), replyQueue);
					break;
				case "StopPeach":
					StopPeach(StopPeachRequest.Deserialize(body), replyQueue);
					break;
				case "Heartbeat":
					HeartbeatReceived(Heartbeat.Deserialize(body));
					break;
				case "ListNodes":
					ListNodes(ListNodesRequest.Deserialize(body), replyQueue);
					break;
				case "ListErrors":
					ListErrors(ListErrorsRequest.Deserialize(body), replyQueue);
					break;
				case "JobInfo":
					JobInfo(JobInfoRequest.Deserialize(body), replyQueue);
					break;
				case "Monitor":
					Monitor(MonitorRequest.Deserialize(body), replyQueue);
					break;
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
				case "StartPeach":
					response = new StartPeachResponse();
					break;
				case "StopPeach":
					response = new StopPeachResponse();
					break;
				case "ListNodes":
					response = new ListNodesResponse();
					break;
				case "ListErrors":
					response = new ListErrorsResponse();
					break;
				case "JobInfo":
					response = new JobInfoResponse();
					break;
				case "Monitor":
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
		private void StopPeach(StopPeachRequest request, string replyQueue)
		{
			StopPeachResponse response = new StopPeachResponse(request);
			if (Common.Mongo.DatabaseHelper.GetJob(request.JobID, config.MongoDb.ConnectionString) == null)
			{
				response.Success = false;
				response.ErrorMessage = String.Format("Job {0} does not exist.", request.JobID);
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
			Reply(response.Serialize(), "StopPeach", replyQueue);

		}

		private void StartPeach(StartPeachRequest request, string replyQueue)
		{
			string action = "StartPeach";
			request.MongoDbConnectionString = config.MongoDb.ConnectionString;

			// moving this to Admin
			//request.JobID = DatabaseHelper.GetJobID(config.MongoDb.ConnectionString);

			StartPeachResponse response = new StartPeachResponse(request);


			if ((request.ClientCount == 1) && (request.IPAddress.Length > 0))
			{
				var node = DatabaseHelper.GetNodeByName(request.IPAddress, config.MongoDb.ConnectionString);
				
				if ((node != null) && (node.Status == Status.Alive))
				{
					DeclareJobExchange(request.JobID, new List<string>() { node.QueueName });

					CommitJobToMongo(request, new List<Heartbeat>(){ node });

					PublishToJob(request.JobID, request.Serialize(), action);


					Reply(response.Serialize(), action, replyQueue);
				}
				else
				{
					response.JobID = String.Empty;
					response.Success = false;
					response.ErrorMessage = String.Format("No Alive Node running at IP address {0}\n", request.IPAddress);
					Reply(response.Serialize(), action, replyQueue);
				}
			}
			else
			{
				var nodes = DatabaseHelper.GetAllNodes(config.MongoDb.ConnectionString);
				var jobNodes = new List<Heartbeat>();

				if (String.IsNullOrEmpty(request.Tags))
				{
					jobNodes = (from Heartbeat h in nodes.OrderByDescending(h => h.Stamp) where h.Status == Status.Alive select h).ToList();
				}
				else
				{
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

					//PublishToJob(request.JobID, request.Serialize(), action);

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
			if (heartbeat.Status == Status.Stopping)
			{
				RemoveNode(heartbeat);
			}
			else
			{
				UpdateNode(heartbeat);
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
			response.Nodes = DatabaseHelper.GetAllNodes(config.MongoDb.ConnectionString).ToList();
			Reply(response.Serialize(), "ListNodes", replyQueue);
		}

		private void ListErrors(ListErrorsRequest listErrorsRequest, string replyQueue)
		{
			ListErrorsResponse response = new ListErrorsResponse();
			//response.Nodes = errors;
			if (String.IsNullOrEmpty(listErrorsRequest.JobID))
			{
				response.Nodes = DatabaseHelper.GetErrors(config.MongoDb.ConnectionString);
			}
			else
			{
				response.Nodes = DatabaseHelper.GetErrors(listErrorsRequest.JobID, config.MongoDb.ConnectionString);
			}
			Reply(response.Serialize(), "ListErrors", replyQueue);
		}

		private void JobInfo(JobInfoRequest request, string replyQueue)
		{
			JobInfoResponse response = new JobInfoResponse();
			Common.Mongo.Job mongoJob = DatabaseHelper.GetJob(request.JobID, config.MongoDb.ConnectionString);
			if (mongoJob == null)
			{
				response.Success = false;
				response.Message = String.Format("Job {0} does not exist.", request.JobID);
			}
			else
			{
				response.Success = true;
				response.Job = new Common.Messages.Job(mongoJob);
				var nodes = DatabaseHelper.GetAllNodes(config.MongoDb.ConnectionString);
				response.Nodes = (from Heartbeat h in nodes where (h.Status == Status.Running) && (h.JobID == request.JobID) select h).ToList();
			}

			Reply(response.Serialize(), "JobInfo", replyQueue);
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

			Reply(response.Serialize(), "Monitor", replyQueue);
		}
		#endregion

		private void CommitJobToMongo(StartPeachRequest request, List<Heartbeat> nodes)
		{

			#region Create Job record in Mongo

			PeachFarm.Common.Mongo.Job mongoJob = new PeachFarm.Common.Mongo.Job();
			mongoJob.JobID = request.JobID;
			mongoJob.UserName = request.UserName;
			mongoJob.Pit.FileName = request.PitFileName;
			mongoJob.Pit.FullText = request.Pit;

			string text = request.Pit;
			if (text.StartsWith("<!"))
			{
				text = text.Substring("<![CDATA[".Length);
				text = text.Substring(0, text.Length - 2);
			}

			try
			{
				XDocument xdoc = XDocument.Parse(text);
				var versionAttrib = xdoc.Root.Attribute("version");
				if (versionAttrib != null)
					mongoJob.Pit.Version = versionAttrib.Value;
			}
			catch
			{

			}

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

		}
	}
}
