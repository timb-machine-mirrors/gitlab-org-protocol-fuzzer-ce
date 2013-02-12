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
				MongoServer server = new MongoClient(connectionString).GetServer();
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
			CreateErrorsCollection(server);
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

		public static Job GetJob(string jobGuid, string connectionString)
		{
			MongoCollection<Job> collection = GetCollection<Job>(MongoNames.Jobs, connectionString);
			var query = Query.EQ("JobID", jobGuid);
			return collection.FindOne(query);
		}

		public static List<Job> GetAllJobs(string connectionString)
		{
			MongoCollection<Job> collection = GetCollection<Job>(MongoNames.Jobs,connectionString);
			return collection.FindAll().ToList();
		}

		public static List<Messages.Heartbeat> GetErrors(string connectionString)
		{
			MongoCollection<Messages.Heartbeat> collection = GetCollection<Messages.Heartbeat>(MongoNames.PeachFarmErrors, connectionString);
			return collection.FindAll().ToList();
		}

		public static List<Messages.Heartbeat> GetErrors(string jobID, string connectionString)
		{
			MongoCollection<Messages.Heartbeat> collection = GetCollection<Messages.Heartbeat>(MongoNames.PeachFarmErrors, connectionString);
			var query = Query.EQ("JobID", jobID);
			return collection.Find(query).ToList();
		}

		public static MongoCollection<T> GetCollection<T>(string collectionname, string connectionString)
		{
			MongoServer server = new MongoClient(connectionString).GetServer();
			MongoDatabase db = server.GetDatabase(MongoNames.Database);
			
			MongoCollection<T> collection = null;

			if (db.CollectionExists(collectionname))
			{
				collection = db.GetCollection<T>(collectionname);
			}
			else
			{
				throw new ApplicationException(String.Format("Collection {0} does not exist", collectionname));
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
