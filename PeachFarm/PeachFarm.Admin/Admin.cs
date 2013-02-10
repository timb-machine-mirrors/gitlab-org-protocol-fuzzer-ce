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
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Configuration;

namespace PeachFarm.Admin
{
	public class Admin
	{
		private UTF8Encoding encoding = new UTF8Encoding();

		PeachFarm.Admin.Configuration.AdminSection config;

		public Admin(string serverHostName = "", int timeoutSeconds = 0)
		{
			config = (Configuration.AdminSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.admin");
			if (String.IsNullOrEmpty(serverHostName))
			{
				ServerHostName = config.Controller.IpAddress;
			}
			else
			{
				ServerHostName = serverHostName;
			}
			TimeoutSeconds = timeoutSeconds;
		}

		#region Properties
		public string ServerHostName { get; private set; }

		public int TimeoutSeconds { get; private set; }

		public bool IsListening { get; private set; }
		#endregion

		#region Methods
		public void StartAdmin()
		{
			try
			{
				OpenConnection(config.RabbitMq.HostName, config.RabbitMq.Port, config.RabbitMq.UserName, config.RabbitMq.Password);
				InitializeQueues();
				StartListeners();
			}
			catch (Exception ex)
			{
				RaiseAdminException(new ApplicationException("Could not open a connection to RabbitMQ host " + ServerHostName));
			}

		}

		public void StopAdmin()
		{
			StopListeners();
			CloseConnection();
		}
		#endregion

		#region AsyncCompletes

		#region StartPeachCompleted
		public class StartPeachCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
		{
			public StartPeachCompletedEventArgs(StartPeachResponse result)
				: base(null, false, null)
			{
				Result = result;
			}

			public StartPeachResponse Result { get; private set; }
		}

		public delegate void StartPeachCompletedEventHandler(object sender, StartPeachCompletedEventArgs e);

		public event StartPeachCompletedEventHandler StartPeachCompleted;

		private void RaiseStartPeachCompleted(StartPeachResponse result)
		{
			if (StartPeachCompleted != null)
				StartPeachCompleted(this, new StartPeachCompletedEventArgs(result));
		}
		#endregion

		#region StopPeachCompleted
		public class StopPeachCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
		{
			public StopPeachCompletedEventArgs(StopPeachResponse result)
				: base(null, false, null)
			{
				Result = result;
			}

			public StopPeachResponse Result { get; private set; }
		}

		public delegate void StopPeachCompletedEventHandler(object sender, StopPeachCompletedEventArgs e);

		public event StopPeachCompletedEventHandler StopPeachCompleted;

		private void RaiseStopPeachCompleted(StopPeachResponse result)
		{
			if (StopPeachCompleted != null)
				StopPeachCompleted(this, new StopPeachCompletedEventArgs(result));
		}
		#endregion

		#region ListNodesCompleted
		public class ListNodesCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
		{
			public ListNodesCompletedEventArgs(ListNodesResponse result)
				: base(null, false, null)
			{
				Result = result;
			}
			public ListNodesResponse Result { get; private set; }
		}

		public delegate void ListNodesCompletedEventHandler(object sender, ListNodesCompletedEventArgs e);

		public event ListNodesCompletedEventHandler ListNodesCompleted;

		private void RaiseListNodesCompleted(ListNodesResponse result)
		{
			if (ListNodesCompleted != null)
				ListNodesCompleted(this, new ListNodesCompletedEventArgs(result));
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

		#region JobInfoCompleted
		public class JobInfoCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
		{
			public JobInfoCompletedEventArgs(JobInfoResponse result)
				: base(null, false, null)
			{
				Result = result;
			}

			public JobInfoResponse Result { get; private set; }
		}

		public delegate void JobInfoCompletedEventHandler(object sender, JobInfoCompletedEventArgs e);

		public event JobInfoCompletedEventHandler JobInfoCompleted;

		private void RaiseJobInfoCompleted(JobInfoResponse result)
		{
			if (JobInfoCompleted != null)
				JobInfoCompleted(this, new JobInfoCompletedEventArgs(result));
		}
		#endregion
		#endregion

		#region AdminException
		public class ExceptionEventArgs : EventArgs
		{
			public ExceptionEventArgs(Exception ex)
			{
				this.Exception = ex;
			}

			public Exception Exception { get; private set; }
		}

		public event EventHandler<ExceptionEventArgs> AdminException;

		private void RaiseAdminException(Exception ex)
		{
			if (AdminException != null)
			{
				AdminException(this, new ExceptionEventArgs(ex));
			}
		}
		#endregion


		#region Sends

		#region StartPeach

		public void StartPeachAsync(string pitFilePath, string definesFilePath, int clientCount, string tagsString, string ip)
		{
			StartPeachRequest request = new StartPeachRequest();
			request.Pit = String.Format("<![CDATA[{0}]]", GetPitXml(pitFilePath));

			if (String.IsNullOrEmpty(definesFilePath) == false)
			{
				request.Defines = String.Format("<![CDATA[{0}]]", File.ReadAllText(definesFilePath));
			}

			if (String.IsNullOrEmpty(ip) == false)
			{
				request.IPAddress = System.Net.IPAddress.Parse(ip).ToString();
				request.ClientCount = 1;
				request.Tags = String.Empty;
			}
			else
			{
				request.IPAddress = String.Empty;
				request.ClientCount = clientCount;
				request.Tags = tagsString;
			}
			request.PitFileName = Path.GetFileNameWithoutExtension(pitFilePath);
			request.UserName = string.Format("{0}\\{1}", Environment.UserDomainName, Environment.UserName);
			PublishToServer(request.Serialize(), "StartPeach");
		}

		#endregion

		#region StopPeachAsync
		public void StopPeachAsync(string jobGuid)
		{
			StopPeachRequest request = new StopPeachRequest();
			request.UserName = string.Format("{0}\\{1}", Environment.UserDomainName, Environment.UserName);
			request.JobID = jobGuid;
			PublishToServer(request.Serialize(), "StopPeach");
		}

		/*
		public void StopPeachAsync()
		{
			StopPeachRequest request = new StopPeachRequest();
			request.UserName = string.Format("{0}\\{1}", Environment.UserDomainName, Environment.UserName);
			request.JobID = String.Empty;
			PublishToServer(request.Serialize(), "StopPeach");
		}
		//*/
		#endregion

		public void ListNodesAsync()
		{
			ListNodesRequest request = new ListNodesRequest();
			PublishToServer(request.Serialize(), "ListNodes");
		}

		public void ListErrorsAsync(string jobID = "")
		{
			ListErrorsRequest request = new ListErrorsRequest();
			request.JobID = jobID;
			PublishToServer(request.Serialize(), "ListErrors");
		}

		public void JobInfoAsync(string jobID)
		{
			JobInfoRequest request = new JobInfoRequest();
			request.JobID = jobID;
			PublishToServer(request.Serialize(), "JobInfo");
		}
		#endregion

		#region MQ functions

		private string serverQueueName;
		private string adminQueueName;

		private IConnection connection;
		private IModel modelSend;
		private IModel modelReceive;

		private BackgroundWorker adminListener = new BackgroundWorker();

		private void OpenConnection(string hostName, int port = -1, string userName = "guest", string password = "guest")
		{
			ConnectionFactory factory = new ConnectionFactory();
			factory.HostName = hostName;
			factory.Port = port;
			factory.UserName = userName;
			factory.Password = password;
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
			adminListener.RunWorkerCompleted += new RunWorkerCompletedEventHandler(adminListener_RunWorkerCompleted);
			adminListener.RunWorkerAsync();
		}

		void adminListener_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			IsListening = false;
			if (e.Cancelled)
			{

			}
			else if (e.Error != null)
			{

			}

		}

		private void Listen(object sender, DoWorkEventArgs e)
		{
			BasicGetResult result = null;
			int count = 0;
			while (!e.Cancel)
			{
				try
				{
					if (modelReceive.IsOpen == false)
						modelReceive = connection.CreateModel();

					result = modelReceive.BasicGet(adminQueueName, false);
				}
				catch (Exception ex)
				{
					RaiseAdminException(new ApplicationException("Could not communicate with RabbitMQ", ex));
					return;
				}

				if (result != null)
				{
					string body = encoding.GetString(result.Body);
					string action = encoding.GetString((byte[])result.BasicProperties.Headers["Action"]);
					try
					{
						ProcessAction(action, body);
						modelReceive.BasicAck(result.DeliveryTag, false);
						e.Cancel = true;
						StopAdmin();
					}
					catch (Exception ex)
					{
						RaiseAdminException(ex);
						return;
					}
				}
				else
				{
					Thread.Sleep(1000);
					if (TimeoutSeconds > 0)
					{
						count++;
						if (count == TimeoutSeconds)
						{
							RaiseAdminException(new ApplicationException("No response received before timeout."));
							return;
						}
					}
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
					RaiseStartPeachCompleted(StartPeachResponse.Deserialize(body));
					break;
				case "StopPeach":
					RaiseStopPeachCompleted(StopPeachResponse.Deserialize(body));
					break;
				case "ListNodes":
					RaiseListNodesCompleted(ListNodesResponse.Deserialize(body));
					break;
				case "ListErrors":
					RaiseListErrorsCompleted(ListErrorsResponse.Deserialize(body));
					break;
				case "JobInfo":
					RaiseJobInfoCompleted(JobInfoResponse.Deserialize(body));
					break;
				default:
					string message = String.Format("Could not process message\nAction: {0}\nBody:\n{1}", action, body);
					RaiseAdminException(new ApplicationException(message));
					break;
			}
		}

		#endregion

		private string GetPitXml(string pitFilePath)
		{
			XDocument doc = XDocument.Load(pitFilePath);
			return doc.ToString();
		}
	}
}
