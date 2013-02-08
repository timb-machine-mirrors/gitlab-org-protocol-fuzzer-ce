using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver;
using System.Xml.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Builders;

namespace PeachFarm.Common.Mongo
{
	public static class ExtensionMethods
	{
		public static List<Job> DatabaseInsert(this List<Job> jobs, string connectionString)
		{
			string collectionname = MongoNames.Jobs;
			MongoServer server = MongoServer.Create(connectionString);
			MongoDatabase db = server.GetDatabase(MongoNames.Database);

			MongoCollection<Job> collection = null;

			if (db.CollectionExists(collectionname))
			{
				collection = db.GetCollection<Job>(collectionname);
			}
			else
			{
				db.CreateCollection(collectionname);
				collection = db.GetCollection<Job>(collectionname);
				//collection.CreateIndex(new string[] { "JobID", "TestName", "ComputerName" });
			}

			foreach (Job job in jobs)
			{
				collection.Insert(job);
			}


			server.Disconnect();

			return jobs;
		}

		public static Job DatabaseInsert(this Job job, string connectionString)
		{
			string collectionname = MongoNames.Jobs;
			MongoServer server = MongoServer.Create(connectionString);
			MongoDatabase db = server.GetDatabase(MongoNames.Database);

			MongoCollection<Job> collection = null;

			if (db.CollectionExists(collectionname))
			{
				collection = db.GetCollection<Job>(collectionname);
			}
			else
			{
				db.CreateCollection(collectionname);
				collection = db.GetCollection<Job>(collectionname);
				//collection.CreateIndex(new string[] { "JobID", "TestName", "ComputerName" });
			}

			collection.Insert(job);
			server.Disconnect();

			return job;
		}

		public static Iteration DatabaseInsert(this Iteration iteration, string connectionString)
		{
			string collectionname = MongoNames.Iterations;
			MongoServer server = MongoServer.Create(connectionString);
			MongoDatabase db = server.GetDatabase(MongoNames.Database);

			MongoCollection<Iteration> collection = null;

			if (db.CollectionExists(collectionname))
			{
				collection = db.GetCollection<Iteration>(collectionname);
			}
			else
			{
				db.CreateCollection(collectionname);
				collection = db.GetCollection<Iteration>(collectionname);
				//collection.CreateIndex(new string[] { "JobID" });
			}

			collection.Insert(iteration);

			server.Disconnect();

			return iteration;
		}

		public static Messages.Heartbeat DatabaseInsert(this Messages.Heartbeat heartbeat, string connectionString)
		{
			string collectionName = MongoNames.PeachFarmErrors;
			MongoServer server = MongoServer.Create(connectionString);
			MongoDatabase db = server.GetDatabase(MongoNames.Database);

			MongoCollection<Messages.Heartbeat> collection = null;

			if (db.CollectionExists(collectionName))
			{
				collection = db.GetCollection<Messages.Heartbeat>(collectionName);
			}
			else
			{
				db.CreateCollection(collectionName);
				collection = db.GetCollection<Messages.Heartbeat>(collectionName);
				//collection.CreateIndex(new string[] { "JobID" });
			}

			collection.Insert(heartbeat);


			server.Disconnect();

			return heartbeat;
		}

		public static List<Iteration> GetIterations(this Job job, string connectionString)
		{
			return new List<Iteration>();
		}
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

	public partial class Job
	{
		/*
		public Job()
		{
			this.JobID = Guid.NewGuid();
		}
		//*/

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
