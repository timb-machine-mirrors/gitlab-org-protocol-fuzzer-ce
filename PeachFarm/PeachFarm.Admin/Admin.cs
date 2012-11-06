using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Net;

using RabbitMQ.Client;

using PeachFarm.Common;
using PeachFarm.Common.Messages;
using System.Threading;

namespace PeachFarm.Admin
{
  public class Admin
  {
    private UTF8Encoding encoding = new UTF8Encoding();


    public Admin(string serverHostName)
    {
      ServerHostName = serverHostName;
    }

    public string ServerHostName { get; private set; }

    public void StartAdmin()
    {
      try
      {
        OpenConnection(ServerHostName);
      }
      catch
      {
        throw new ApplicationException(String.Format("Could not open connection to RabbitMQ server at {0}, exiting now.", ServerHostName));
      }

      InitializeQueues();
      StartListeners();
    }

    public void StopAdmin()
    {
      StopListeners();
      CloseConnection();
    }

    #region AsyncCompletes

    #region StartPeachCompleted
    public class StartPeachCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
      public StartPeachCompletedEventArgs(StartPeachResponse result)
        :base(null, false, null)
      {
        Result = result;
      }

      public StartPeachResponse Result { get; private set; }
    }

    public delegate void StartPeachCompletedEventHandler(object sender, StartPeachCompletedEventArgs e);

    public event StartPeachCompletedEventHandler StartPeachCompleted;

    private void RaiseStartPeachCompleted(StartPeachResponse result)
    {
      if(StartPeachCompleted != null)
        StartPeachCompleted(this, new StartPeachCompletedEventArgs(result));
    }
    #endregion

    #region StopPeachCompleted
    public class StopPeachCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
      public StopPeachCompletedEventArgs(StopPeachResponse result)
        :base(null, false, null)
      {
        Result = result;
      }

      public StopPeachResponse Result { get; private set; }
    }

    public delegate void StopPeachCompletedEventHandler(object sender, StopPeachCompletedEventArgs e);

    public event StopPeachCompletedEventHandler StopPeachCompleted;

    private void RaiseStopPeachCompleted(StopPeachResponse result)
    {
      if(StopPeachCompleted != null)
        StopPeachCompleted(this, new StopPeachCompletedEventArgs(result));
    }
    #endregion

    #region ListComputersCompleted
    public class ListComputersCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
      public ListComputersCompletedEventArgs(ListComputersResponse result)
        : base(null, false, null)
      {
        Result = result;
      }

      public ListComputersResponse Result { get; private set; }
    }

    public delegate void ListComputersCompletedEventHandler(object sender, ListComputersCompletedEventArgs e);

    public event ListComputersCompletedEventHandler ListComputersCompleted;

    private void RaiseListComputersCompleted(ListComputersResponse result)
    {
      if (ListComputersCompleted != null)
        ListComputersCompleted(this, new ListComputersCompletedEventArgs(result));
    }
    #endregion

    #region ListErrorsCompleted
    public class ListErrorsCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
      public ListErrorsCompletedEventArgs(ListErrorsResponse result)
        : base(null, false, null)
      {
        Result = result;
      }

      public ListErrorsResponse Result { get; private set; }
    }

    public delegate void ListErrorsCompletedEventHandler(object sender, ListErrorsCompletedEventArgs e);

    public event ListErrorsCompletedEventHandler ListErrorsCompleted;

    private void RaiseListErrorsCompleted(ListErrorsResponse result)
    {
      if (ListErrorsCompleted != null)
        ListErrorsCompleted(this, new ListErrorsCompletedEventArgs(result));
    }
    #endregion

    #endregion

    #region Sends
    //private void StartPeach(string commandLine, int clientCount, string logPath)
    public void StartPeachAsync(string pitFilePath, int clientCount, string logPath)
    {
      StartPeachRequest request = new StartPeachRequest();
      request.ClientCount = clientCount;
      //request.CommandLine = commandLine;
      request.PitFilePath = pitFilePath;
      request.LogPath = logPath;
      request.IPAddress = String.Empty;
      PublishToServer(request.Serialize(), "StartPeach");

      
    }

    //private void StartPeach(string commandLine, string ipAddress, string logPath)
    public void StartPeachAsync(string pitFilePath, string ipAddress, string logPath)
    {
      StartPeachRequest request = new StartPeachRequest();
      //request.CommandLine = commandLine;
      request.PitFilePath = pitFilePath;
      request.ClientCount = 1;
      request.IPAddress = ipAddress;
      request.LogPath = logPath;
      PublishToServer(request.Serialize(), "StartPeach");
      Console.WriteLine("waiting for result...");
      Console.ReadLine();
    }

    public void StopPeachAsync(string commandLine = "")
    {
      StopPeachRequest request = new StopPeachRequest();
      request.CommandLineSearch = commandLine;
      PublishToServer(request.Serialize(), "StopPeach");
    }

    public void ListComputersAsync()
    {
      ListComputersRequest request = new ListComputersRequest();
      PublishToServer(request.Serialize(), "ListComputers");
    }

    public void ListErrorsAsync()
    {
      ListErrorsRequest request = new ListErrorsRequest();
      PublishToServer(request.Serialize(), "ListErrors");
    }
    #endregion

    #region Receives
    private void ListComputersReply(ListComputersResponse response)
    {
      RaiseListComputersCompleted(response);
    }

    private void ListErrorsReply(ListErrorsResponse response)
    {
      RaiseListErrorsCompleted(response);
    }

    private void StartPeachReply(StartPeachResponse response)
    {
      RaiseStartPeachCompleted(response);
    }

    private void StopPeachReply(StopPeachResponse response)
    {
      RaiseStopPeachCompleted(response);
    }
    #endregion

    #region MQ functions

    private string serverQueueName;
    private string adminQueueName;

    private IConnection connection;
    private IModel modelSend;
    private IModel modelReceive;

    private BackgroundWorker adminListener = new BackgroundWorker();

    private void OpenConnection(string hostName)
    {
      ConnectionFactory factory = new ConnectionFactory();
      factory.HostName = hostName;
      connection = factory.CreateConnection();
      modelSend = connection.CreateModel();
      modelReceive = connection.CreateModel();

    }

    private void InitializeQueues()
    {
      IPAddress[] ipaddresses = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
      string ipAddress = (from i in ipaddresses where i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork select i).First().ToString();

      serverQueueName = String.Format(QueueNames.QUEUE_CONTROLLER, ServerHostName);
      adminQueueName = String.Format(QueueNames.QUEUE_ADMIN, ipAddress);

      //server queue
      modelSend.QueueDeclare(serverQueueName, true, false, false, null);

      //admin queue
      modelReceive.QueueDeclare(adminQueueName, true, false, false, null);

      if (modelReceive.IsOpen == false)
        modelReceive = connection.CreateModel();

      // flush admin queue
      BasicGetResult result = modelReceive.BasicGet(adminQueueName, false);
      while (result != null)
      {
        result = modelReceive.BasicGet(adminQueueName, false);
      }
    }

    private void StartListeners()
    {

      adminListener = new BackgroundWorker();
      adminListener.WorkerSupportsCancellation = true;
      adminListener.DoWork += Listen;
      adminListener.RunWorkerAsync();
    }

    private void Listen(object sender, DoWorkEventArgs e)
    {
      BasicGetResult result = null;

      while (!e.Cancel)
      {
        try
        {
          if (modelReceive.IsOpen == false)
            modelReceive = connection.CreateModel();

          result = modelReceive.BasicGet(adminQueueName, false);
        }
        catch
        {
          result = null;
          Console.WriteLine("Could not communicate with RabbitMQ");
        }

        if (result != null)
        {
          string body = encoding.GetString(result.Body);
          string action = encoding.GetString((byte[])result.BasicProperties.Headers["Action"]);
          try
          {
            ProcessAction(action, body);
            modelReceive.BasicAck(result.DeliveryTag, false);
            StopListeners();
            CloseConnection();
            System.Environment.Exit(1);
          }
          catch (Exception ex)
          {
            Console.WriteLine(ex.Message);
          }
        }
        else
        {
          Thread.Sleep(1000);
        }
      }
    }

    private void StopListeners()
    {
      adminListener.CancelAsync();

    }

    private void CloseConnection()
    {
      modelSend.Close();
      connection.Close();
    }

    private void PublishToServer(string message, string action)
    {
      modelSend.PublishToQueue(serverQueueName, message, action, adminQueueName);
    }

    private void ProcessAction(string action, string body)
    {
      switch (action)
      {
        case "StartPeach":
          StartPeachReply(StartPeachResponse.Deserialize(body));
          break;
        case "StopPeach":
          StopPeachReply(StopPeachResponse.Deserialize(body));
          break;
        case "ResumePeach":
          break;
        case "Heartbeat":
          break;
        case "ListComputers":
          ListComputersReply(ListComputersResponse.Deserialize(body));
          break;
        case "ListErrors":
          ListErrorsReply(ListErrorsResponse.Deserialize(body));
          break;
      }
    }

    #endregion

  }
}
