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

using PeachFarm.Common;
using PeachFarm.Common.Messages;
using System.Net;
using PeachFarm.Node.Configuration;

namespace PeachFarm.Node
{
	public class PeachFarmNode
	{
		private static UTF8Encoding encoding = new UTF8Encoding();
 		private static System.Timers.Timer heartbeat;
		private static NodeState nodeState;
		private static Peach.Core.Engine peach;

		private static NLog.Logger nlog = NLog.LogManager.GetCurrentClassLogger();

		private RabbitMqHelper rabbit;

		public PeachFarmNode()
		{
			#region trap unhandled exceptions and Ctrl-C
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
			AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
			#endregion

			#region node state
			nodeState = new NodeState((Configuration.NodeSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.node"));


			#endregion

			Directory.SetCurrentDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));

			rabbit = new RabbitMqHelper(nodeState.RabbitMq.HostName, nodeState.RabbitMq.Port, nodeState.RabbitMq.UserName, nodeState.RabbitMq.Password);
			rabbit.MessageReceived += new EventHandler<RabbitMqHelper.MessageReceivedEventArgs>(rabbit_MessageReceived);
			rabbit.StartListener(nodeState.ClientQueueName);

			heartbeat = new System.Timers.Timer(10000);
			heartbeat.Elapsed += (o, e) => { SendHeartbeat(); };
			heartbeat.Start();
		}

		void rabbit_MessageReceived(object sender, RabbitMqHelper.MessageReceivedEventArgs e)
		{
			if (nodeState.Status != Common.Messages.Status.Stopping)
			{
				try
				{
					ProcessAction(e.Action, e.Body);
				}
				catch (Exception ex)
				{
					SendHeartbeat(CreateHeartbeat(ex.Message));
					nlog.Error(ex.Message);
				}
			}
		}

		#region Properties
		public Status Status
		{
			get { return nodeState.Status; }
		}

		public string ServerQueue
		{
			get { return nodeState.ServerQueueName; }
		}

		#endregion

		#region Events
		public event EventHandler<StatusChangedEventArgs> StatusChanged;

		private void ChangeStatus(Status newStatus)
		{
			if (nodeState.Status != newStatus)
			{
				nodeState.Status = newStatus;
				if (StatusChanged != null)
				{
					StatusChanged(this, new StatusChangedEventArgs(nodeState.Status));
				} 
				SendHeartbeat(null);
			}
		}
		#endregion

		#region Public Methods
		public void Close()
		{
			try
			{
				#region stop heartbeat
				heartbeat.Stop();
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
				rabbit.StopListener();
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
			if ((nodeState.Status != Status.Stopping))// && (connection != null) && (connection.IsOpen))
			{
				ChangeStatus(Status.Stopping);
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

			if ((nodeState.Status != Status.Stopping))// && (connection != null) && (connection.IsOpen) && e.IsTerminating)
			{
				ChangeStatus(Status.Stopping);

				try
				{
					rabbit.StopListener();
				}
				catch (RabbitMqException) { }
			}
		}
		#endregion

		#region Rabbit MQ Functions
		private void PublishToServer(string body, string action)
		{
			rabbit.PublishToQueue(ServerQueue, body, action);
		}
		#endregion

		#region Sends
		private void SendHeartbeat(Heartbeat heartbeat = null)
		{
			if (heartbeat == null)
			{
				heartbeat = CreateHeartbeat();
			}
			nlog.Trace("HEARTBEAT: {0}", heartbeat.Stamp);
			try
			{
				PublishToServer(heartbeat.Serialize(), "Heartbeat");
			}
			catch(RabbitMqException rex)
			{
				nlog.Error(String.Format("Could not send heartbeat to RabbitMQ {0}.\nException:\n{1}", heartbeat.Stamp.ToString(), rex.ToString()));

				if (heartbeat.Status == Common.Messages.Status.Error)
				{
					nlog.Error(String.Format("Could not send error message to RabbitMQ.\n{0}", heartbeat.ErrorMessage));
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
					throw new ApplicationException(message);
			}
		}

		private void StopPeach(string errorMessage)
		{
			if (nodeState.Status == Status.Running)
			{
				SendHeartbeat(CreateHeartbeat(errorMessage));
				nlog.Error(errorMessage);
				StopPeach();
			}
		}

		private void StopPeach(StopPeachRequest request = null)
		{
			if (nodeState.Status == Status.Running)
			{
				if ((request != null) && (request.JobID != nodeState.JobID))
					return;
					
				StopFuzzer();
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
			heartbeat.QueueName = nodeState.ClientQueueName;
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

		private void StopFuzzer()
		{
			if (nodeState.RunContext != null)
			{
				if (nodeState.RunContext.continueFuzzing)
				{
					nodeState.RunContext.continueFuzzing = false;
				}
				nodeState.RunContext = null;
			}
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
		public NodeState(PeachFarm.Node.Configuration.NodeSection config)
		{
			Status = Common.Messages.Status.Alive;

			ServerQueueName = String.Format(QueueNames.QUEUE_CONTROLLER, config.Controller.IpAddress);
			
			if ((config.Tags != null) && (config.Tags.Count > 0))
			{
				Tags = config.Tags.ToString();
			}

			#region get ip address, used for client name
			IPAddress[] ipaddresses = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
			IPAddress = (from i in ipaddresses where i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork select i).First().ToString();
			#endregion

			ClientQueueName = String.Format(QueueNames.QUEUE_NODE, IPAddress);

			RabbitMq = config.RabbitMq;
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
							JobID = String.Empty;
							UserName = "";
							PitFileName = String.Empty;
							RunContext = null;
							break;
					}
				}
			}
		}

		internal RabbitMqElement RabbitMq { get; private set; }

		internal string IPAddress { get; private set; }

		internal string ClientQueueName { get; private set; }
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
}
