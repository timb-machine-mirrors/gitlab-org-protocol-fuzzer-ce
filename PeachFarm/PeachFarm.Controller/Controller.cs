using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PeachFarm.Common.Messages;
using System.Net;
using PeachFarm.Common;
using System.Threading;
using RabbitMQ.Client;
using System.ComponentModel;
using NLog;
using System.Diagnostics;
using System.Security.Cryptography;
using PeachFarm.Common.Mongo;
 
namespace PeachFarm.Controller
{
	public class PeachFarmServer
	{
		private static Dictionary<string, Heartbeat> nodes = new Dictionary<string, Heartbeat>();
		private static Dictionary<string, List<string>> tags = new Dictionary<string, List<string>>();
		//private List<Heartbeat> errors = new List<Heartbeat>();
		private UTF8Encoding encoding = new UTF8Encoding();
		private static string ipaddress;
		private Configuration.ControllerSection config;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		private static Timer statusCheck = null;
		public PeachFarmServer()
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

			try
			{
				OpenConnection(config.RabbitMq.HostName, config.RabbitMq.Port, config.RabbitMq.UserName, config.RabbitMq.Password);
			}
			catch (Exception ex)
			{
				string error = String.Format("Could not open connection to RabbitMQ server at {0}, exiting now. Exception:\n{1}", config.RabbitMq.HostName, ex.Message);
				throw new ApplicationException(error);
			}

			RegisterQueues();
			StartListener();

			if (statusCheck == null)
			{
				statusCheck = new Timer(new TimerCallback(StatusCheck), null, TimeSpan.FromMilliseconds(0), TimeSpan.FromMinutes(1));
			}

			IsOpen = true;
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

		private void StatusCheck(object state)
		{
			List<Heartbeat> remove = new List<Heartbeat>();
			lock (nodes)
			{
				foreach (KeyValuePair<string, Heartbeat> pair in nodes)
				{
					Debug.WriteLine(String.Format("Status Check:{0} {1}", pair.Value.Stamp.ToString(), DateTime.Now.ToString()));
					if (pair.Value.Stamp.AddMinutes(20) < DateTime.Now)
					{
						logger.Warn("{0}\t{1}\t{2}", pair.Value.NodeName, "Node Expired", pair.Value.Stamp);
						remove.Add(pair.Value);
						pair.Value.ErrorMessage = "Node Expired";
						pair.Value.Stamp = DateTime.Now;
						pair.Value.DatabaseInsert(config.MongoDb.ConnectionString);
					}
					else
					{
						if (pair.Value.Stamp.AddMinutes(10) < DateTime.Now)
						{
							pair.Value.Status = Status.Late;
							logger.Info("{0}\t{1}\t{2}", pair.Value.NodeName, pair.Value.Status.ToString(), pair.Value.Stamp);
						}
					}
				}
			}

			foreach (Heartbeat node in remove)
			{
				RemoveNode(node);
			}
		}

		#region MQ functions

		private string serverQueueName;

		private IConnection connection;
		private IModel modelSend;
		private IModel modelReceive;

		private BackgroundWorker listener;

		private void OpenConnection(string hostName, int port, string userName, string password)
		{
			ConnectionFactory factory = new ConnectionFactory();
			if (hostName.Length > 0)
			{
				factory.HostName = hostName;
			}
			else
			{
				factory.HostName = "localhost";
			}

			if (port > 0)
			{
				factory.Port = port;
			}
			else
			{
				factory.Port = -1;
			}

			if (userName.Length > 0)
			{
				factory.UserName = userName;
			}
			else
			{
				factory.UserName = "guest";
			}

			if (password.Length > 0)
			{
				factory.Password = password;
			}
			else
			{
				factory.Password = "guest";
			}
			connection = factory.CreateConnection();
			modelSend = connection.CreateModel();
			modelReceive = connection.CreateModel();
		}

		private void RegisterQueues()
		{
			//server queue
			serverQueueName = String.Format(QueueNames.QUEUE_CONTROLLER, ipaddress);
			modelSend.QueueDeclare(serverQueueName, true, false, false, null);
		}

		private void StartListener()
		{
			listener = new BackgroundWorker();
			listener.WorkerSupportsCancellation = true;
			listener.DoWork += Listen;
			listener.RunWorkerAsync();
		}

		private void Listen(object sender, DoWorkEventArgs e)
		{
			while (!e.Cancel)
			{
				BasicGetResult result = null;
				try
				{
					result = modelReceive.BasicGet(serverQueueName, false);
				}
				catch (Exception ex)
				{
					logger.Error("Could not communicate with RabbitMQ server. Exception:\n{0}", ex.Message);
					Thread.Sleep(4000);
					ReopenConnection();
				}

				if (result != null)
				{
					modelReceive.BasicAck(result.DeliveryTag, false);
					string body = encoding.GetString(result.Body);
					string action = encoding.GetString((byte[])result.BasicProperties.Headers["Action"]);
					string replyQueue = String.Empty;
					if (result.BasicProperties.Headers.Contains("ReplyQueue"))
					{
						replyQueue = encoding.GetString((byte[])result.BasicProperties.Headers["ReplyQueue"]);
					}
					logger.Trace("Received Action: {0}\nBody:\n{1}", action, body);

					try
					{
						ProcessAction(action, body, replyQueue);
						logger.Trace("Done: {0}", action);
					}
					catch (Exception ex)
					{
						logger.Error("Could not process message.\nAction: {0}\nBody:\n{1}\nException:\n{2}", action, body, ex.Message);
					}
				}
				else
				{
					Thread.Sleep(1000);
				}
			}
		}

		private bool ReopenConnection()
		{
			bool success = true;

			lock (connection)
			{
				if (connection.IsOpen == true)
				{
					try
					{
						CloseConnection();
					}
					catch { }
				}

				if (connection.IsOpen == false)
				{
					try
					{
						OpenConnection(config.RabbitMq.HostName, config.RabbitMq.Port, config.RabbitMq.UserName, config.RabbitMq.Password);
					}
					catch
					{
						logger.Error("Cannot reopen connection to RabbitMQ: " + config.RabbitMq.HostName);
						return false;
					}
				}

				if (modelReceive.IsOpen == false || modelSend.IsOpen == false)
				{
					try
					{
						if (modelReceive.IsOpen == false)
						{
							modelReceive = connection.CreateModel();
						}

						if (modelSend.IsOpen == false)
						{
							modelSend = connection.CreateModel();
						}
					}
					catch
					{
						return false;
					}
				}

			}

			try
			{
				RegisterQueues();
			}
			catch
			{
				return false;
			}

			return success;
		}

		public void Close()
		{
			IsOpen = false;

			StopListener();
			CloseConnection();
		}

		private void StopListener()
		{
			listener.CancelAsync();
		}

		private void CloseConnection()
		{
			if (modelSend.IsOpen)
				modelSend.Close();

			if (connection.IsOpen)
				connection.Close();
		}

		private void PublishToClients(string body, string action)
		{
			modelSend.PublishToExchange(QueueNames.EXCHANGE_NODE, body, action);
			logger.Trace("Sent Action to Clients: {0}\nBody:\n{1}", action, body);
		}

		private bool PublishToJob(string jobID, string body, string action)
		{
			bool result;
			string exchangename = String.Format(QueueNames.EXCHANGE_JOB, jobID);

			try
			{
				modelSend.ExchangeDeclarePassive(exchangename);
				result = true;
			}
			catch
			{
				result = false;
			}

			if (result)
			{
				modelSend.PublishToExchange(exchangename, body, action);
				logger.Trace("Sent Action to Job {0}: {1}\nBody:\n{2}", jobID, action, body);
			}
			return result;
		}

		private void DeclareJobExchange(string jobID, List<string> queueNames)
		{
			string exchangeName = String.Format(QueueNames.EXCHANGE_JOB, jobID);
			modelSend.ExchangeDeclare(exchangeName, "fanout", true);

			foreach (string queueName in queueNames)
			{
				modelSend.QueueBind(queueName, exchangeName, jobID);
			}
		}

		private void DeleteJobExchange(string jobID)
		{
			string exchangeName = String.Format(QueueNames.EXCHANGE_JOB, jobID);
			modelSend.ExchangeDelete(exchangeName, true);
		}

		private void ReplyToAdmin(string body, string action, string replyQueue)
		{
			modelSend.PublishToQueue(replyQueue, body, action);
			logger.Trace("Sent Action to Admin: {0}\nBody:\n{1}", action, body);
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
					logger.Error(String.Format("Received unknown action {0}", action));
					break;
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
				response.Message = String.Format("Job {0} does not exist.", request.JobID);
			}
			else
			{
				bool result = PublishToJob(request.JobID, request.Serialize(), "StopPeach");

				if (result == false)
				{
					response.Success = false;
					response.Message = String.Format("Cannot stop job {0}", request.JobID);
				}
			}
			ReplyToAdmin(response.Serialize(), "StopPeach", replyQueue);

		}

		private void StartPeach(StartPeachRequest request, string replyQueue)
		{
			string action = "StartPeach";
			request.JobID = CreateJobID();
			request.MongoDbConnectionString = config.MongoDb.ConnectionString;

			while (Common.Mongo.DatabaseHelper.GetJob(request.JobID, request.MongoDbConnectionString) != null)
			{
				request.JobID = CreateJobID();
			}

			#region Create Job record in Mongo

			PeachFarm.Common.Mongo.Job mongoJob = new PeachFarm.Common.Mongo.Job();
			mongoJob.JobID = request.JobID;
			mongoJob.UserName = request.UserName;
			mongoJob.PitFileName = request.PitFileName;
			mongoJob.StartDate = DateTime.Now;
			mongoJob = mongoJob.DatabaseInsert(request.MongoDbConnectionString);

			#endregion

			StartPeachResponse response = new StartPeachResponse(request);


			if ((request.ClientCount == 1) && (request.IPAddress.Length > 0))
			{
				if ((nodes.ContainsKey(request.IPAddress)) && (nodes[request.IPAddress].Status == Status.Alive))
				{
					DeclareJobExchange(request.JobID, new List<string>() { nodes[request.IPAddress].QueueName });

					//modelSend.PublishToQueue(nodes[request.IPAddress].QueueName, request.Serialize(), action);
					PublishToJob(request.JobID, request.Serialize(), action);
					ReplyToAdmin(response.Serialize(), action, replyQueue);
				}
				else
				{
					response.JobID = String.Empty;
					response.Success = false;
					response.Message = String.Format("No Alive Node running at IP address {0}\n", request.IPAddress);
					ReplyToAdmin(response.Serialize(), action, replyQueue);
				}
			}
			else
			{
				var aliveQueues = new List<string>();
				if (String.IsNullOrEmpty(request.Tags))
				{
					var matches = (from Heartbeat h in nodes.Values where h.Status == Status.Alive select h.QueueName as string);
					aliveQueues.AddRange(matches);
				}
				else
				{
					var ts = request.Tags.Split(',').ToList();
					foreach (string computerName in nodes.Keys)
					{
						var matches = tags[computerName].Intersect(ts);
						if ((matches.Count() == ts.Count) && (nodes[computerName].Status == Status.Alive))
						{
							aliveQueues.Add(nodes[computerName].QueueName);
						}
					}
				}



				if ((aliveQueues.Count > 0) && (aliveQueues.Count >= request.ClientCount))
				{
					if (request.ClientCount <= 0)
					{
						request.ClientCount = aliveQueues.Count;
					}

					var jobQueues = aliveQueues.Take(request.ClientCount).ToList();

					DeclareJobExchange(request.JobID, jobQueues);


					foreach(string jobQueue in jobQueues)
					{
						request.Seed = (uint)DateTime.Now.Ticks & 0x0000FFFF;
						modelSend.PublishToQueue(jobQueue, request.Serialize(), action);
					}

					//PublishToJob(request.JobID, request.Serialize(), action);

					ReplyToAdmin(response.Serialize(), action, replyQueue);
				}
				else
				{
					response.JobID = String.Empty;
					response.Success = false;
					response.Message = String.Format("Not enough Alive nodes available, current available: {0}\n", aliveQueues.Count);
					ReplyToAdmin(response.Serialize(), action, replyQueue);
				}
			}
		}

		private void HeartbeatReceived(Heartbeat heartbeat)
		{
			if (nodes.ContainsKey(heartbeat.NodeName) == false)
			{
				AddNode(heartbeat);
			}
			else
			{
				if (heartbeat.Status == Status.Stopping)
				{
					RemoveNode(heartbeat);
				}
				else
				{
					UpdateNode(heartbeat);
				}
			}

			if (heartbeat.Status == Status.Error)
			{
				//errors.Add(heartbeat);
				heartbeat.DatabaseInsert(config.MongoDb.ConnectionString);
				logger.Warn("{0} errored at {1}\n{2}", heartbeat.NodeName, heartbeat.Stamp, heartbeat.ErrorMessage);
			}
		}

		private void ListNodes(ListNodesRequest request, string replyQueue)
		{
			ListNodesResponse response = new ListNodesResponse();
			response.Nodes = nodes.Values.ToList();
			ReplyToAdmin(response.Serialize(), "ListNodes", replyQueue);
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
			ReplyToAdmin(response.Serialize(), "ListErrors", replyQueue);
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
				response.Nodes = (from Heartbeat h in nodes.Values where (h.Status == Status.Running) && (h.JobID == request.JobID) select h).ToList();
			}

			ReplyToAdmin(response.Serialize(), "JobInfo", replyQueue);
		}

		private void Monitor(MonitorRequest request, string replyQueue)
		{
			MonitorResponse response = new MonitorResponse();

			response.Nodes = nodes.Values.ToList();
			response.Jobs = response.Nodes.GetJobs(config.MongoDb.ConnectionString).ToMessagesJobs();
			response.Errors = response.Nodes.GetErrors(config.MongoDb.ConnectionString);

			ReplyToAdmin(response.Serialize(), "Monitor", replyQueue);
		}
		#endregion

		private string CreateJobID()
		{
			using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
			{
				// change the size of the array depending on your requirements
				var rndBytes = new byte[6];
				rng.GetBytes(rndBytes);
				return BitConverter.ToString(rndBytes).Replace("-", "");
			}
		}

		private void AddNode(Heartbeat heartbeat)
		{
			lock (nodes)
			{
				nodes.Add(heartbeat.NodeName, heartbeat);
				logger.Info("{0}\t{1}\t{2}", heartbeat.NodeName, heartbeat.Status.ToString(), heartbeat.Stamp);

				if (String.IsNullOrEmpty(heartbeat.Tags) == false)
				{
					tags[heartbeat.NodeName] = heartbeat.Tags.Split(',').ToList();
				}

			}
		}

		private void UpdateNode(Heartbeat heartbeat)
		{
			if (nodes.ContainsKey(heartbeat.NodeName) == false)
			{
				AddNode(heartbeat);
			}

			nodes[heartbeat.NodeName] = heartbeat;
			logger.Debug("{0}\t{1}\t{2}", heartbeat.NodeName, heartbeat.Status.ToString(), heartbeat.Stamp);

		}

		private void RemoveNode(Heartbeat heartbeat)
		{
			lock (nodes)
			{
				nodes.Remove(heartbeat.NodeName);

				if (tags.ContainsKey(heartbeat.NodeName))
				{
					tags.Remove(heartbeat.NodeName);
				}


				logger.Info("{0}\t{1}\t{2}", heartbeat.NodeName, heartbeat.Status.ToString(), heartbeat.Stamp);

			}
		}
	}
}
