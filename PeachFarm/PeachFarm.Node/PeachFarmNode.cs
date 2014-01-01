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
using System.Text.RegularExpressions;

namespace PeachFarm.Node
{
	public class PeachHolder : MarshalByRefObject
	{
		public PeachHolder()
		{
			engine = new Peach.Core.Engine(null);
		}

		public Peach.Core.Engine engine { get; private set; }
	}

	public class PeachFarmNode : IDisposable
	{
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

			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();

			if (assembly == null)
			{
				assembly = System.Reflection.Assembly.GetAssembly(typeof(PeachFarmNode));
			}

			Environment.CurrentDirectory = Path.GetDirectoryName(assembly.Location);

			#region node state

			var nodeconfig = (Configuration.NodeSection) System.Configuration.ConfigurationManager.GetSection("peachfarm.node");
			nodeconfig.Validate();
			nodeState = new NodeState(nodeconfig);

			#endregion

			
			rabbit = new RabbitMqHelper(nodeState.RabbitMq.HostName, nodeState.RabbitMq.Port, nodeState.RabbitMq.UserName, nodeState.RabbitMq.Password, nodeState.RabbitMq.SSL);
			rabbit.MessageReceived += new EventHandler<RabbitMqHelper.MessageReceivedEventArgs>(rabbit_MessageReceived);
			rabbit.StartListener(nodeState.NodeQueue);

			heartbeat = new System.Timers.Timer(10000);
			heartbeat.Elapsed += (o, e) => { SendHeartbeat(); };
			heartbeat.Start();

			nlog.Info("Peach Farm Node connected.\nController: {0}\nRabbitMQ: {1}\nDirectory: {2}", nodeState.ControllerQueue, nodeState.RabbitMq.HostName, Environment.CurrentDirectory);
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
			get { return nodeState.ControllerQueue; }
		}

		public bool IsListening
		{
			get { return rabbit.IsListening; }
		}

		public string Version
		{
			get { return nodeState.Version; }
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
					while (nodeState.Status != Status.Alive)
					{
						Thread.Sleep(1000);
					}
				}
				#endregion

				#region send a stopping message to controller
				ChangeStatus(Status.Stopping);
				#endregion

				#region deregister from RabbitMQ
				rabbit.StopListener();
				//rabbit.CloseConnection();
				#endregion

				#region clean up temp files
				var tempfolder = Path.Combine(nodeState.RootDirectory, "jobtmp");
				string[] subfolders = null;
				try
				{
					subfolders = Directory.GetDirectories(tempfolder);
				}
				catch { }

				if (subfolders != null)
				{
					foreach (var subfolder in subfolders)
					{
						try
						{
							Directory.Delete(subfolder, true);
						}
						catch { }
					}
				}
				#endregion

			}
			catch (Exception ex)
			{
				nlog.Error(String.Format("Error while shutting down node, continuing shutdown.\nException:\n{0}", ex.ToString()));
			}
		}
		#endregion

		#region Termination handlers
		void CurrentDomain_ProcessExit(object sender, EventArgs e)
		{
			try
			{
				this.Close();
			}
			catch { }
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
				try
				{
					this.Close();
				}
				catch { }
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
					StartPeachRequest request = null;
					try
					{
						request = StartPeachRequest.Deserialize(body);
						StartPeach(request);
					}
					catch (Exception ex)
					{
						if (request != null)
						{
							StartPeachCleanUp();
						}
						throw ex;
					}
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
			SendHeartbeat(CreateHeartbeat(errorMessage));
			nlog.Error(errorMessage);
			StopPeach();
		}

		private void StopPeach(StopPeachRequest request = null)
		{
			if (nodeState.Status == Status.Running)
			{
				if ((request != null) && (request.JobID != nodeState.StartPeachRequest.JobID))
					return;

				StopFuzzer();

				
			}
		}

		private void StartPeach(StartPeachRequest request)
		{
			if (nodeState.Status == Status.Running)
			{
				SendHeartbeat(CreateHeartbeat(String.Format("Node {0} is already running Job {1}", nodeState.NodeName, nodeState.StartPeachRequest.JobID)));
				return;
			}

			nodeState.StartPeachRequest = request;

			if (PeachFarm.Common.Mongo.DatabaseHelper.GridFSFileExists(nodeState.StartPeachRequest.ZipFile, nodeState.StartPeachRequest.MongoDbConnectionString) == false)
			{
				SendHeartbeat(CreateHeartbeat("Error with StartPeachRequest. Zip file can not be found in database: " + nodeState.StartPeachRequest.ZipFile));
				return;
			}

			string jobtempfolder = Path.Combine(nodeState.RootDirectory, "jobtmp", nodeState.StartPeachRequest.JobID);
			FileWriter.CreateDirectory(jobtempfolder);

			ChangeStatus(Status.Running);

			//nodeState.PitFilePath = WriteTextToTempFile(request.Pit, nodeState.StartPeachRequest.JobID, request.PitFileName + ".xml");
			
			//if(String.IsNullOrEmpty(request.Defines) == false)
			//{
			//  nodeState.DefinesFilePath = WriteTextToTempFile(request.Defines, nodeState.StartPeachRequest.JobID, request.PitFileName + ".xml.config");
			//}

			//if (String.IsNullOrEmpty(request.ZipFile) == false)
			//{
				string localfile = Path.Combine(jobtempfolder, nodeState.StartPeachRequest.PitFileName + ".zip");

				PeachFarm.Common.Mongo.DatabaseHelper.DownloadFromGridFS(localfile, nodeState.StartPeachRequest.ZipFile, nodeState.StartPeachRequest.MongoDbConnectionString);
				var zip = Ionic.Zip.ZipFile.Read(localfile);
				zip.ExtractAll(jobtempfolder, Ionic.Zip.ExtractExistingFileAction.DoNotOverwrite);

				//new
				nodeState.PitFilePath = Path.Combine(jobtempfolder, nodeState.StartPeachRequest.PitFileName + ".xml");

				if (nodeState.StartPeachRequest.HasDefines)
					nodeState.DefinesFilePath = Path.Combine(jobtempfolder, nodeState.StartPeachRequest.PitFileName + ".xml.config");
			//}

			#region initialize Peach Engine

			peach = new Peach.Core.Engine(null);
			//// Create
			//var domain = System.AppDomain.CreateDomain(request.JobID);
			//domain.UnhandledException += new UnhandledExceptionEventHandler(domain_UnhandledException);
			//var holder = (PeachHolder)domain.CreateInstanceFrom("Peaches/3.1.20/PeachFrarm.Node.dll", "PeachFarm.Node.PeachHolder").Unwrap();
			//peach = holder.engine;

			//// Destroy
			//peach = null;
			//System.AppDomain.Unload(domain);

			#region context settings
			//peach.context.reproducingMaxBacksearch = 0;	// tell Peach to not replay iterations
			#endregion

			peach.TestStarting += new Peach.Core.Engine.TestStartingEventHandler(peach_TestStarting);
			peach.TestError += new Peach.Core.Engine.TestErrorEventHandler(peach_TestError);
			peach.TestFinished += new Peach.Core.Engine.TestFinishedEventHandler(peach_TestFinished);
			#endregion

			BackgroundWorker peachWorker = new BackgroundWorker();
			peachWorker.DoWork += new DoWorkEventHandler(peachWorker_DoWork);
			peachWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(peachWorker_RunWorkerCompleted);
			peachWorker.RunWorkerAsync();
		}
		#endregion

		#region Peach Background Worker
		void peachWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			#region Parse Pit and Defines
			Peach.Core.Analyzers.PitParser pitParser = new Peach.Core.Analyzers.PitParser();

			Peach.Core.Dom.Dom dom = null;

			Environment.CurrentDirectory = Path.GetDirectoryName(nodeState.PitFilePath);
			Dictionary<string, object> parserArgs = new Dictionary<string, object>();
			Dictionary<string, string> defines = new Dictionary<string, string>();

			if (String.IsNullOrEmpty(nodeState.DefinesFilePath) == false)
			{
				try
				{
					defines = Peach.Core.Analyzers.PitParser.parseDefines(nodeState.DefinesFilePath);
				}
				catch (Exception ex)
				{
					string message = "Exception on Defines parse:\n" + ex.ToString();
					e.Result = CreateHeartbeat(message);
					return;
				}
			}

			defines["Peach.Cwd"] = Environment.CurrentDirectory;
			parserArgs[Peach.Core.Analyzers.PitParser.DEFINED_VALUES] = defines;

			try
			{
				dom = pitParser.asParser(parserArgs, nodeState.PitFilePath);
			}
			catch (Peach.Core.PeachException ex)
			{
				string message = "Exception on Pit parse:\n" + ex.ToString();
				e.Result = CreateHeartbeat(message);
				return;
			}
			#endregion

			#region Create Loggers
			List<Peach.Core.Logger> loggers = new List<Peach.Core.Logger>();

			var jobnodefolder = String.Format(Formats.JobNodeFolder, nodeState.StartPeachRequest.JobID, nodeState.StartPeachRequest.PitFileName, reverseStringFormat(QueueNames.QUEUE_NODE, nodeState.NodeQueue)[0]);
			if (String.IsNullOrEmpty(nodeState.StartPeachRequest.MongoDbConnectionString) == false)
			{
				string nodeName = nodeState.NodeQueue;
				var ret = nodeState.NodeQueue.ReverseFormatString(QueueNames.QUEUE_NODE);
				if (ret.Count == 1)
				{
					nodeName = ret[0];
				}
				Dictionary<string, Peach.Core.Variant> mongoargs = new Dictionary<string, Peach.Core.Variant>();
				mongoargs.Add("MongoDbConnectionString", new Peach.Core.Variant(nodeState.StartPeachRequest.MongoDbConnectionString));
				mongoargs.Add("JobID", new Peach.Core.Variant(nodeState.StartPeachRequest.JobID));
				mongoargs.Add("NodeName", new Peach.Core.Variant(nodeName));
				mongoargs.Add("PitFileName", new Peach.Core.Variant(nodeState.StartPeachRequest.PitFileName));
				mongoargs.Add("Path", new Peach.Core.Variant(jobnodefolder));
				mongoargs.Add("RabbitHost", new Peach.Core.Variant(nodeState.RabbitMq.HostName));
				mongoargs.Add("RabbitPort", new Peach.Core.Variant(nodeState.RabbitMq.Port));
				mongoargs.Add("RabbitUser", new Peach.Core.Variant(nodeState.RabbitMq.UserName));
				mongoargs.Add("RabbitPassword", new Peach.Core.Variant(nodeState.RabbitMq.Password));
				mongoargs.Add("RabbitUseSSL", new Peach.Core.Variant(nodeState.RabbitMq.SSL.ToString(), "system.boolean"));
				try
				{
					loggers.Add(new PeachFarm.Loggers.PeachFarmLogger(mongoargs));
				}
				catch (Exception ex)
				{
					var message = String.Format("Error Starting Peach ({0}):\n{1}", nodeState.StartPeachRequest.JobID, ex.Message);
					e.Result = CreateHeartbeat(message);
					return;
				}
			}

			Dictionary<string, Peach.Core.Variant> metricsargs = new Dictionary<string, Peach.Core.Variant>();
			metricsargs.Add("Path", new Peach.Core.Variant(jobnodefolder));
			metricsargs.Add("JobID", new Peach.Core.Variant(nodeState.StartPeachRequest.JobID));
			metricsargs.Add("RabbitHost", new Peach.Core.Variant(nodeState.RabbitMq.HostName));
			metricsargs.Add("RabbitPort", new Peach.Core.Variant(nodeState.RabbitMq.Port));
			metricsargs.Add("RabbitUser", new Peach.Core.Variant(nodeState.RabbitMq.UserName));
			metricsargs.Add("RabbitPassword", new Peach.Core.Variant(nodeState.RabbitMq.Password));
			metricsargs.Add("RabbitUseSSL", new Peach.Core.Variant(nodeState.RabbitMq.SSL.ToString(), "system.boolean"));
			try
			{
				loggers.Add(new PeachFarm.Loggers.MetricsRabbitLogger(metricsargs));
			}
			catch (Exception ex)
			{
				var message = String.Format("Error Starting Peach ({0}):\n{1}", nodeState.StartPeachRequest.JobID, ex.Message);
				e.Result = CreateHeartbeat(message);
				return;
			}

			foreach (var test in dom.tests.Values)
			{
				test.loggers = loggers;
			}
			#endregion

			#region Configure Run
			Peach.Core.RunConfiguration config = new Peach.Core.RunConfiguration();
			config.pitFile = nodeState.PitFilePath;
			config.debug = 0;
			config.runName = nodeState.StartPeachRequest.TestName;

			if (nodeState.StartPeachRequest.RangeStartSpecified && nodeState.StartPeachRequest.RangeEndSpecified)
			{
				config.range = true;
				config.rangeStart = nodeState.StartPeachRequest.RangeStart;
				config.rangeStop = nodeState.StartPeachRequest.RangeEnd;
			}

			if (nodeState.StartPeachRequest.Seed > 0)
			{
				config.randomSeed = nodeState.StartPeachRequest.Seed;
			}
			#endregion

			#region Run Peach
			try
			{
				peach.startFuzzing(dom, config);
			}
			catch (Peach.Core.PeachException pex)
			{
				string message = String.Format("Peach Exception during job ({0}):\n{1}", nodeState.StartPeachRequest.JobID, pex.ToString());
				e.Result = CreateHeartbeat(message);
			}
			catch (Exception ex)
			{
				string message = String.Format("Unknown Exception from Peach during job {0}:\n{1}", nodeState.StartPeachRequest.JobID, ex.ToString());
				e.Result = CreateHeartbeat(message);
			}
			#endregion
		}

		void peachWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Result != null)
			{
				if (e.Result is Heartbeat)
				{
					SendHeartbeat((Heartbeat)e.Result);
				}
			}
			StartPeachCleanUp();
		}

		private void StartPeachCleanUp()
		{
			Environment.CurrentDirectory = nodeState.RootDirectory;
			if (nodeState.StartPeachRequest != null)
			{
				string temppath = Path.Combine(nodeState.RootDirectory, "jobtmp", nodeState.StartPeachRequest.JobID);
				if (Directory.Exists(temppath))
				{
					try
					{
						Directory.Delete(temppath, true);
					}
					catch { }
				}
			}
			ChangeStatus(Common.Messages.Status.Alive);
			nodeState.Reset();
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
			if (nodeState.StartPeachRequest == null)
			{
				nlog.Info("Test Finished, though I don't know the JobID");
			}
			else
			{
				nlog.Info("Test Finished: " + nodeState.StartPeachRequest.JobID);
			}
		}

		void peach_TestError(Peach.Core.RunContext context, Exception e)
		{
			if (nodeState.StartPeachRequest == null)
			{
				nlog.Error("Test Error: {0}\n{1}", "Job ID unknown", e.Message);
			}
			else
			{
				nlog.Error("Test Error: {0}\n{1}", nodeState.StartPeachRequest.JobID, e.Message);
			}
		}
		#endregion

		private Heartbeat CreateHeartbeat()
		{
			Heartbeat heartbeat = new Heartbeat();
			heartbeat.NodeName = nodeState.NodeName;
			heartbeat.Version = nodeState.Version;
			
			if (nodeState.Status == Common.Messages.Status.Running)
			{
				heartbeat.JobID = nodeState.StartPeachRequest.JobID;
				heartbeat.UserName = nodeState.StartPeachRequest.UserName;
				heartbeat.PitFileName = nodeState.StartPeachRequest.PitFileName;
				heartbeat.Seed = nodeState.Seed;
				heartbeat.Iteration = nodeState.Iteration;
			}
			heartbeat.Tags = nodeState.Tags;
			heartbeat.QueueName = nodeState.NodeQueue;

			// Nodes shouldn't set their own stamp, system time/date could be incorrect.
			// Really it shouldn't be necessary for a node to set its own stamp,
			// the Controller can do that itself
			// heartbeat.Stamp = DateTime.Now;
			
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

		private void StopFuzzer()
		{
			if (nodeState.RunContext != null)
			{
				if (nodeState.RunContext.continueFuzzing)
				{
					nodeState.RunContext.continueFuzzing = false;
				}
			}
			else
			{
				StartPeachCleanUp();
			}
		}

		private List<string> reverseStringFormat(string template, string str)
		{
			string pattern = "^" + Regex.Replace(template, @"\{[0-9]+\}", "(.*?)") + "$";

			Regex r = new Regex(pattern);
			Match m = r.Match(str);

			List<string> ret = new List<string>();

			for (int i = 1; i < m.Groups.Count; i++)
			{
				ret.Add(m.Groups[i].Value);
			}

			return ret;
		}

		public void Dispose()
		{
			this.Close();
		}
	}

	internal class NodeState
	{
		public NodeState(PeachFarm.Node.Configuration.NodeSection config)
		{
			config.Validate();

			Status = Common.Messages.Status.Alive;

			ControllerQueue = String.Format(QueueNames.QUEUE_CONTROLLER, config.Controller.IpAddress);
			
			if ((config.Tags != null) && (config.Tags.Count > 0))
			{
				Tags = config.Tags.ToString();
			}

			if (String.IsNullOrEmpty(config.Node.NameOverride))
			{
				NodeName = RabbitMqHelper.GetLocalIP(config.RabbitMq.HostName).ToString();
			}
			else
			{
				NodeName = config.Node.NameOverride;
			}

			RabbitMq = config.RabbitMq;

			Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

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
				}
			}
		}

		internal string Tags { get; private set; }

		internal RabbitMqElement RabbitMq { get; private set; }

		internal StartPeachRequest StartPeachRequest { get; set; }

		internal string NodeName { get; private set; }

		internal string NodeQueue 
		{
			get { return String.Format(QueueNames.QUEUE_NODE, NodeName); }
		}

		internal string ControllerQueue { get; private set; }

		internal Peach.Core.RunContext RunContext { get; set; }

		internal string RootDirectory { get; set; }

		internal string PitFilePath { get; set; }
		internal string DefinesFilePath { get; set; }

		internal string Version { get; private set; }

		private uint iteration = 0;

		internal uint Iteration
		{
			get
			{
				if ((RunContext != null) && (RunContext.test != null) && (RunContext.test.strategy != null))
				{
					if (RunContext.test.strategy.Iteration > iteration)
					{
						iteration = RunContext.test.strategy.Iteration;
					}
				}
				else
				{
					return 0;
				}
				return iteration; 
			}
		}

		internal uint Seed
		{
			get
			{
				if ((RunContext != null) && (RunContext.config != null))
				{
					return RunContext.config.randomSeed;
				}
				else
				{
					return 0;
				}
			}
		}


		public void Reset()
		{
			RunContext = null;
			PitFilePath = String.Empty;
			DefinesFilePath = String.Empty;
			StartPeachRequest = null;
			iteration = 0;
		}
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
