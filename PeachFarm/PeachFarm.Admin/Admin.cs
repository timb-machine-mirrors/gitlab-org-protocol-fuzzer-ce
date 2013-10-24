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
	public class PeachFarmAdmin
	{
		Configuration.AdminSection config;

		private string controllerQueueName;
		private string adminQueueName;

		RabbitMqHelper rabbit = null;

		private const string configext = ".xml.config";
		private const string xmlext = ".xml";
		private const string zipext = ".zip";

		public PeachFarmAdmin(string adminName = "")
		{
			config = (Configuration.AdminSection)System.Configuration.ConfigurationManager.GetSection("peachfarm.admin");

			config.Validate();

			ServerHostName = config.Controller.IpAddress;
			MongoDbConnectionString = config.MongoDb.ConnectionString;

			IPAddress[] ipaddresses = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
			string ipAddress = (from i in ipaddresses where i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork select i).First().ToString();

			controllerQueueName = String.Format(QueueNames.QUEUE_CONTROLLER, ServerHostName);
			if (String.IsNullOrEmpty(adminName))
			{
				adminQueueName = String.Format(QueueNames.QUEUE_ADMIN, ipAddress);
			}
			else
			{
				adminQueueName = String.Format(QueueNames.QUEUE_ADMIN, adminName);
			}
			
			rabbit = new RabbitMqHelper(config.RabbitMq.HostName, config.RabbitMq.Port, config.RabbitMq.UserName, config.RabbitMq.Password, config.RabbitMq.SSL);
			rabbit.MessageReceived += new EventHandler<RabbitMqHelper.MessageReceivedEventArgs>(rabbit_MessageReceived);
			rabbit.StartListener(adminQueueName);
			this.IsListening = true;
		}

		#region Properties
		public string ServerHostName { get; private set; }

		public bool IsListening { get; private set; }

		public string MongoDbConnectionString { get; private set; }
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
		/*
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
		//*/
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

		public void StartPeachAsync(string pitFilePath, string definesFilePath, int clientCount, string tagsString, string ip, string target)
		{
			List<string> tempfiles = new List<string>();
			string zipfilepath = String.Empty;
			StartPeachRequest request = new StartPeachRequest();

			if (File.Exists(pitFilePath) == false)
			{
				throw new ApplicationException("File path does not exist: " + pitFilePath);
			}

			if((String.IsNullOrEmpty(definesFilePath) == false) && (File.Exists(definesFilePath) == false))
			{
				throw new ApplicationException("File path does not exist: " + definesFilePath);
			}

			request.PitFileName = Path.GetFileNameWithoutExtension(pitFilePath);

			if (String.IsNullOrEmpty(target))
			{
				request.Target = request.PitFileName;
			}
			else
			{
				request.Target = target;
			}

			string newdefinesfilepath = String.Empty;
			if (Path.GetExtension(pitFilePath) == xmlext)
			{
				zipfilepath = Path.Combine(Path.GetTempPath(), request.PitFileName + zipext);
				Ionic.Zip.ZipFile zipfile = new Ionic.Zip.ZipFile();
				zipfile.AddFile(pitFilePath, ".");
				if (String.IsNullOrEmpty(definesFilePath) == false)
				{
					if (definesFilePath.EndsWith(request.PitFileName + configext))
					{
						zipfile.AddFile(definesFilePath, ".");
					}
					else
					{
						newdefinesfilepath = Path.Combine(Path.GetTempPath(), request.PitFileName + configext);
						File.Copy(definesFilePath, newdefinesfilepath);
						zipfile.AddFile(newdefinesfilepath, ".");
						tempfiles.Add(newdefinesfilepath);
					}
				}
				zipfile.Save(zipfilepath);
				tempfiles.Add(zipfilepath);
			}
			else if(Path.GetExtension(pitFilePath) == zipext)
			{
				zipfilepath = pitFilePath;
			}

			#region get pit version if exists
			XDocument xdoc = null;
			try
			{
				xdoc = XDocument.Parse(GetPitXml(zipfilepath));
			}
			catch(Exception ex)
			{
				throw new ApplicationException("The pit file was not well formed.\n" + ex.ToString());
			}

			try
			{
				var versionAttrib = xdoc.Root.Attribute("version");
				if (versionAttrib != null)
					request.PitVersion = versionAttrib.Value;
			}
			catch { }
			#endregion

			var definesxml = GetConfigXml(zipfilepath);
			if (String.IsNullOrEmpty(definesxml) == false)
			{
				//request.Defines = String.Format("<![CDATA[{0}]]", definesxml);
				request.HasDefines = true;
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


			request.JobID = DatabaseHelper.GetJobID(MongoDbConnectionString);

			request.ZipFile = String.Format(Formats.JobFolder + "/{1}.zip", request.JobID, request.PitFileName);
			DatabaseHelper.SaveFileToGridFS(zipfilepath, request.ZipFile, MongoDbConnectionString);

			request.UserName = string.Format("{0}\\{1}", Environment.UserDomainName, Environment.UserName);
			PublishToServer(request.Serialize(), Actions.StartPeach);

			foreach (string tempfile in tempfiles)
			{
				try
				{
					File.Delete(tempfile);
				}
				catch { }
			}
		}

		#endregion

		#region StopPeachAsync
		public void StopPeachAsync(string jobGuid)
		{
			var job = DatabaseHelper.GetJob(jobGuid, MongoDbConnectionString);
			if (job == null)
			{
				throw new ApplicationException("Job does not exist: " + jobGuid);
			}
			else
			{
				StopPeachRequest request = new StopPeachRequest();
				request.UserName = string.Format("{0}\\{1}", Environment.UserDomainName, Environment.UserName);
				request.JobID = jobGuid;
				PublishToServer(request.Serialize(), Actions.StopPeach);
			}
		}
		#endregion

		public ListNodesResponse ListNodes()
		{
			//ListNodesRequest request = new ListNodesRequest();
			//PublishToServer(request.Serialize(), Actions.ListNodes);

			ListNodesResponse response = new ListNodesResponse();
			response.Nodes = DatabaseHelper.GetAllNodes(MongoDbConnectionString).ToList();
			return response;
		}

		public ListErrorsResponse ListErrors(string jobID = "")
		{
			//ListErrorsRequest request = new ListErrorsRequest();
			//request.JobID = jobID;
			//PublishToServer(request.Serialize(), Actions.ListErrors);

			ListErrorsResponse response = new ListErrorsResponse();
			//response.Nodes = errors;
			if (String.IsNullOrEmpty(jobID))
			{
				response.Errors = DatabaseHelper.GetAllErrors(MongoDbConnectionString);
			}
			else
			{
				response.Errors = DatabaseHelper.GetErrors(jobID, MongoDbConnectionString);
			}
			//RaiseListErrorsCompleted(response);
			return response;
		}

		public JobInfoResponse JobInfo(string jobID)
		{
			//JobInfoRequest request = new JobInfoRequest();
			//request.JobID = jobID;
			//PublishToServer(request.Serialize(), Actions.JobInfo);

			JobInfoResponse response = new JobInfoResponse();
			Common.Mongo.Job mongoJob = DatabaseHelper.GetJob(jobID, MongoDbConnectionString);
			if (mongoJob == null)
			{
				response.Success = false;
				response.ErrorMessage = String.Format("Job {0} does not exist.", jobID);
				response.Job = new Common.Messages.Job() { JobID = jobID };
			}
			else
			{
				response.Success = true;
				response.Job = new Common.Messages.Job(mongoJob);
				var nodes = DatabaseHelper.GetAllNodes(MongoDbConnectionString);
				response.Nodes = (from Heartbeat h in nodes where (h.Status == Status.Running) && (h.JobID == jobID) select h).ToList();
			}

			//RaiseJobInfoCompleted(response);
			return response;
		}

		public MonitorResponse Monitor()
		{
			//PublishToServer(new MonitorRequest().Serialize(), Actions.Monitor);

			MonitorResponse response = new MonitorResponse();
			response.MongoDbConnectionString = MongoDbConnectionString;

			response.Nodes = DatabaseHelper.GetAllNodes(MongoDbConnectionString);
			var activeJobs = response.Nodes.GetJobs(MongoDbConnectionString);
			var allJobs = DatabaseHelper.GetAllJobs(MongoDbConnectionString);
			response.ActiveJobs = activeJobs.ToMessagesJobs();
			response.InactiveJobs = allJobs.Except(activeJobs, new JobComparer()).ToMessagesJobs();

			response.Errors = DatabaseHelper.GetAllErrors(MongoDbConnectionString);
			//RaiseMonitorCompleted(response);
			return response;
		}


		#endregion

		#region MQ functions
		private void PublishToServer(string message, string action)
		{
			rabbit.PublishToQueue(controllerQueueName, message, action, adminQueueName);
		}

		void rabbit_MessageReceived(object sender, RabbitMqHelper.MessageReceivedEventArgs e)
		{
			ProcessAction(e.Action, e.Body);
			rabbit.StopListener();
			//rabbit.CloseConnection();
			this.IsListening = false;
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
				//case Actions.ListNodes:
				//  RaiseListNodesCompleted(ListNodesResponse.Deserialize(body));
				//  break;
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
			if (Path.GetExtension(pitFilePath) == zipext)
			{
			  //var zip = Telerik.Windows.Zip.ZipPackage.OpenFile(pitFilePath, FileAccess.Read);
				var zip = Ionic.Zip.ZipFile.Read(pitFilePath);
			  var pitfilename = Path.GetFileNameWithoutExtension(pitFilePath) + xmlext;
			  var pitfile = (from e in zip.Entries where e.FileName == pitfilename select e).FirstOrDefault();

			  if (pitfile == null)
			    throw new ApplicationException("Your zip package must contain a Pit file with the name: " + pitfilename);

				var stream = pitfile.OpenReader();
			  XDocument doc = XDocument.Load(stream);
			  stream.Close();
			  return doc.ToString();
			}
			else if (Path.GetExtension(pitFilePath) == xmlext)
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
			if (Path.GetExtension(pitFilePath) == zipext)
			{
				//var zip = Telerik.Windows.Zip.ZipPackage.OpenFile(pitFilePath, FileAccess.Read);
				var zip = Ionic.Zip.ZipFile.Read(pitFilePath);
				var definesfilename = Path.GetFileNameWithoutExtension(pitFilePath) + configext;
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



		public void DumpFiles(string jobID, string destinationFolder)
		{
			FileWriter.DumpFiles(MongoDbConnectionString, destinationFolder, jobID);
		}

#if DEBUG
		public void Report(string jobid)
		{
			GenerateFaultReportRequest request = new GenerateFaultReportRequest();
			request.JobID = jobid;
			request.ReportFormat = ReportFormat.PDF;

			rabbit.PublishToQueue(QueueNames.QUEUE_REPORTGENERATOR_PROCESSONE, request.Serialize(), Actions.GenerateFaultReport, this.controllerQueueName);
		}

		public void TruncateAllCollections()
		{
			DatabaseHelper.TruncateAllCollections(MongoDbConnectionString);
		}
#endif
	}
}
