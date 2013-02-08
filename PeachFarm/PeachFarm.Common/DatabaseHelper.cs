using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver;
using PeachFarm.Common.Messages;
using System.IO;
using MongoDB.Driver.Builders;

namespace PeachFarm.Common.Mongo
{
	public class DatabaseHelper
	{
		public static bool TestConnection(string connectionString)
		{
			bool result = true;
			try
			{
				MongoServer server = MongoServer.Create(connectionString);
				server.Connect();
				CreateCollections(server);
				server.Disconnect();
			}
			catch
			{
				result = false;
			}
			return result;
		}

		public static void CreateCollections(MongoServer server)
		{
			CreateJobsCollection(server);
			CreateIterationsCollection(server);
		}

		public static void CreateJobsCollection(MongoServer server)
		{
			string collectionname = MongoNames.Jobs;
			MongoDatabase db = server.GetDatabase(MongoNames.Database);

			MongoCollection<Job> collection;
			if (!db.CollectionExists(collectionname))
			{
				db.CreateCollection(collectionname);
				collection = db.GetCollection<Job>(collectionname);
				collection.CreateIndex(new string[] { "JobID" });
			}

		}

		public static void CreateIterationsCollection(MongoServer server)
		{
			string collectionname = MongoNames.Iterations;
			MongoDatabase db = server.GetDatabase(MongoNames.Database);

			MongoCollection<Job> collection;
			if (!db.CollectionExists(collectionname))
			{
				db.CreateCollection(collectionname);
				collection = db.GetCollection<Job>(collectionname);
				collection.CreateIndex(new string[] { "JobID" });
				collection.CreateIndex(new string[] { "NodeName" });
				collection.CreateIndex(new string[] { "IterationNumber" });
			}
		}

		public static void CreateErrorsCollection(MongoServer server)
		{
			string collectionname = MongoNames.PeachFarmErrors;
			MongoDatabase db = server.GetDatabase(MongoNames.Database);

			MongoCollection<Job> collection;
			if (!db.CollectionExists(collectionname))
			{
				db.CreateCollection(collectionname);
				collection = db.GetCollection<Job>(collectionname);
				collection.CreateIndex(new string[] { "JobID" });
				collection.CreateIndex(new string[] { "NodeName" });
			}
		}
		//public static Job GetJob(Guid jobGuid, string connectionString)
		public static Job GetJob(string jobGuid, string connectionString)
		{
			MongoCollection<Job> collection = GetJobsCollection(connectionString);
			var query = Query.EQ("JobID", jobGuid);
			return collection.FindOne(query);
		}

		public static List<Job> GetAllJobs(string connectionString)
		{
			MongoCollection<Job> collection = GetJobsCollection(connectionString);
			return collection.FindAll().ToList();
		}

		public static List<Messages.Heartbeat> GetErrors(string connectionString)
		{
			string collectionName = MongoNames.PeachFarmErrors;
			MongoServer server = MongoServer.Create(connectionString);
			MongoDatabase db = server.GetDatabase(MongoNames.Database);

			MongoCollection<Heartbeat> collection;

			if (db.CollectionExists(collectionName))
			{
				collection = db.GetCollection<Heartbeat>(collectionName);
			}
			else
			{
				db.CreateCollection(collectionName);
				collection = db.GetCollection<Heartbeat>(collectionName);
				//collection.CreateIndex(new string[] { "JobID", "TestName", "ComputerName" });
			}


			if (db.CollectionExists(collectionName) == false)
			{
				throw new ApplicationException("Database does not exist.");
			}

			return collection.FindAll().ToList();
		}

		public static List<Messages.Heartbeat> GetErrors(string jobID, string connectionString)
		{
			string collectionName = MongoNames.PeachFarmErrors;
			MongoServer server = MongoServer.Create(connectionString);
			MongoDatabase db = server.GetDatabase(MongoNames.Database);

			MongoCollection<Heartbeat> collection;

			if (db.CollectionExists(collectionName))
			{
				collection = db.GetCollection<Heartbeat>(collectionName);
			}
			else
			{
				db.CreateCollection(collectionName);
				collection = db.GetCollection<Heartbeat>(collectionName);
				//collection.CreateIndex(new string[] { "JobID", "TestName", "ComputerName" });
			}


			if (db.CollectionExists(collectionName) == false)
			{
				throw new ApplicationException("Database does not exist.");
			}

			var query = Query.EQ("JobID", jobID);
			return collection.Find(query).ToList();
		}

		private static MongoCollection<Job> GetJobsCollection(string connectionString)
		{
			string collectionname = MongoNames.Jobs;
			MongoServer server = MongoServer.Create(connectionString);
			MongoDatabase db = server.GetDatabase(MongoNames.Database);
			
			MongoCollection<Job> collection;

			if (db.CollectionExists(collectionname))
			{
				collection = db.GetCollection<Job>(collectionname);
			}
			else
			{
				db.CreateCollection(collectionname);
				collection = db.GetCollection<Job>(collectionname);
				collection.CreateIndex(new string[] { "JobID"});
			}

			if (db.CollectionExists(collectionname) == false)
			{
				throw new ApplicationException("Database does not exist.");
			}

			return collection;
		}
	}

	public static class MongoNames
	{
		public const string Database = "PeachFarm";
		public const string Jobs = "jobs";
		public const string Iterations = "job.iterations";
		public const string PeachFarmErrors = "peachFarm.errors";

	}
}
