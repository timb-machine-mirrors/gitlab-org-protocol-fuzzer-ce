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
