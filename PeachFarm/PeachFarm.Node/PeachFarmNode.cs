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

    private NodeState clientState = new NodeState();

    private Peach.Core.Engine peach;

    private Configuration.NodeSection config;

    public PeachFarmNode(string controllerIPAddress = "")
    {
      config = (Configuration.NodeSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.node");

      if (string.IsNullOrEmpty(controllerIPAddress))
      {
        controllerIPAddress = config.Controller.IpAddress;
      }

      #region trap unhandled exceptions and Ctrl-C
      AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
      AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
      #endregion
      
      #region command line arguments
      clientState.RabbitHostName = controllerIPAddress;
      clientState.ServerQueueName = String.Format(QueueNames.QUEUE_CONTROLLER, controllerIPAddress);
      #endregion

      #region get ip address, used for client name
      IPAddress[] ipaddresses = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
      clientState.IPAddress = (from i in ipaddresses where i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork select i).First().ToString();
      #endregion
    }

    public void StartNode()
    {

      #region set up RabbitMQ connection and start listening for messages
      try
      {
        OpenConnection(clientState.RabbitHostName);
      }
      catch
      {
        throw new ApplicationException(String.Format("Could not open connection to RabbitMQ server at {0}, exiting now.", clientState.RabbitHostName));
      }
      RegisterQueues();
      StartListener();

      #endregion

      #region start heartbeat
      heartbeat = new Timer(new TimerCallback(SendHeartbeat), null, TimeSpan.FromMilliseconds(0), TimeSpan.FromSeconds(10));
      #endregion

    }

    public void StopNode()
    {
      #region stop heartbeat
      heartbeat.Dispose();
      #endregion

      #region stop the currently running peach job
      if (clientState.Status == Status.Running)
      {
        StopPeach(new StopPeachRequest());
      }
      #endregion

      #region send a stopping message to controller
      clientState.Status = Status.Stopping;
      RaiseStatusChanged();

      SendHeartbeat(null);
      #endregion

      #region deregister from RabbitMQ
      DeregisterQueues();
      StopListeners();
      CloseConnection();
      #endregion
    }

    public Status Status
    {
      get { return clientState.Status; }
    }

    public string ServerQueue
    {
      get { return clientState.ServerQueueName; }
    }

    public Configuration.NodeSection Configuration
    {
      get { return config; }
    }

    public event EventHandler<StatusChangedEventArgs> StatusChanged;

    private void RaiseStatusChanged()
    {
      if (StatusChanged != null)
      {
        StatusChanged(this, new StatusChangedEventArgs(this.clientState.Status));
      }
    }
    
    #region Termination handlers

    private void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
      if ((clientState.Status != Status.Stopping) && (connection != null) && (connection.IsOpen))
      {
        clientState.Status = Status.Stopping;
        RaiseStatusChanged();

        SendHeartbeat(null);
        DeregisterQueues();
        StopListeners();
        CloseConnection();
      }
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      if ((clientState.Status != Status.Stopping) && (connection != null) && (connection.IsOpen))
      { 
        Heartbeat error = new Heartbeat();
        //error.CommandLine = clientState.CommandLine;
        error.PitFilePath = clientState.PitFilePath;
        error.ComputerName = clientState.IPAddress;
        error.Stamp = DateTime.Now;
        error.QueueName = clientQueueName;
        error.Status = Status.Error;
        error.ErrorMessage = "Unknown error";

        if (e.ExceptionObject is Exception)
        {
          error.ErrorMessage = ((Exception)e.ExceptionObject).Message;
        }
        SendHeartbeat(error);

        if (e.IsTerminating)
        {
          clientState.Status = Status.Stopping;
          RaiseStatusChanged();

          SendHeartbeat(null);
          DeregisterQueues();
          StopListeners();
          CloseConnection();
        }
      }
    }
    #endregion

    #region Log file functions - Peach 2
    /*
    private void ProcessLogFiles(object state)
    {
      string[] runfolders = Directory.GetDirectories(clientState.LogPath);
      for (int r = 0; r < runfolders.Length; r++)
      {
        string runfoldername = runfolders[r].Split(Path.DirectorySeparatorChar).Last();

        Run run = clientState.Runs.Find(runfoldername);

        if (run == null)
        {
          if (clientState.Runs == null)
          {
            clientState.Runs = new List<Run>();
          }

          run = new Run() { ComputerName = clientState.IPAddress };
          //run.CommandLine = clientState.CommandLine;
          run.PitFilePath = clientState.PitFilePath;
          run.RunName = runfoldername;
          run.ComputerName = clientState.IPAddress;
          clientState.Runs.Add(run);
        }

        string[] filenames = Directory.GetFiles(runfolders[r], "*.txt", SearchOption.AllDirectories);
        for (int f = 0; f < filenames.Length; f++)
        {
          string fileName = Path.Combine(run.ComputerName, filenames[f].Substring(clientState.LogPath.Length + 1));
          LogFile file = run.LogFiles.Find(fileName);
          if (file == null)
          {
            file = new LogFile();
            file.FileName = fileName;
            file.LocalFileName = filenames[f];
            run.LogFiles.Add(file);
          }

          //try
          //{
          //  using (StreamReader reader = new StreamReader(filenames[f]))
          //  {
          //    file.Data = encoding.GetBytes(reader.ReadToEnd());
          //  }
          //  run.LogFiles.Add(file);
          //}
          //catch
          //{
          //  // if the file can't be opened, do nothing and move on to the next
          //}
        }
      }

      if (clientState.Runs.Count > 0)
      {
        //StopPeachResponse response = new StopPeachResponse();
        //response.Runs = runs;
        //PublishToServer(response.Serialize(), "ProcessRuns");

        clientState.Runs = clientState.Runs.DatabaseInsert(clientState.MongoDbConnectionString);
      }

    }

    private static void DeleteSubFolders(string path)
    {
      string[] folders = Directory.GetDirectories(path);
      for (int f = 0; f < folders.Length; f++)
      try
      {
        Directory.Delete(folders[f], true);
      }
      catch (Exception)
      {
        throw new ApplicationException(String.Format("Could not delete folders {0}", folders[f]));
      }
    }
    //*/
    #endregion

    #region MQ functions

    private static string clientQueueName;

    private static IConnection connection;
    private static IModel modelSend;
    private static IModel modelReceive;

    private static BackgroundWorker listener = new BackgroundWorker();

    private static void OpenConnection(string hostName)
    {
      ConnectionFactory factory = new ConnectionFactory();
      factory.HostName = hostName;
      connection = factory.CreateConnection();
      modelSend = connection.CreateModel();
      modelReceive = connection.CreateModel();

    }

    private void RegisterQueues()
    {

      //client queue
      clientQueueName = String.Format(QueueNames.QUEUE_NODE, clientState.IPAddress);
      modelSend.ExchangeDeclare(QueueNames.EXCHANGE_NODE, QueueNames.EXCHANGETYPE_NODE);
      modelSend.QueueDeclare(clientQueueName, false, false, false, null);
      modelSend.QueueBind(clientQueueName, QueueNames.EXCHANGE_NODE, "");
      modelSend.QueuePurge(clientQueueName);
    }

    private void DeregisterQueues()
    {
      modelSend.QueueUnbind(clientQueueName, QueueNames.EXCHANGE_NODE, "", null);
      modelSend.QueueDelete(clientQueueName);
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
        if ((clientState.Status == Status.Alive) || (clientState.Status == Status.Running))
        {
          BasicGetResult result = null;
          try
          {
            result = modelReceive.BasicGet(clientQueueName, false);
          }
          catch
          {
            throw new ApplicationException("Can't communicate with RabbitMQ server, press Enter to quit.");
          }
          if (result != null)
          {
            string body = encoding.GetString(result.Body);
            string action = encoding.GetString((byte[])result.BasicProperties.Headers["Action"]);
            Debug.WriteLine(String.Format("Action: {0}", action));
            //Debug.WriteLine(String.Format("Body:\n'{0}'", body));
            try
            {
              ProcessAction(action, body);
              modelSend.BasicAck(result.DeliveryTag, false);
            }
            catch(Exception ex)
            {
              Heartbeat heartbeat = new Heartbeat();
              //heartbeat.CommandLine = clientState.CommandLine;
              heartbeat.PitFilePath = clientState.PitFilePath;
              heartbeat.ComputerName = clientState.IPAddress;
              heartbeat.ErrorMessage = String.Format("{0}\n\n{1}", result.Body, ex.Message);
              heartbeat.QueueName = clientQueueName;
              heartbeat.Status = Status.Error;
              SendHeartbeat(heartbeat);
            }
            finally
            {
            }
            Debug.WriteLine(String.Format("Done '{0}'", action));
          }
        }

        Thread.Sleep(1000);
      }
    }

    private void StopListeners()
    {
      listener.CancelAsync();
    }

    private void CloseConnection()
    {
      if(modelSend.IsOpen)
        modelSend.Close();

      if (modelReceive.IsOpen)
        modelReceive.Close();

      if(connection.IsOpen)
        connection.Close();
    }

    private void PublishToServer(string message, string action)
    {
      Debug.WriteLine(message);
      Debug.WriteLine("----------------------------------");
      modelSend.PublishToQueue(clientState.ServerQueueName, message, action);
    }

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
      }
    }
    #endregion

    #region Sends

    private void SendErrorHeartbeat(string message)
    {
      Heartbeat heartbeat = new Heartbeat();
      heartbeat.ComputerName = clientState.IPAddress;
      heartbeat.Stamp = DateTime.Now;
      heartbeat.Status = Status.Error;
      heartbeat.QueueName = clientQueueName;
      heartbeat.ErrorMessage = message;
      //heartbeat.CommandLine = clientState.CommandLine;
      heartbeat.PitFilePath = clientState.PitFilePath;
      SendHeartbeat(heartbeat);
    }

    private void SendHeartbeat(object state)
    {
      Heartbeat heartbeat = null;
      if ((state != null) && (state is Heartbeat))
      {
        heartbeat = (Heartbeat)state;
      }
      else
      {
        heartbeat = new Heartbeat();
        heartbeat.ComputerName = clientState.IPAddress;
        heartbeat.Stamp = DateTime.Now;
        heartbeat.Status = clientState.Status;
        heartbeat.QueueName = clientQueueName;

        if (heartbeat.Status == Status.Running)
        {
          //heartbeat.CommandLine = clientState.CommandLine;
          heartbeat.PitFilePath = clientState.PitFilePath;
        }
      }

      if (modelSend.IsOpen == false)
        modelSend = connection.CreateModel();

      PublishToServer(heartbeat.Serialize(), "Heartbeat");
    }

    #endregion

    #region Receives

    private void StopPeach(StopPeachRequest stopPeachRequest)
    {
      if (clientState.Status == Status.Running)
      {
        #region Peach 2
        /*
        if (clientState.PeachProcess != null)
        {
          clientState.PeachProcess.Exited -= peachProcess_Exited;

          try
          {
            clientState.PeachProcess.KillPlusChildren();
          }
          catch (Exception ex)
          {
            SendErrorHeartbeat(String.Format("Exception while trying to kill Peach:\n{0}", ex.Message));
          }
        }
        
        fileSyncTimer.Dispose();
        fileSyncTimer = null;

        try
        {
          ProcessLogFiles(clientState);
        }
        catch (Exception ex)
        {
          SendErrorHeartbeat(String.Format("Exception while processing log files:\n{0}", ex.Message));
        }

        clientState.Runs.Clear();
        DeleteSubFolders(clientState.LogPath);


        //*/
        #endregion

        #region Peach 3
        if (clientState.RunContext != null)
        {
          clientState.RunContext.continueFuzzing = false;
        }
        #endregion

        clientState.Status = Status.Alive;
        RaiseStatusChanged();

        SendHeartbeat(null);
      }

    }

    private void StartPeach(StartPeachRequest startPeachRequest)
    {
      if (clientState.Status == Status.Running)
        return;

      #region Mongo
      /*
      if (DatabaseHelper.TestConnection(startPeachRequest.MongoDbConnectionString) == false)
      {
        string message = String.Format("MongoDB connection test failed: {0}", startPeachRequest.MongoDbConnectionString);
        SendErrorHeartbeat(message);
        throw new ApplicationException(message);
      }
      //*/
      #endregion

      if (Directory.Exists(startPeachRequest.LogPath) == false)
      {
        string message = String.Format("Log Path directory does not exist:\n{0}", startPeachRequest.LogPath);
        SendErrorHeartbeat(message);
        throw new ApplicationException(message);
      }

      #region pit file stuff
      //string pitfilename = String.Empty;

      //if (Path.IsPathRooted(args[1]))
      //{
      //  pitfilename = args[1];
      //}
      //else
      //{
      //  pitfilename = Path.Combine(Path.GetDirectoryName(args[0]), args[1]);
      //}

      //string pitxml = String.Empty;
      //using (StreamReader reader = new StreamReader(pitfilename))
      //{
      //  pitxml = reader.ReadToEnd();
      //}
      //pitStuff = new PitStuff(pitxml);

      //pitStuff = new PitStuff(startPeachRequest.PitXml);

      //string tempFileName = Path.Combine(Path.GetTempPath(), String.Format("{0}_{1}.xml", pitStuff.RunName, DateTime.Now.ToString("yyyyMMdd_hhmm")));
      //StreamWriter writer = new StreamWriter(tempFileName);
      //writer.Write(startPeachRequest.PitXml);
      //writer.Close();
      //Debug.WriteLine(String.Format("TempFileName: {0}", tempFileName));
      #endregion

      //TODO comment out log file collection, Peach 2 only


      clientState.LogPath = startPeachRequest.LogPath;
      clientState.MongoDbConnectionString = startPeachRequest.MongoDbConnectionString;

      #region Peach 2
      /*
      DeleteSubFolders(clientState.LogPath);

      string filename = String.Empty;
      string arguments = String.Empty;
      int endindex = -1;
      if (startPeachRequest.CommandLine[0].Equals('\''))
      {
        endindex = startPeachRequest.CommandLine.IndexOf('\'', 1);
      }
      else
      {
        endindex = startPeachRequest.CommandLine.IndexOf(' ', 1);
      }
      
      filename = startPeachRequest.CommandLine.Substring(1, endindex - 1);
      if (File.Exists(filename) == false)
      {
        string message = String.Format("Command line executable does not exist:\n{0}", filename);
        SendErrorHeartbeat(message);
        throw new ApplicationException(message);
      }


      arguments = startPeachRequest.CommandLine.Substring(filename.Length + 2);

      fileSyncTimer = new Timer(ProcessLogFiles, clientState, TimeSpan.FromMilliseconds(0), TimeSpan.FromSeconds(60));

      ProcessStartInfo startInfo = new ProcessStartInfo(filename, arguments);
      startInfo.CreateNoWindow = true;
      try
      {
        clientState.PeachProcess = new Process();
        clientState.PeachProcess.EnableRaisingEvents = true;
        clientState.PeachProcess.StartInfo = startInfo;
        clientState.PeachProcess.Exited += new EventHandler(peachProcess_Exited);
        clientState.PeachProcess.Start();
        clientState.Status = Status.Running;
        RaiseStatusChanged();

        clientState.CommandLine = startPeachRequest.CommandLine;
      }
      catch (Exception ex)
      {
        throw ex;
      }
      finally
      {
        //File.Delete(tempFileName);
      }
      //*/
      #endregion

      #region Peach 3
      Dictionary<string, Peach.Core.Variant> args = new Dictionary<string,Peach.Core.Variant>();
      args.Add("SqlConnectionString", new Peach.Core.Variant("Provider=SQLNCLI10.1;Integrated Security=SSPI;Persist Security Info=False;User ID=\"\";Initial Catalog=PeachFarmReporting;Data Source=dejaapps;Initial File Name=\"\";Server SPN=\"\""));
      PeachFarmReportingLogger logger = new PeachFarmReportingLogger(args);

      peach = new Peach.Core.Engine(logger);
      peach.TestStarting += new Peach.Core.Engine.TestStartingEventHandler(peach_TestStarting);
      peach.TestError += new Peach.Core.Engine.TestErrorEventHandler(peach_TestError);
      peach.TestFinished += new Peach.Core.Engine.TestFinishedEventHandler(peach_TestFinished);

      BackgroundWorker peachWorker = new BackgroundWorker();
      peachWorker.DoWork += new DoWorkEventHandler(peachWorker_DoWork);
      peachWorker.RunWorkerAsync(startPeachRequest.PitFilePath);
      #endregion
    }

    void peach_TestStarting(Peach.Core.RunContext context)
    {
      clientState.RunContext = context;
    }

    void peachWorker_DoWork(object sender, DoWorkEventArgs e)
    {
      string filename = (string)e.Argument;

      Peach.Core.Analyzers.PitParser pitParser = new Peach.Core.Analyzers.PitParser();
      Peach.Core.Dom.Dom dom = pitParser.asParser(null, filename);

      Peach.Core.RunConfiguration config = new Peach.Core.RunConfiguration();
      config.pitFile = filename;
      config.debug = false;

      peach.startFuzzing(dom, config);
    }

    void peach_TestFinished(Peach.Core.RunContext context)
    {
      StopPeach(null);
    }

    void peach_TestError(Peach.Core.RunContext context, Exception e)
    {
      StopPeach(null);
    }

    #endregion

    #region Peach 2 process handlers
    /*
    private void peachProcess_Exited(object sender, EventArgs e)
    {
      StopPeach(null);
    }
    //*/
    #endregion
  }

  internal class NodeState
  {
    public NodeState()
    {
      Runs = new List<Run>();
      Status = Common.Messages.Status.Alive;
    }

    internal Status Status { get;  set; }
    internal string LogPath { get;  set; }
    internal string MongoDbConnectionString { get;  set; }
    //internal string CommandLine { get;  set; }
    internal string PitFilePath { get; set; }
    internal string IPAddress { get;  set; }
    //internal Process PeachProcess { get;  set; }
    internal Peach.Core.RunContext RunContext { get; set; }
    internal string RabbitHostName { get;  set; }
    internal string ServerQueueName { get;  set; }
    internal List<Run> Runs { get; set; }
  }

  internal class PitStuff
  {

    internal PitStuff(string xml)
    {

      StringReader reader = new StringReader(xml);
      XPathNavigator nav = new XPathDocument(reader).CreateNavigator();

      XmlNamespaceManager ns = new XmlNamespaceManager(nav.NameTable);
      ns.AddNamespace("p", "http://phed.org/2008/Peach");
      try
      {
        RunName = nav.SelectSingleNode("/p:Peach/p:Run/@name", ns).Value;
        LogPath = nav.SelectSingleNode("/p:Peach/p:Run/p:Logger[@class='logger.Filesystem']/p:Param[@name='path']/@value", ns).Value;
      }
      catch(Exception ex)
      {
        throw ex;
      }

    }

    internal string RunName { get; set; }
    internal string LogPath { get; set; }
  }

  public class StatusChangedEventArgs : EventArgs
  {
    public StatusChangedEventArgs(Status status)
      :base()
    {
      this.Status = status;
    }

    public Status Status { get; private set; }
  }
}
