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

namespace PeachFarm.Admin
{
	public class Admin
	{
		private UTF8Encoding encoding = new UTF8Encoding();

		public Admin(string serverHostName, int timeoutSeconds = 0)
		{
			ServerHostName = serverHostName;
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
				OpenConnection(ServerHostName);
			}
			catch (Exception ex)
			{
				RaiseAdminException(ex);
			}

			InitializeQueues();
			StartListeners();
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

		public void StartPeachAsync(string pitFilePath, int clientCount, string tagsString, string ip)
		{
			StartPeachRequest request = new StartPeachRequest();
			request.Pit = String.Format("<![CDATA[{0}]]", GetPitXml(pitFilePath));
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
			Console.WriteLine("waiting for result...");
			Console.ReadLine();
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

		public void ListComputersAsync()
		{
			ListComputersRequest request = new ListComputersRequest();
			PublishToServer(request.Serialize(), "ListComputers");
		}

		public void ListErrorsAsync(string jobID = "")
		{
			ListErrorsRequest request = new ListErrorsRequest();
			request.JobID = jobID;
			PublishToServer(request.Serialize(), "ListErrors");
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
				case "ListComputers":
					RaiseListComputersCompleted(ListComputersResponse.Deserialize(body));
					break;
				case "ListErrors":
					RaiseListErrorsCompleted(ListErrorsResponse.Deserialize(body));
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
