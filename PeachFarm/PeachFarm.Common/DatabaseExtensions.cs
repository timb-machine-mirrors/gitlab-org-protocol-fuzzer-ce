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
				collection.Save(fault);
			}
			collection.Database.Server.Disconnect();
		}

		public static void SaveToDatabase(this Fault fault, string connectionString)
		{
			MongoCollection<Fault> collection = DatabaseHelper.GetCollection<Fault>(MongoNames.Faults, connectionString);
			collection.Save(fault);
			collection.Database.Server.Disconnect();
		}

		public static void UpdateNode(this Node node, string jobid, string connectionString)
		{
			MongoCollection<Job> collection = DatabaseHelper.GetCollection<Job>(MongoNames.Jobs, connectionString);

			var query = Query.EQ("JobID", jobid);
			

			collection.Database.Server.Disconnect();
		}

		public static MongoDB.Bson.ObjectId ToMongoID(this string input)
		{
			try
			{
				return new ObjectId(input);
			}
			catch
			{
				return MongoDB.Bson.ObjectId.Empty;
			}
		}
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
	}

	public partial class Fault
	{
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
	}
}
