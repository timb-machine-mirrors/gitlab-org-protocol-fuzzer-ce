﻿using System;
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

		public PeachFarmNode(string nodeName = "")
		{
			#region trap unhandled exceptions and Ctrl-C
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
			#endregion

			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();

			if (assembly == null)
			{
				assembly = System.Reflection.Assembly.GetAssembly(typeof(PeachFarmNode));
			}

			Environment.CurrentDirectory = Path.GetDirectoryName(assembly.Location);

			#region node state
			nodeState = new NodeState((Configuration.NodeSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.node"));
			if (String.IsNullOrEmpty(nodeName) == false)
				nodeState.NodeQueueName = String.Format(QueueNames.QUEUE_NODE, nodeName);

			#endregion

			
			rabbit = new RabbitMqHelper(nodeState.RabbitMq.HostName, nodeState.RabbitMq.Port, nodeState.RabbitMq.UserName, nodeState.RabbitMq.Password, nodeState.RabbitMq.SSL);
			rabbit.MessageReceived += new EventHandler<RabbitMqHelper.MessageReceivedEventArgs>(rabbit_MessageReceived);
			rabbit.StartListener(nodeState.NodeQueueName);

			heartbeat = new System.Timers.Timer(10000);
			heartbeat.Elapsed += (o, e) => { SendHeartbeat(); };
			heartbeat.Start();

			nlog.Info("Peach Farm Node connected.\nController: {0}\nRabbitMQ: {1}\nDirectory: {2}", nodeState.ControllerQueueName, nodeState.RabbitMq.HostName, Environment.CurrentDirectory);
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
			get { return nodeState.ControllerQueueName; }
		}

		public bool IsListening
		{
			get { return rabbit.IsListening; }
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
				SendHeartbeat(CreateHeartbeat(String.Format("Node {0} is already running Job {1}", nodeState.IPAddress, nodeState.StartPeachRequest.JobID)));
				return;
			}

			nodeState.StartPeachRequest = request;

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
					StopPeach(message);
					StartPeachCleanUp(jobid);
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
				StopPeach(message);
				StartPeachCleanUp(jobid);
				return;
			}

			List<Peach.Core.Logger> loggers = new List<Peach.Core.Logger>();


			if (String.IsNullOrEmpty(nodeState.StartPeachRequest.MongoDbConnectionString) == false)
			{
				string nodeName = nodeState.NodeQueueName;
				var ret = nodeState.NodeQueueName.ReverseFormatString(QueueNames.QUEUE_NODE);
				if (ret.Count == 1)
				{
					nodeName = ret[0];
				}
				Dictionary<string, Peach.Core.Variant> mongoargs = new Dictionary<string, Peach.Core.Variant>();
				mongoargs.Add("MongoDbConnectionString", new Peach.Core.Variant(nodeState.StartPeachRequest.MongoDbConnectionString));
				mongoargs.Add("JobID", new Peach.Core.Variant(nodeState.StartPeachRequest.JobID));
				mongoargs.Add("NodeName", new Peach.Core.Variant(nodeName));
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
				string message = "PeachException:\n" + pex.ToString();
				SendHeartbeat(CreateHeartbeat(message));
			}
			catch (Exception ex)
			{
				string message = String.Format("Unknown Exception from Peach:\n{0}", ex.ToString());
				SendHeartbeat(CreateHeartbeat(message));
			}
			finally
			{
				StartPeachCleanUp(jobid);
			}
		}

		private void StartPeachCleanUp(string jobid)
		{
			Environment.CurrentDirectory = nodeState.RootDirectory;
			string temppath = Path.Combine(nodeState.RootDirectory, "jobtmp", jobid);
			if (Directory.Exists(temppath))
			{
				try
				{
					Directory.Delete(temppath, true);
				}
				catch { }
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
			nlog.Info("Test Finished: " + nodeState.StartPeachRequest.JobID);
		}

		void peach_TestError(Peach.Core.RunContext context, Exception e)
		{
			nlog.Error("Test Error: {0}\n{1}", nodeState.StartPeachRequest.JobID, e.Message);
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

					if(nodeState.RunContext.test != null)
						if(nodeState.RunContext.test.strategy != null)
							heartbeat.Iteration = nodeState.RunContext.test.strategy.Iteration;
				}
			}
			heartbeat.Tags = nodeState.Tags;
			heartbeat.QueueName = nodeState.NodeQueueName;
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
			}
		}

	}

	internal class NodeState
	{
		public NodeState(PeachFarm.Node.Configuration.NodeSection config)
		{
			Status = Common.Messages.Status.Alive;

			ControllerQueueName = String.Format(QueueNames.QUEUE_CONTROLLER, config.Controller.IpAddress);
			
			if ((config.Tags != null) && (config.Tags.Count > 0))
			{
				Tags = config.Tags.ToString();
			}

			#region get ip address, used for client name
			IPAddress[] ipaddresses = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
			IPAddress = (from i in ipaddresses where i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork select i).First().ToString();
			#endregion

			NodeQueueName = String.Format(QueueNames.QUEUE_NODE, IPAddress);

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
				}
			}
		}

		internal string Tags { get; private set; }

		internal RabbitMqElement RabbitMq { get; private set; }

		internal StartPeachRequest StartPeachRequest { get; set; }

		internal string IPAddress { get; private set; }

		internal string NodeQueueName { get; set; }
		internal string ControllerQueueName { get; private set; }

		internal Peach.Core.RunContext RunContext { get; set; }

		internal string RootDirectory { get; set; }

		internal string PitFilePath { get; set; }
		internal string DefinesFilePath { get; set; }

		public void Reset()
		{
			RunContext = null;
			PitFilePath = String.Empty;
			DefinesFilePath = String.Empty;
			StartPeachRequest = null;
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
