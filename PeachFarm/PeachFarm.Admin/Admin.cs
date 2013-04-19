using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Net;

using PeachFarm.Common;
using PeachFarm.Common.Messages;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Configuration;
using PeachFarm.Common.Mongo;

namespace PeachFarm.Admin
{
	public class Admin
	{
		private UTF8Encoding encoding = new UTF8Encoding();

		PeachFarm.Admin.Configuration.AdminSection config;

		private string serverQueueName;
		private string adminQueueName;

		RabbitMqHelper rabbit = null;

		public Admin(string adminQueueOverride = "")
		{
			config = (Configuration.AdminSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.admin");
			ServerHostName = config.Controller.IpAddress;

			IPAddress[] ipaddresses = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
			string ipAddress = (from i in ipaddresses where i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork select i).First().ToString();

			serverQueueName = String.Format(QueueNames.QUEUE_CONTROLLER, ServerHostName);
			if (String.IsNullOrEmpty(adminQueueOverride))
			{
				adminQueueName = String.Format(QueueNames.QUEUE_ADMIN, ipAddress);
			}
			else
			{
				adminQueueName = adminQueueOverride;
			}
			
			rabbit = new RabbitMqHelper(config.RabbitMq.HostName, config.RabbitMq.Port, config.RabbitMq.UserName, config.RabbitMq.Password, config.RabbitMq.SSL);
			rabbit.MessageReceived += new EventHandler<RabbitMqHelper.MessageReceivedEventArgs>(rabbit_MessageReceived);
			rabbit.StartListener(adminQueueName);
			this.IsListening = true;
		}

		void rabbit_MessageReceived(object sender, RabbitMqHelper.MessageReceivedEventArgs e)
		{
			ProcessAction(e.Action, e.Body);
			rabbit.StopListener();
			//rabbit.CloseConnection();
			this.IsListening = false;
		}



		#region Properties
		public string ServerHostName { get; private set; }

		public bool IsListening { get; private set; }
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

		#region MonitorCompleted
		public class MonitorCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
		{
			public MonitorCompletedEventArgs(MonitorResponse result)
				: base(null, false, null)
			{
				Result = result;
			}

			public MonitorResponse Result { get; private set; }
		}

		public delegate void MonitorCompletedEventHandler(object sender, MonitorCompletedEventArgs e);

		public event MonitorCompletedEventHandler MonitorCompleted;

		private void RaiseMonitorCompleted(MonitorResponse result)
		{
			if (MonitorCompleted != null)
				MonitorCompleted(this, new MonitorCompletedEventArgs(result));
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

			if (File.Exists(pitFilePath) == false)
			{
				throw new ApplicationException("File path does not exist: " + pitFilePath);
			}

			if (String.IsNullOrEmpty(definesFilePath))
			{
				if (Path.GetExtension(pitFilePath) == ".zip")
				{
					var definesxml = GetConfigXml(pitFilePath);
					request.Defines = String.Format("<![CDATA[{0}]]", definesxml);
				}
			}
			else
			{
				if (File.Exists(definesFilePath))
				{
					request.Defines = String.Format("<![CDATA[{0}]]", File.ReadAllText(definesFilePath));
				}
				else
				{
					throw new ApplicationException("File path does not exist: " + definesFilePath);
				}
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
			request.Pit = String.Format("<![CDATA[{0}]]", GetPitXml(pitFilePath));

			request.JobID = DatabaseHelper.GetJobID(config.MongoDb.ConnectionString);

			if (Path.GetExtension(pitFilePath) == ".zip")
			{
				request.ZipFile = String.Format("Job_{0}_{1}\\{1}.zip", request.JobID, request.PitFileName);
				DatabaseHelper.SaveFileToGridFS(pitFilePath, request.ZipFile, config.MongoDb.ConnectionString);
			}
			request.UserName = string.Format("{0}\\{1}", Environment.UserDomainName, Environment.UserName);
			PublishToServer(request.Serialize(), Actions.StartPeach);
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
		#endregion

		public void ListNodesAsync()
		{
			ListNodesRequest request = new ListNodesRequest();
			PublishToServer(request.Serialize(), Actions.ListNodes);
		}

		public void ListErrorsAsync(string jobID = "")
		{
			ListErrorsRequest request = new ListErrorsRequest();
			request.JobID = jobID;
			PublishToServer(request.Serialize(), Actions.ListErrors);
		}

		public void JobInfoAsync(string jobID)
		{
			JobInfoRequest request = new JobInfoRequest();
			request.JobID = jobID;
			PublishToServer(request.Serialize(), Actions.JobInfo);
		}

		public void MonitorAsync()
		{
			PublishToServer(new MonitorRequest().Serialize(), Actions.Monitor);
		}
		#endregion

		#region MQ functions
		private void PublishToServer(string message, string action)
		{
			rabbit.PublishToQueue(serverQueueName, message, action, adminQueueName);
		}

		private void ProcessAction(string action, string body)
		{
			switch (action)
			{
				case Actions.StartPeach:
					RaiseStartPeachCompleted(StartPeachResponse.Deserialize(body));
					break;
				case Actions.StopPeach:
					RaiseStopPeachCompleted(StopPeachResponse.Deserialize(body));
					break;
				case Actions.ListNodes:
					RaiseListNodesCompleted(ListNodesResponse.Deserialize(body));
					break;
				case Actions.ListErrors:
					RaiseListErrorsCompleted(ListErrorsResponse.Deserialize(body));
					break;
				case Actions.JobInfo:
					RaiseJobInfoCompleted(JobInfoResponse.Deserialize(body));
					break;
				case Actions.Monitor:
					RaiseMonitorCompleted(MonitorResponse.Deserialize(body));
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
			if (Path.GetExtension(pitFilePath) == ".zip")
			{
			  //var zip = Telerik.Windows.Zip.ZipPackage.OpenFile(pitFilePath, FileAccess.Read);
				var zip = Ionic.Zip.ZipFile.Read(pitFilePath);
			  var pitfilename = Path.GetFileNameWithoutExtension(pitFilePath) + ".xml";
			  var pitfile = (from e in zip.Entries where e.FileName == pitfilename select e).FirstOrDefault();

			  if (pitfile == null)
			    throw new ApplicationException("Your zip package must contain a Pit file with the name: " + pitfilename);

				var stream = pitfile.OpenReader();
			  XDocument doc = XDocument.Load(stream);
			  stream.Close();
			  return doc.ToString();
			}
			else if (Path.GetExtension(pitFilePath) == ".xml")
			{
			  XDocument doc = XDocument.Load(pitFilePath);
			  return doc.ToString();
			}
			else
			{
			  throw new ApplicationException("Unsupported file extension. Peach Farm only accepts .xml and .zip files");
			}
		}

		private string GetConfigXml(string pitFilePath)
		{
			if (Path.GetExtension(pitFilePath) == ".zip")
			{
				//var zip = Telerik.Windows.Zip.ZipPackage.OpenFile(pitFilePath, FileAccess.Read);
				var zip = Ionic.Zip.ZipFile.Read(pitFilePath);
				var definesfilename = Path.GetFileNameWithoutExtension(pitFilePath) + ".xml.config";
				var definesfile = (from e in zip.Entries where e.FileName == definesfilename select e).FirstOrDefault();

				//if (pitfile == null)
				//  throw new ApplicationException("Your zip package must contain a Pit file with the name: " + pitfilename);
				if (definesfile == null)
				{
					return String.Empty;
				}
				else
				{
					var stream = definesfile.OpenReader();
					XDocument doc = XDocument.Load(stream);
					stream.Close();
					return doc.ToString();
				}
			}
			else
			{
				return String.Empty;
			}
		}


	}
}
