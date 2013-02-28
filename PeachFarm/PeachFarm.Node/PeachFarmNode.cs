using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Xml;
using System.Xml.XPath;

using RabbitMQ.Client;

using PeachFarm.Common;
using PeachFarm.Common.Messages;
using System.Runtime.InteropServices;
using System.Net;

namespace PeachFarm.Node
{
	public class PeachFarmNode
	{
		private UTF8Encoding encoding = new UTF8Encoding();

		//private Timer fileSyncTimer;
		private Timer heartbeat;

		private NodeState nodeState;

		private Peach.Core.Engine peach;

		private Configuration.NodeSection config;

		private static NLog.Logger nlog = NLog.LogManager.GetCurrentClassLogger();

		public PeachFarmNode()
		{
			config = (Configuration.NodeSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.node");

			#region trap unhandled exceptions and Ctrl-C
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
			AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
			#endregion

			#region node state
			nodeState = new NodeState(String.Format(QueueNames.QUEUE_CONTROLLER, config.Controller.IpAddress));

			if ((config.Tags != null) && (config.Tags.Count > 0))
			{
				nodeState.Tags = config.Tags.ToString();
			}
			#endregion

			Directory.SetCurrentDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));
		}

		#region Properties
		public bool IsOpen { get; private set; }

		public Status Status
		{
			get { return nodeState.Status; }
		}

		public string ServerQueue
		{
			get { return nodeState.ServerQueueName; }
		}

		public Configuration.NodeSection Configuration
		{
			get { return config; }
		}
		#endregion

		#region Events
		public event EventHandler<StatusChangedEventArgs> StatusChanged;

		private void ChangeStatus(Status newStatus)
		{
			if (this.nodeState.Status != newStatus)
			{
				this.nodeState.Status = newStatus;
				if (StatusChanged != null)
				{
					StatusChanged(this, new StatusChangedEventArgs(this.nodeState.Status));
				} 
				SendHeartbeat(null);
			}
		}
		#endregion

		#region Public Methods
		public void StartNode()
		{

			#region set up RabbitMQ connection and start listening for messages
			try
			{
				OpenConnection(config.RabbitMq.HostName, config.RabbitMq.Port, config.RabbitMq.UserName, config.RabbitMq.Password);
				RegisterQueues();
				StartListener();
				heartbeat = new Timer(new TimerCallback(SendHeartbeat), null, TimeSpan.FromMilliseconds(0), TimeSpan.FromSeconds(10));
				IsOpen = true;
			}
			catch(RabbitMqException rex)
			{
				if (heartbeat != null)
				{
					heartbeat.Dispose();
					heartbeat = null;
				}
				StopListeners();
				nlog.Fatal(String.Format("Could not open connection to RabbitMQ server at {0}, exiting now.\nException:\n{1}", config.RabbitMq.HostName, rex.InnerException.ToString()));
			}
			catch(Exception ex)
			{
				nlog.Fatal(String.Format("Unknown exception while starting Peach Farm Node:\n{0}", ex.Message));
				return;
			}

			#endregion


		}

		public void StopNode()
		{
			IsOpen = false;

			try
			{
				#region stop heartbeat
				heartbeat.Dispose();
				#endregion

				#region stop the currently running peach job
				if (nodeState.Status == Status.Running)
				{
					StopPeach();
				}
				#endregion

				#region send a stopping message to controller
				ChangeStatus(Status.Stopping);
				#endregion

				#region deregister from RabbitMQ
				DeregisterQueues();
				StopListeners();
				CloseConnection();
				#endregion

			}
			catch (Exception ex)
			{
				nlog.Error(String.Format("Error while shutting down node, continuing shutdown.\nException:\n{0}", ex.ToString()));
			}
		}
		#endregion

		#region Termination handlers

		private void CurrentDomain_ProcessExit(object sender, EventArgs e)
		{
			if ((nodeState.Status != Status.Stopping) && (connection != null) && (connection.IsOpen))
			{
				ChangeStatus(Status.Stopping);

				try
				{
					DeregisterQueues();
					StopListeners();
					CloseConnection();
				}
				catch (RabbitMqException) { }
			}
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject is RabbitMqException)
			{
				nlog.Fatal("Unhandled RabbitMq Exception, please file a bug.\nException:\n{0}", ((RabbitMqException)e.ExceptionObject).InnerException.ToString());
			}
			else if (e.ExceptionObject is Exception)
			{
				Heartbeat error = CreateHeartbeat(String.Format("Peach Farm Node unhandled exception:\n{0}", ((Exception)e.ExceptionObject).Message));
				SendHeartbeat(error);
			}

			if ((nodeState.Status != Status.Stopping) && (connection != null) && (connection.IsOpen) && e.IsTerminating)
			{
				ChangeStatus(Status.Stopping);

				try
				{
					DeregisterQueues();
					StopListeners();
					CloseConnection();
				}
				catch (RabbitMqException) { }
			}
		}
		#endregion

		#region Node communication functions

		private static string clientQueueName;

		private static IConnection connection;
		private static IModel modelSend;
		private static IModel modelReceive;

		private static BackgroundWorker listener = new BackgroundWorker();

		private static void OpenConnection(string hostName, int port = -1, string userName = "guest", string password = "guest")
		{
			ConnectionFactory factory = new ConnectionFactory();
			factory.HostName = hostName;
			factory.Port = port;
			factory.UserName = userName;
			factory.Password = password;

			try
			{
				connection = factory.CreateConnection();
				modelSend = connection.CreateModel();
				modelReceive = connection.CreateModel();
			}
			catch(Exception ex)
			{
				throw new RabbitMqException(ex);
			}

		}

		private void RegisterQueues()
		{

			//client queue
			clientQueueName = String.Format(QueueNames.QUEUE_NODE, nodeState.IPAddress);

			try
			{
				modelSend.ExchangeDeclare(QueueNames.EXCHANGE_NODE, QueueNames.EXCHANGETYPE_NODE);
				modelSend.QueueDeclare(clientQueueName, false, false, false, null);
				modelSend.QueueBind(clientQueueName, QueueNames.EXCHANGE_NODE, "");
				modelSend.QueuePurge(clientQueueName);
			}
			catch (Exception ex)
			{
				throw new RabbitMqException(ex);
			}
		}

		private void DeregisterQueues()
		{
			if (modelSend.IsOpen)
			{
				try
				{
					modelSend.QueueUnbind(clientQueueName, QueueNames.EXCHANGE_NODE, "", null);
					modelSend.QueueDelete(clientQueueName);
				}
				catch (Exception ex)
				{
					throw new RabbitMqException(ex);
				}
			}
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
				if ((nodeState.Status == Status.Alive) || (nodeState.Status == Status.Running))
				{
					BasicGetResult result = null;
					try
					{
						result = GetFromNodeQueue();
					}
					catch(RabbitMqException)
					{
						nlog.Error("Can't communicate with RabbitMQ server, will retry.");
					}

					if (result != null)
					{
						string body = encoding.GetString(result.Body);
						string action = encoding.GetString((byte[])result.BasicProperties.Headers["Action"]);
						nlog.Debug(String.Format("Action: {0}", action));
						nlog.Trace(String.Format("Body:\n'{0}'", body));
						try
						{
							ProcessAction(action, body);
						}
						catch (Exception ex)
						{
							Heartbeat heartbeat = CreateHeartbeat(String.Format("{0}\n\n{1}", result.Body, ex.Message));
							SendHeartbeat(heartbeat);
						}

						try
						{
							AckMessage(result.DeliveryTag);
						}
						catch { }

						nlog.Debug(String.Format("Done processing message '{0}'", action));
					}
				}

				Thread.Sleep(1000);
			}
		}

		private void StopListeners()
		{
			if(listener.IsBusy)
				listener.CancelAsync();
		}

		private void CloseConnection()
		{
			if (modelSend.IsOpen)
				modelSend.Close();

			if (modelReceive.IsOpen)
				modelReceive.Close();

			if (connection.IsOpen)
				connection.Close();
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
						nlog.Error("Cannot reopen connection to RabbitMQ: " + config.RabbitMq.HostName);
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
		#endregion

		#region Rabbit MQ Functions
		private void PublishToServer(string message, string action)
		{
			bool open = true;

			try
			{
				if (modelSend.IsOpen == false)
				{
					open = ReopenConnection();
				}

				if (open)
				{
					modelSend.PublishToQueue(nodeState.ServerQueueName, message, action);
				}
				else
				{
					throw new RabbitMqException(null);
				}
			}
			catch(Exception ex)
			{
				throw new RabbitMqException(ex);
			}
		}

		private BasicGetResult GetFromNodeQueue()
		{
			bool open = true;
			try
			{
				if ((connection == null) || (connection.IsOpen == false))
				{
					open = ReopenConnection();
				}

				if (open)
				{
					return modelReceive.BasicGet(clientQueueName, false);
				}
				else
				{
					return null;
				}
			}
			catch (Exception ex)
			{
				throw new RabbitMqException(ex);
			}
		}

		private void AckMessage(ulong deliveryTag)
		{
			try
			{
				modelSend.BasicAck(deliveryTag, false);
			}
			catch (Exception ex)
			{
				throw new RabbitMqException(ex);
			}
		}

		#endregion

		#region Sends
		private void SendHeartbeat(object state)
		{
			Heartbeat heartbeat = null;
			if ((state != null) && (state is Heartbeat))
			{
				heartbeat = (Heartbeat)state;
			}
			else
			{
				heartbeat = CreateHeartbeat();
			}

			try
			{
				PublishToServer(heartbeat.Serialize(), "Heartbeat");
			}
			catch
			{
				if (heartbeat.Status == Common.Messages.Status.Error)
				{
					nlog.Error(String.Format("Could not send error message to RabbitMQ.\nException:\n{0}", heartbeat.ErrorMessage));
				}
			}
		}
		#endregion

		#region Message Handlers

		private void ProcessAction(string action, string body)
		{
			switch (action)
			{
				case "StartPeach":
					StartPeach(StartPeachRequest.Deserialize(body));
					break;
				case "StopPeach":
					StopPeach(StopPeachRequest.Deserialize(body));
					break;
				default:
					string message = String.Format("Received unknown action {0}", action);
					nlog.Error(message);
					SendHeartbeat(CreateHeartbeat(message));
					break;
			}
		}

		private void StopPeach()
		{
			if (nodeState.Status == Status.Running)
			{
				if (nodeState.RunContext.continueFuzzing)
				{
					nodeState.RunContext.continueFuzzing = false;
				}
				nodeState.RunContext = null;


				ChangeStatus(Status.Alive);
			}
		}

		private void StopPeach(StopPeachRequest request)
		{
			if ((nodeState.Status == Status.Running) && (request.JobID == nodeState.JobID))
			{
				if (nodeState.RunContext.continueFuzzing)
				{
					nodeState.RunContext.continueFuzzing = false;
				}
				nodeState.RunContext = null;

				ChangeStatus(Status.Alive);
			}
		}

		private void StopPeach(string errorMessage)
		{
			if (String.IsNullOrEmpty(errorMessage))
			{
				StopPeach();
			}
			else
			{
				if (nodeState.RunContext != null)
				{
					if (nodeState.Status == Status.Running)
					{
						if (nodeState.RunContext.continueFuzzing)
						{
							nodeState.RunContext.continueFuzzing = false;
						}
						nodeState.RunContext = null;
					}
				}

				SendHeartbeat(CreateHeartbeat(errorMessage));
				nlog.Error(errorMessage);

				ChangeStatus(Status.Alive);
			}

		}

		private void StartPeach(StartPeachRequest request)
		{
			if (nodeState.Status == Status.Running)
			{
				SendHeartbeat(CreateHeartbeat(String.Format("Node {0} is already running Job {1}", nodeState.IPAddress, nodeState.JobID)));
				return;
			}

			nodeState.MongoDbConnectionString = request.MongoDbConnectionString;
			nodeState.JobID = request.JobID;
			nodeState.PitFileName = request.PitFileName;
			nodeState.UserName = request.UserName;
			nodeState.Seed = request.Seed;

			ChangeStatus(Status.Running);


			nodeState.PitFilePath = WriteTextToTempFile(request.Pit);
			
			if(String.IsNullOrEmpty(request.Defines) == false)
			{
				nodeState.DefinesFilePath = WriteTextToTempFile(request.Defines);
			}


			#region initialize Peach Engine
			peach = new Peach.Core.Engine(null);

			#region context settings
			//peach.context.reproducingMaxBacksearch = 0;	// tell Peach to not replay iterations
			#endregion

			peach.TestStarting += new Peach.Core.Engine.TestStartingEventHandler(peach_TestStarting);
			peach.TestError += new Peach.Core.Engine.TestErrorEventHandler(peach_TestError);
			peach.TestFinished += new Peach.Core.Engine.TestFinishedEventHandler(peach_TestFinished);
			#endregion

			BackgroundWorker peachWorker = new BackgroundWorker();
			peachWorker.DoWork += new DoWorkEventHandler(peachWorker_DoWork);
			peachWorker.RunWorkerAsync();
		}

		#endregion

		#region Peach Background Worker
		void peachWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			Peach.Core.Analyzers.PitParser pitParser = new Peach.Core.Analyzers.PitParser();

			Peach.Core.Dom.Dom dom = null;
			try
			{
				if (String.IsNullOrEmpty(nodeState.DefinesFilePath))
				{
					dom = pitParser.asParser(null, nodeState.PitFilePath);
				}
				else
				{
					Dictionary<string, object> parserArgs = new Dictionary<string, object>();
					parserArgs[Peach.Core.Analyzers.PitParser.DEFINED_VALUES] = ProcessDefines(nodeState.DefinesFilePath);
					dom = pitParser.asParser(parserArgs, nodeState.PitFilePath);
				}
			}
			catch (Peach.Core.PeachException ex)
			{
				string message = "Peach Exception on Pit parse:\n" + ex.ToString();
				StopPeach(message);
				return;
			}

			List<Peach.Core.Logger> loggers = new List<Peach.Core.Logger>();


			if (String.IsNullOrEmpty(nodeState.MongoDbConnectionString) == false)
			{
				Dictionary<string, Peach.Core.Variant> mongoargs = new Dictionary<string, Peach.Core.Variant>();
				mongoargs.Add("MongoDbConnectionString", new Peach.Core.Variant(nodeState.MongoDbConnectionString));
				mongoargs.Add("JobID", new Peach.Core.Variant(nodeState.JobID.ToString()));
				mongoargs.Add("UserName", new Peach.Core.Variant(nodeState.UserName));
				mongoargs.Add("PitFileName", new Peach.Core.Variant(nodeState.PitFileName));

				loggers.Add(new Loggers.MongoLogger(mongoargs));
			}


			foreach (var test in dom.tests.Values)
			{
				//TODO
				test.loggers = loggers;
			}

			Peach.Core.RunConfiguration config = new Peach.Core.RunConfiguration();
			config.pitFile = nodeState.PitFilePath;
			config.debug = false;
			if (nodeState.Seed > 0)
			{
				config.randomSeed = nodeState.Seed;
			}
			//config.runName = clientState.JobID.ToString();

			try
			{
				peach.startFuzzing(dom, config);
			}
			catch (Peach.Core.PeachException pex)
			{
				StopPeach("PeachException:\n" + pex.ToString());
			}
			catch (Exception ex)
			{
				StopPeach("Unknown Exception from Peach:\n" + ex.Message);
			}
		}
		#endregion

		#region Peach Event Handlers
		void peach_TestStarting(Peach.Core.RunContext context)
		{
			nlog.Info("Test Starting: " + nodeState.JobID.ToString());
			nodeState.RunContext = context;
		}

		void peach_TestFinished(Peach.Core.RunContext context)
		{
			nlog.Info("Test Finished: " + nodeState.JobID.ToString());
			StopPeach();
		}

		void peach_TestError(Peach.Core.RunContext context, Exception e)
		{
			string message = String.Format("Test Error: {0}\n{1}", nodeState.JobID.ToString(), e.Message);
			StopPeach(message);
		}
		#endregion

		private Heartbeat CreateHeartbeat()
		{
			Heartbeat heartbeat = new Heartbeat();
			heartbeat.NodeName = nodeState.IPAddress;
			if (nodeState.Status == Common.Messages.Status.Running)
			{
				heartbeat.JobID = nodeState.JobID;
				heartbeat.UserName = nodeState.UserName;
				heartbeat.PitFileName = nodeState.PitFileName;
			}
			heartbeat.Tags = nodeState.Tags;
			heartbeat.QueueName = clientQueueName;
			heartbeat.Stamp = DateTime.Now;
			heartbeat.Status = nodeState.Status;
			return heartbeat;
		}

		private Heartbeat CreateHeartbeat(string errorMessage)
		{
			Heartbeat heartbeat = CreateHeartbeat();
			heartbeat.Status = Common.Messages.Status.Error;
			heartbeat.ErrorMessage = errorMessage;
			return heartbeat;
		}

		private string WriteTextToTempFile(string text)
		{
			if (text.StartsWith("<!"))
			{
				text = text.Substring("<![CDATA[".Length);
				text = text.Substring(0, text.Length - 2);
			}

			string temppath = Path.GetTempFileName();
			using (FileStream stream = new FileStream(temppath, FileMode.Create))
			{
				StreamWriter writer = new StreamWriter(stream);
				writer.Write(text);
				writer.Flush();
				writer.Close();
				stream.Close();
			}
			return temppath;
		}

		#region functions copied from Peach
		private Dictionary<string, string> ProcessDefines(string definedValuesFile)
		{
			Dictionary<string, string> DefinedValues = new Dictionary<string,string>();

			if (definedValuesFile != null)
			{
				if (!File.Exists(definedValuesFile))
					throw new Peach.Core.PeachException("Error, defined values file \"" + definedValuesFile + "\" does not exist.");

				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(definedValuesFile);

				var root = xmlDoc.FirstChild;
				if (root.Name != "PitDefines")
				{
					root = xmlDoc.FirstChild.NextSibling;
					if (root.Name != "PitDefines")
						throw new Peach.Core.PeachException("Error, definition file root element must be PitDefines.");
				}

				foreach (XmlNode node in root.ChildNodes)
				{
					if (hasXmlAttribute(node, "platform"))
					{
						switch (getXmlAttribute(node, "platform").ToLower())
						{
							case "osx":
								if (Peach.Core.Platform.GetOS() != Peach.Core.Platform.OS.OSX)
									continue;
								break;
							case "linux":
								if (Peach.Core.Platform.GetOS() != Peach.Core.Platform.OS.Linux)
									continue;
								break;
							case "windows":
								if (Peach.Core.Platform.GetOS() != Peach.Core.Platform.OS.Windows)
									continue;
								break;
							default:
								throw new Peach.Core.PeachException("Error, unknown platform name \"" + getXmlAttribute(node, "platform") + "\" in definition file.");
						}
					}

					foreach (XmlNode defNode in node.ChildNodes)
					{
						if (defNode is XmlComment)
							continue;

						if (!hasXmlAttribute(defNode, "key") || !hasXmlAttribute(defNode, "value"))
							throw new Peach.Core.PeachException("Error, Define elements in definition file must have both key and value attributes.");

						// Allow command line to override values in XML file.
						if (!DefinedValues.ContainsKey(getXmlAttribute(defNode, "key")))
						{
							DefinedValues[getXmlAttribute(defNode, "key")] =
								getXmlAttribute(defNode, "value");
						}
					}
				}
			}

			return DefinedValues;
		}

		private bool hasXmlAttribute(XmlNode node, string name)
		{
			if (node.Attributes == null)
				return false;

			object o = node.Attributes.GetNamedItem(name);
			return o != null;
		}

		private string getXmlAttribute(XmlNode node, string name)
		{
			System.Xml.XmlAttribute attr = node.Attributes.GetNamedItem(name) as System.Xml.XmlAttribute;
			if (attr != null)
			{
				return attr.InnerText;
			}
			else
			{
				return null;
			}
		}
		#endregion
	}

	internal class NodeState
	{
		public NodeState(string serverQueueName)
		{
			Status = Common.Messages.Status.Alive;

			ServerQueueName = serverQueueName;

			#region get ip address, used for client name
			IPAddress[] ipaddresses = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
			IPAddress = (from i in ipaddresses where i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork select i).First().ToString();
			#endregion

		}

		private Status statusField;

		internal Status Status
		{
			get
			{
				return statusField;
			}
			set
			{
				if (statusField != value)
				{
					statusField = value;
					switch (value)
					{
						case Status.Alive:
							MongoDbConnectionString = String.Empty;
							PitFilePath = String.Empty;
							DefinesFilePath = String.Empty;
							//JobID = Guid.Empty;
							JobID = String.Empty;
							UserName = "";
							PitFileName = String.Empty;
							RunContext = null;
							break;
					}
				}
			}
		}

		internal string IPAddress { get; private set; }
		internal string ServerQueueName { get; private set; }

		internal string MongoDbConnectionString { get; set; }

		internal string PitFilePath { get; set; }
		internal string DefinesFilePath { get; set; }

		internal uint Seed { get; set; }

		internal string PitFileName { get; set; }
		internal Peach.Core.RunContext RunContext { get; set; }
		//internal Guid JobID { get; set; }
		internal string JobID { get; set; }
		internal string UserName { get; set; }
		internal string Tags { get; set; }
	}

	public class StatusChangedEventArgs : EventArgs
	{
		public StatusChangedEventArgs(Status status)
			: base()
		{
			this.Status = status;
		}

		public Status Status { get; private set; }
	}

	public class RabbitMqException : Exception 
	{
		public RabbitMqException(Exception innerException, string message = "Peach Farm Node encountered a RabbitMq Exception.")
			:base(message, innerException)
		{
		}
	}
}
