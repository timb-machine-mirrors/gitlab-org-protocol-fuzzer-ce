using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver;
using System.Xml.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Builders;
using System.Diagnostics;

namespace PeachFarm.Common.Mongo
{
	public static class ExtensionMethods
	{
		public static List<Job> DatabaseInsert(this List<Job> jobs, string connectionString)
		{
			MongoCollection<Job> collection = DatabaseHelper.GetCollection<Job>(MongoNames.Jobs, connectionString);

			foreach (Job job in jobs)
			{
				collection.Insert(job);
			}

			collection.Database.Server.Disconnect();

			return jobs;
		}

		public static Job DatabaseInsert(this Job job, string connectionString)
		{
			MongoCollection<Job> collection = DatabaseHelper.GetCollection<Job>(MongoNames.Jobs, connectionString);

			collection.Insert(job);
			collection.Database.Server.Disconnect();

			return job;
		}

		public static Iteration DatabaseInsert(this Iteration iteration, string connectionString)
		{
			MongoCollection<Iteration> collection = DatabaseHelper.GetCollection<Iteration>(MongoNames.Iterations, connectionString);

			collection.Insert(iteration);

			collection.Database.Server.Disconnect();

			Debug.WriteLine("******* WRITING ITERATION TO DATABASE ******");

			return iteration;
		}

		public static Messages.Heartbeat DatabaseInsert(this Messages.Heartbeat heartbeat, string connectionString)
		{
			MongoCollection<Messages.Heartbeat> collection = DatabaseHelper.GetCollection<Messages.Heartbeat>(MongoNames.PeachFarmErrors, connectionString);
			collection.Insert(heartbeat);


			collection.Database.Server.Disconnect();

			return heartbeat;
		}

		public static MongoCursor<Iteration> GetIterationsCursor(this Job job, string connectionString)
		{
			MongoCollection<Iteration> collection = DatabaseHelper.GetCollection<Iteration>(MongoNames.Iterations, connectionString);
			var query = Query.EQ("JobID", job.JobID);
			return collection.Find(query);
		}

		public static List<Job> GetJobs(this List<PeachFarm.Common.Messages.Heartbeat> nodes, string connectionString)
		{
			MongoCollection<Job> collection = DatabaseHelper.GetCollection<Job>(MongoNames.Jobs, connectionString);
			var jobids = (from PeachFarm.Common.Messages.Heartbeat h in nodes where !String.IsNullOrEmpty(h.JobID) select BsonValue.Create(h.JobID)).Distinct();
			var query = Query.In("JobID", jobids);
			return collection.Find(query).OrderBy(k => k.StartDate).ToList();
		}

		public static List<Messages.Job> ToMessagesJobs(this List<Mongo.Job> mongoJobs)
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
	}
	

	public partial class Iteration
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

	public partial class Job
	{
		public Job() { }

		public Job(Messages.Job mJob)
		{
			this.JobID = mJob.JobID;
			this.PitFileName = mJob.PitFileName;
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
					return _id.AsString;
			}
		}

	}

}

namespace PeachFarm.Common.Messages
{
	public partial class Heartbeat
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
					return _id.AsString;
			}
		}
	}
}
