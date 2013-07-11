using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver;
using System.Xml.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;

namespace PeachFarm.Common.Mongo
{
	public static class ExtensionMethods
	{
		public static Job SaveToDatabase(this Job job, string connectionString)
		{
			MongoCollection<Job> collection = DatabaseHelper.GetCollection<Job>(MongoNames.Jobs, connectionString);

			collection.Save(job);
			collection.Database.Server.Disconnect();

			return job;
		}

		private static string FormatDate(DateTime dateTime)
		{
			return String.Format("{0:yyyyMMddhhmmss}", dateTime);
		}

		public static Messages.Heartbeat SaveToDatabase(this Messages.Heartbeat heartbeat, string connectionString)
		{
			MongoCollection<Messages.Heartbeat> collection = DatabaseHelper.GetCollection<Messages.Heartbeat>(MongoNames.PeachFarmNodes, connectionString);
			var query = Query.EQ("NodeName", heartbeat.NodeName);
			var storedHeartbeat = collection.FindOne(query);
			if (storedHeartbeat == null)
			{
				collection.Save(heartbeat);
				storedHeartbeat = heartbeat;
			}
			else
			{
				storedHeartbeat.Iteration = heartbeat.Iteration;
				storedHeartbeat.JobID = heartbeat.JobID;
				storedHeartbeat.PitFileName = heartbeat.PitFileName;
				storedHeartbeat.Seed = heartbeat.Seed;
				storedHeartbeat.Stamp = heartbeat.Stamp;
				storedHeartbeat.Tags = heartbeat.Tags;
				storedHeartbeat.UserName = heartbeat.UserName;
				storedHeartbeat.QueueName = heartbeat.QueueName;
				storedHeartbeat.ErrorMessage = heartbeat.ErrorMessage;
				storedHeartbeat.Status = heartbeat.Status;
				collection.Save(storedHeartbeat);
			}
			collection.Database.Server.Disconnect();

			return storedHeartbeat;
		}

		public static void RemoveFromDatabase(this Messages.Heartbeat heartbeat, string connectionString)
		{
			MongoCollection<Messages.Heartbeat> collection = DatabaseHelper.GetCollection<Messages.Heartbeat>(MongoNames.PeachFarmNodes, connectionString);
			var query = Query.EQ("NodeName", heartbeat.NodeName);
			if(collection.FindOne(query) != null)
			{
				collection.Remove(query);
			}
			collection.Database.Server.Disconnect();

			return;
		}
		
		public static Messages.Heartbeat SaveToErrors(this Messages.Heartbeat heartbeat, string connectionString)
		{
			MongoCollection<Messages.Heartbeat> collection = DatabaseHelper.GetCollection<Messages.Heartbeat>(MongoNames.PeachFarmErrors, connectionString);
			heartbeat._id = BsonObjectId.Empty;
			collection.Save(heartbeat);
			collection.Database.Server.Disconnect();

			return heartbeat;
		}

		public static List<Job> GetJobs(this List<PeachFarm.Common.Messages.Heartbeat> nodes, string connectionString)
		{
			MongoCollection<Job> collection = DatabaseHelper.GetCollection<Job>(MongoNames.Jobs, connectionString);
			var jobids = (from PeachFarm.Common.Messages.Heartbeat h in nodes where !String.IsNullOrEmpty(h.JobID) select BsonValue.Create(h.JobID)).Distinct();
			var query = Query.In("JobID", jobids);
			return collection.Find(query).OrderBy(k => k.StartDate).ToList();
		}

		public static void FillNodes(this Job job, string connectionString)
		{
			MongoCollection<Node> collection = DatabaseHelper.GetCollection<Node>(MongoNames.JobNodes, connectionString);
			var query = Query.EQ("JobID", job.JobID);
			job.Nodes = collection.Find(query).ToList();
			collection.Database.Server.Disconnect();
		}

		private static string[] dataFields = new string[]
		{
		    "Faults.CollectedData.Data",
		    "StateModel.Data"
		};

		public static void FillFaults(this Job job, string connectionString, bool excludeData = false)
		{
			MongoCollection<Fault> collection = DatabaseHelper.GetCollection<Fault>(MongoNames.Faults, connectionString);
			var query = Query.EQ("JobID", job.JobID);
			if(excludeData)
				job.Faults = collection.Find(query).SetSortOrder("Group").SetFields(Fields.Exclude(dataFields)).ToList();
			else
				job.Faults = collection.Find(query).SetSortOrder("Group").ToList();

			foreach (var fault in job.Faults)
			{
				fault.Stamp = fault.Stamp.ToLocalTime();
			}
			collection.Database.Server.Disconnect();
		}

		public static void FillFaults(this Node node, string connectionString, bool excludeData = false)
		{
			MongoCollection<Fault> collection = DatabaseHelper.GetCollection<Fault>(MongoNames.Faults, connectionString);
			var query = Query.And(Query.EQ("JobID", node.JobID), Query.EQ("NodeName", node.Name));
			if (excludeData)
				node.Faults = collection.Find(query).SetFields(Fields.Exclude(dataFields)).ToList();
			else
				node.Faults = collection.Find(query).ToList(); 
			
			foreach (var fault in node.Faults)
			{
				fault.Stamp = fault.Stamp.ToLocalTime();
			} 
			collection.Database.Server.Disconnect();
		}

		public static List<Messages.Job> ToMessagesJobs(this IEnumerable<Mongo.Job> mongoJobs)
		{
			List<Messages.Job> jobs = new List<Messages.Job>();
			foreach (Mongo.Job mongoJob in mongoJobs)
			{
				jobs.Add(new Messages.Job(mongoJob));
			}
			return jobs;
		}

		public static List<Messages.Heartbeat> GetErrors(this List<Messages.Heartbeat> nodes, string connectionString)
		{
			MongoCollection<Messages.Heartbeat> collection = DatabaseHelper.GetCollection<Messages.Heartbeat>(MongoNames.PeachFarmErrors, connectionString);
			var nodenames = (from PeachFarm.Common.Messages.Heartbeat h in nodes select BsonValue.Create(h.NodeName)).Distinct();
			var query = Query.In("NodeName", nodenames);
			return collection.Find(query).OrderBy(k => k.NodeName).OrderBy(k => k.JobID).ToList();
		}

		public static Node SaveToDatabase(this Node node, string connectionString)
		{
			MongoCollection<Node> collection = DatabaseHelper.GetCollection<Node>(MongoNames.JobNodes, connectionString);
			collection.Save(node);
			collection.Database.Server.Disconnect();
			return node;
		}

		public static void SaveToDatabase(this List<PeachFarm.Common.Mongo.Fault> faults, string connectionString)
		{
			MongoCollection<Fault> collection = DatabaseHelper.GetCollection<Fault>(MongoNames.Faults, connectionString);
			foreach (var fault in faults)
			{
				//var updatedfault = fault.UpdateDataPaths(connectionString);
				collection.Save(fault);
			}
			collection.Database.Server.Disconnect();
		}

		public static void SaveToDatabase(this Fault fault, string connectionString)
		{
			MongoCollection<Fault> collection = DatabaseHelper.GetCollection<Fault>(MongoNames.Faults, connectionString);
			//fault = fault.UpdateDataPaths(connectionString);
			collection.Save(fault);
			collection.Database.Server.Disconnect();
		}

		public static void UpdateNode(this Node node, string jobid, string connectionString)
		{
			MongoCollection<Job> collection = DatabaseHelper.GetCollection<Job>(MongoNames.Jobs, connectionString);

			var query = Query.EQ("JobID", jobid);
			

			collection.Database.Server.Disconnect();
		}

		#region old code
		/*
		private static Fault UpdateDataPaths(this Fault fault, string connectionString)
		{
			string jobFolder = String.Empty;
			string nodeFolder = String.Empty;
			string testFolder = String.Empty;
			string faultsFolder = String.Empty;
			string statusFile = String.Empty;
			string faultFolder = String.Empty;
			string iterationFolder = String.Empty;
			string actionFile = String.Empty;
			string collectedDataFile = String.Empty;

			Job job = DatabaseHelper.GetJob(fault.JobID, connectionString);

			jobFolder = String.Format("Job_{0}_{1}", job.JobID, job.Pit.FileName);
			nodeFolder = Path.Combine(jobFolder, "Node_" + fault.NodeName);
			testFolder = Path.Combine(nodeFolder, String.Format("{0}_{1}_{2}", job.Pit.FileName, fault.TestName, FormatDate(job.StartDate)));
			faultsFolder = Path.Combine(testFolder, "Faults");

			#region set faultFolder
			if (fault.FolderName != null)
			{
				faultFolder = Path.Combine(faultsFolder, fault.FolderName);
			}
			else if (String.IsNullOrEmpty(fault.MajorHash) && String.IsNullOrEmpty(fault.MinorHash) && String.IsNullOrEmpty(fault.Exploitability))
			{
				faultFolder = Path.Combine(faultsFolder, "Unknown");
			}
			else
			{
				faultFolder = Path.Combine(faultsFolder, String.Format("{0}_{1}_{2}", fault.Exploitability, fault.MajorHash, fault.MinorHash));
			}
			#endregion

			iterationFolder = Path.Combine(faultFolder, fault.Iteration.ToString());

			#region action files
			int cnt = 0;
			foreach (PeachFarm.Common.Mongo.Action action in fault.StateModel)
			{
				cnt++;
				if (action.Parameter == 0)
				{
					actionFile = System.IO.Path.Combine(iterationFolder, string.Format("action_{0}_{1}_{2}.txt",
									cnt, action.ActionType.ToString(), action.ActionName));

				}
				else
				{
					actionFile = System.IO.Path.Combine(iterationFolder, string.Format("action_{0}-{1}_{2}_{3}.txt",
									cnt, action.Parameter, action.ActionType.ToString(), action.ActionName));
				}
				
				DatabaseHelper.SaveToGridFS(action.Data, actionFile, connectionString);

				action.DataPath = actionFile;
			}
			#endregion


			#region write collected data files
			foreach (CollectedData cd in fault.CollectedData)
			{
				collectedDataFile = System.IO.Path.Combine(iterationFolder,
					fault.DetectionSource + "_" + cd.Key);

				DatabaseHelper.SaveToGridFS(cd.Data, collectedDataFile, connectionString);

				cd.DataPath = collectedDataFile;
			}
			#endregion

			return fault;
		}
		//*/
		#endregion
	}
	
	public partial class Job
	{
		public Job(Messages.Job mJob)
		{
			Nodes = new List<Node>();

			this.JobID = mJob.JobID;
			this.Pit = new Pit(mJob.Pit);
			this.StartDate = mJob.StartDate;
			this.UserName = mJob.UserName;
		}

		[XmlIgnore]
		public BsonObjectId _id { get; set; }

		[XmlAttribute]
		[BsonIgnore]
		public string ID
		{
			get
			{
				if ((_id == null) || (_id == BsonObjectId.Empty))
					return String.Empty;
				else
					return _id.ToString();
			}
		}

		private List<Node> nodesField = new List<Node>();
		
		[BsonIgnore]
		public List<Node> Nodes
		{
			get { return nodesField; }
			set { nodesField = value; }
		}

		private List<Fault> faultsField = new List<Fault>();

		[BsonIgnore]
		public List<Fault> Faults
		{
			get { return faultsField; }
			set { faultsField = value; }
		}

		[BsonIgnore]
		[XmlIgnore]
		public string JobFolder
		{
			get
			{
				return String.Format(Formats.JobFolder, this.JobID, this.Pit.FileName);
			}
		}
	}

	public partial class Pit
	{
		public Pit() { }

		public Pit(Messages.Pit mPit)
		{
			this.FileName = mPit.FileName;
			this.FullText = mPit.FullText;
			this.Version = mPit.Version;
		}
	}

	public partial class Node
	{
		public Node()
		{
			Faults = new List<Fault>();
		}

		[XmlIgnore]
		public BsonObjectId _id { get; set; }

		[XmlAttribute]
		[BsonIgnore]
		[DataMember]
		public string ID
		{
			get
			{
				if ((_id == null) || (_id == BsonObjectId.Empty))
					return String.Empty;
				else
					return _id.ToString();
			}
		}

		[BsonIgnore]
		public List<Fault> Faults { get; set; }

		[BsonIgnore]
		public string NodeFolder
		{
			get { return "Node_" + this.Name; }
		}
	}

	public partial class Fault
	{
		[XmlIgnore]
		public BsonObjectId _id { get; set; }

		[XmlAttribute]
		[BsonIgnore]
		[DataMember]
		public string ID
		{
			get
			{
				if ((_id == null) || (_id == BsonObjectId.Empty))
					return String.Empty;
				else
					return _id.ToString();
			}
		}
	}

	/*
	public partial class CollectedData
	{
		private byte[] dataField;

		[XmlIgnore]
		[BsonIgnore]
		public byte[] Data { get; set; }
	}

	public partial class Action
	{
		private byte[] dataField;

		[XmlIgnore]
		[BsonIgnore]
		public byte[] Data { get; set; }
	}
	//*/
}
