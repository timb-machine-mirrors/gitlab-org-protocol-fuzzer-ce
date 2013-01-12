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

    private NodeState clientState;

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
      clientState = new NodeState(controllerIPAddress, String.Format(QueueNames.QUEUE_CONTROLLER, controllerIPAddress));
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
        StopPeach();
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
        Heartbeat error = CreateHeartbeat();
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
              Heartbeat heartbeat = CreateHeartbeat(String.Format("{0}\n\n{1}", result.Body, ex.Message));
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
          StopPeach();
          break;
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

      if (modelSend.IsOpen == false)
        modelSend = connection.CreateModel();

      PublishToServer(heartbeat.Serialize(), "Heartbeat");
    }

    #endregion

    #region Receives

    private void StopPeach()
    {
      if (clientState.Status == Status.Running)
      {
        if (clientState.RunContext.continueFuzzing)
        {
          clientState.RunContext.continueFuzzing = false;
        }
        clientState.RunContext = null;


        clientState.Status = Status.Alive;
        RaiseStatusChanged();

        SendHeartbeat(null);
      }
    }

    private void StopPeach(string errorMessage)
    {
      if(String.IsNullOrEmpty(errorMessage))
      {
        StopPeach();
        return;
      }

      if (clientState.Status == Status.Running)
      {
        if (clientState.RunContext.continueFuzzing)
        {
          clientState.RunContext.continueFuzzing = false;
        }
        clientState.RunContext = null;


        SendHeartbeat(CreateHeartbeat(errorMessage));

        clientState.Status = Status.Error;
        RaiseStatusChanged();
      }
    }

    private void StartPeach(StartPeachRequest startPeachRequest)
    {
      if (clientState.Status == Status.Running)
        return;

      string pit = startPeachRequest.Pit;
      if (pit.StartsWith("<!"))
      {
        pit = pit.Substring("<![CDATA[".Length);
        pit = pit.Substring(0, pit.Length - 2);
      }

      clientState.PitFilePath = Path.GetTempFileName();
      using (FileStream stream = new FileStream(clientState.PitFilePath, FileMode.Create))
      {
        StreamWriter writer = new StreamWriter(stream);
        writer.Write(pit);
        writer.Flush();
        writer.Close();
        stream.Close();
      }

      clientState.MongoDbConnectionString = startPeachRequest.MongoDbConnectionString;
      clientState.SqlConnectionString = startPeachRequest.SqlConnectionString;
      clientState.JobID = startPeachRequest.JobID;



      peach = new Peach.Core.Engine(null);
      peach.TestStarting += new Peach.Core.Engine.TestStartingEventHandler(peach_TestStarting);
      peach.TestError += new Peach.Core.Engine.TestErrorEventHandler(peach_TestError);
      peach.TestFinished += new Peach.Core.Engine.TestFinishedEventHandler(peach_TestFinished);

      BackgroundWorker peachWorker = new BackgroundWorker();
      peachWorker.DoWork += new DoWorkEventHandler(peachWorker_DoWork);
      peachWorker.RunWorkerAsync();
    }

    void peach_TestStarting(Peach.Core.RunContext context)
    {
      clientState.RunContext = context;
    }

    void peachWorker_DoWork(object sender, DoWorkEventArgs e)
    {
      Peach.Core.Analyzers.PitParser pitParser = new Peach.Core.Analyzers.PitParser();

     
      Peach.Core.Dom.Dom dom = pitParser.asParser(null, clientState.PitFilePath);

      List<Peach.Core.Logger> loggers = new List<Peach.Core.Logger>();
      if(String.IsNullOrEmpty(clientState.SqlConnectionString) == false)
      {
        Dictionary<string, Peach.Core.Variant> sqlargs = new Dictionary<string, Peach.Core.Variant>();
        sqlargs.Add("SqlConnectionString", new Peach.Core.Variant(clientState.SqlConnectionString));
        sqlargs.Add("JobID", new Peach.Core.Variant(clientState.JobID.ToString()));

        loggers.Add(new Loggers.SqlLogger(sqlargs));
      }

      if(String.IsNullOrEmpty(clientState.MongoDbConnectionString) == false)
      {
        Dictionary<string, Peach.Core.Variant> mongoargs = new Dictionary<string, Peach.Core.Variant>();
        mongoargs.Add("MongoDbConnectionString", new Peach.Core.Variant(clientState.MongoDbConnectionString));
        mongoargs.Add("JobID", new Peach.Core.Variant(clientState.JobID.ToString()));

        loggers.Add(new Loggers.MongoLogger(mongoargs));
      }


      foreach (var test in dom.tests.Values)
      {
        //TODO
        test.loggers = loggers;
      }

      Peach.Core.RunConfiguration config = new Peach.Core.RunConfiguration();
      //config.pitFile = clientState.PitFilePath;
      config.debug = false;
      //config.runName = clientState.JobID.ToString();

      clientState.Status = Common.Messages.Status.Running;
      RaiseStatusChanged();

      peach.startFuzzing(dom, config);
    }

    void peach_TestFinished(Peach.Core.RunContext context)
    {
      StopPeach();
    }

    void peach_TestError(Peach.Core.RunContext context, Exception e)
    {
      StopPeach(e.Message);
    }

    #endregion

    private Heartbeat CreateHeartbeat()
    {
      Heartbeat heartbeat = new Heartbeat();
      heartbeat.ComputerName = clientState.IPAddress;
      if (clientState.Status == Common.Messages.Status.Running)
      {
        heartbeat.JobID = clientState.JobID;
      }
      heartbeat.QueueName = clientQueueName;
      heartbeat.Stamp = DateTime.Now;
      heartbeat.Status = clientState.Status;
      return heartbeat;
    }

    private Heartbeat CreateHeartbeat(string errorMessage)
    {
      Heartbeat heartbeat = CreateHeartbeat();
      heartbeat.Status = Common.Messages.Status.Error;
      heartbeat.ErrorMessage = errorMessage;
      return heartbeat;
    }
  }

  internal class NodeState
  {
    public NodeState(string rabbitHostName, string serverQueueName)
    {
      Status = Common.Messages.Status.Alive;

      RabbitHostName = rabbitHostName;
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
              SqlConnectionString = String.Empty;
              PitFilePath = String.Empty;
              JobID = Guid.Empty;
              RunContext = null;
              break;
          }
        }
      }
    }

    internal string IPAddress { get; private set; }
    internal string ServerQueueName { get; private set; }
    internal string RabbitHostName { get; private set; }
    
    internal string MongoDbConnectionString { get; set; }
    internal string SqlConnectionString { get; set; }
    internal string PitFilePath { get; set; }
    internal Peach.Core.RunContext RunContext { get; set; }
    internal Guid JobID { get; set; }
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

