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
			#endregion
			Environment.CurrentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

			#region node state
			nodeState = new NodeState((Configuration.NodeSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.node"));
			#endregion

			
			rabbit = new RabbitMqHelper(nodeState.RabbitMq.HostName, nodeState.RabbitMq.Port, nodeState.RabbitMq.UserName, nodeState.RabbitMq.Password, nodeState.RabbitMq.SSL);
			rabbit.MessageReceived += new EventHandler<RabbitMqHelper.MessageReceivedEventArgs>(rabbit_MessageReceived);
			rabbit.StartListener(nodeState.ClientQueueName);

			heartbeat = new System.Timers.Timer(10000);
			heartbeat.Elapsed += (o, e) => { SendHeartbeat(); };
			heartbeat.Start();

			nlog.Info("Peach Farm Node connected.\nController: {0}\nRabbitMQ: {1}\nDirectory: {2}", nodeState.ServerQueueName, nodeState.RabbitMq.HostName, Environment.CurrentDirectory);
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
				//rabbit.CloseConnection();
				#endregion

			}
			catch (Exception ex)
			{
				nlog.Error(String.Format("Error while shutting down node, continuing shutdown.\nException:\n{0}", ex.ToString()));
			}
		}
		#endregion

		#region Termination handlers
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
				case Actions.StartPeach:
					StartPeach(StartPeachRequest.Deserialize(body));
					break;
				case Actions.StopPeach:
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
				if ((request != null) && (request.JobID != nodeState.StartPeachRequest.JobID))
					return;
					
				StopFuzzer();
				ChangeStatus(Status.Alive);
			}
		}

		private void StartPeach(StartPeachRequest request)
		{
			if (nodeState.Status == Status.Running)
			{
				SendHeartbeat(CreateHeartbeat(String.Format("Node {0} is already running Job {1}", nodeState.IPAddress, nodeState.StartPeachRequest.JobID)));
				return;
			}

			nodeState.StartPeachRequest = request;

			ChangeStatus(Status.Running);

			nodeState.PitFilePath = WriteTextToTempFile(request.Pit, nodeState.StartPeachRequest.JobID, request.PitFileName + ".xml");
			
			if(String.IsNullOrEmpty(request.Defines) == false)
			{
				nodeState.DefinesFilePath = WriteTextToTempFile(request.Defines, nodeState.StartPeachRequest.JobID, request.PitFileName + ".xml.config");
			}

			if (String.IsNullOrEmpty(request.ZipFile) == false)
			{
				string localfile = Path.Combine(Path.GetDirectoryName(nodeState.PitFilePath), nodeState.StartPeachRequest.PitFileName + ".zip");
				PeachFarm.Common.Mongo.DatabaseHelper.DownloadFromGridFS(localfile, request.ZipFile, nodeState.StartPeachRequest.MongoDbConnectionString);
				var zip = Ionic.Zip.ZipFile.Read(localfile);
				zip.ExtractAll(Path.GetDirectoryName(nodeState.PitFilePath), Ionic.Zip.ExtractExistingFileAction.DoNotOverwrite);
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

			Environment.CurrentDirectory = Path.GetDirectoryName(nodeState.PitFilePath);
			string jobid = nodeState.StartPeachRequest.JobID;
			try
			{
				Dictionary<string, object> parserArgs = new Dictionary<string, object>();
				Dictionary<string, string> defines = new Dictionary<string, string>();
				if (String.IsNullOrEmpty(nodeState.DefinesFilePath) == false)
				{
					defines = ProcessDefines(nodeState.DefinesFilePath);
				}
				defines.Add("Peach.Cwd", Environment.CurrentDirectory);
				parserArgs[Peach.Core.Analyzers.PitParser.DEFINED_VALUES] = defines;
				dom = pitParser.asParser(parserArgs, nodeState.PitFilePath);
			}
			catch (Peach.Core.PeachException ex)
			{
				string message = "Peach Exception on Pit parse:\n" + ex.ToString();
				StopPeach(message);
				return;
			}

			List<Peach.Core.Logger> loggers = new List<Peach.Core.Logger>();


			if (String.IsNullOrEmpty(nodeState.StartPeachRequest.MongoDbConnectionString) == false)
			{
				Dictionary<string, Peach.Core.Variant> mongoargs = new Dictionary<string, Peach.Core.Variant>();
				mongoargs.Add("MongoDbConnectionString", new Peach.Core.Variant(nodeState.StartPeachRequest.MongoDbConnectionString));
				mongoargs.Add("JobID", new Peach.Core.Variant(nodeState.StartPeachRequest.JobID));
				mongoargs.Add("UserName", new Peach.Core.Variant(nodeState.StartPeachRequest.UserName));
				mongoargs.Add("PitFileName", new Peach.Core.Variant(nodeState.StartPeachRequest.PitFileName));

				loggers.Add(new Loggers.MongoLogger(mongoargs));
			}


			foreach (var test in dom.tests.Values)
			{
				test.loggers = loggers;
			}

			Peach.Core.RunConfiguration config = new Peach.Core.RunConfiguration();
			config.pitFile = nodeState.PitFilePath;
			config.debug = false;
			if (nodeState.StartPeachRequest.Seed > 0)
			{
				config.randomSeed = nodeState.StartPeachRequest.Seed;
			}

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
			finally
			{
				Environment.CurrentDirectory = nodeState.RootDirectory;
				string temppath = Path.Combine(Environment.CurrentDirectory, "jobtmp", jobid);
				if (Directory.Exists(temppath))
				{
					try
					{
						Directory.Delete(temppath, true);
					}
					catch { }
				}
			}
		}
		#endregion

		#region Peach Event Handlers
		void peach_TestStarting(Peach.Core.RunContext context)
		{
			nlog.Info("Test Starting: {0} | Seed: {0}", nodeState.StartPeachRequest.JobID, context.config.randomSeed.ToString());
			nodeState.RunContext = context;
		}

		void peach_TestFinished(Peach.Core.RunContext context)
		{
			nlog.Info("Test Finished: " + nodeState.StartPeachRequest.JobID);
			StopPeach();
		}

		void peach_TestError(Peach.Core.RunContext context, Exception e)
		{
			string message = String.Format("Test Error: {0}\n{1}", nodeState.StartPeachRequest.JobID, e.Message);
			StopPeach(message);
		}
		#endregion

		private Heartbeat CreateHeartbeat()
		{
			Heartbeat heartbeat = new Heartbeat();
			heartbeat.NodeName = nodeState.IPAddress;
			if (nodeState.Status == Common.Messages.Status.Running)
			{
				heartbeat.JobID = nodeState.StartPeachRequest.JobID;
				heartbeat.UserName = nodeState.StartPeachRequest.UserName;
				heartbeat.PitFileName = nodeState.StartPeachRequest.PitFileName;
				if (nodeState.RunContext != null)
				{
					if (nodeState.RunContext.config != null)
						heartbeat.Seed = nodeState.RunContext.config.randomSeed;

					if(nodeState.RunContext.test.strategy != null)
						heartbeat.Iteration = nodeState.RunContext.test.strategy.Iteration;
				}
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

		private string WriteTextToTempFile(string text, string jobID, string fileName = "")
		{
			if (text.StartsWith("<!"))
			{
				text = text.Substring("<![CDATA[".Length);
				text = text.Substring(0, text.Length - 2);
			}

			if (String.IsNullOrEmpty(fileName))
			{
				fileName = Path.GetFileName(Path.GetTempFileName());
			}

			string temppath	= Path.Combine(Environment.CurrentDirectory, "jobtmp", jobID, fileName);
			PeachFarm.Common.FileWriter.CreateDirectory(Path.GetDirectoryName(temppath));
			File.WriteAllText(temppath, text);
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

			RootDirectory = Environment.CurrentDirectory;
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
							this.StartPeachRequest = null;
							RunContext = null;
							break;
					}
				}
			}
		}

		internal string Tags { get; private set; }

		internal RabbitMqElement RabbitMq { get; private set; }

		internal StartPeachRequest StartPeachRequest { get; set; }

		internal string IPAddress { get; private set; }

		internal string ClientQueueName { get; private set; }
		internal string ServerQueueName { get; private set; }

		internal Peach.Core.RunContext RunContext { get; set; }

		internal string RootDirectory { get; set; }

		internal string PitFilePath { get; set; }
		internal string DefinesFilePath { get; set; }
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
