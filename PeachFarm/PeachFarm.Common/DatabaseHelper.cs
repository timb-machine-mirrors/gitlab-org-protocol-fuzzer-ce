﻿using System;
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
		public static string[] FaultInfoFields = new string[]
				{
					"Faults.Description",
					"Faults.DetectionSource",
					"Faults.Exploitability",
					"Faults.FolderName",
					"Faults.MajorHash",
					"Faults.MinorHash",
					"Faults.Title",
					"Faults.FaultType",
					"Faults.StateModel",
					"Faults.CollectedData"
				};

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
			CreateJobNodesCollection(server);
			CreateNodesCollection(server);
			CreateFaultsCollection(server);
			CreateErrorsCollection(server);
		}

		public static void CreateCollection(MongoServer server, string collectionName)
		{
			switch (collectionName)
			{
				case MongoNames.Faults:
					CreateFaultsCollection(server);
					break;
				case MongoNames.JobNodes:
					CreateJobNodesCollection(server);
					break;
				case MongoNames.Jobs:
					CreateJobsCollection(server);
					break;
				case MongoNames.PeachFarmErrors:
					CreateErrorsCollection(server);
					break;
				case MongoNames.PeachFarmNodes:
					CreateNodesCollection(server);
					break;
				default:
					throw new ApplicationException("Collection name unknown: " + collectionName);
			}
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

		public static void CreateJobNodesCollection(MongoServer server)
		{
			string collectionname = MongoNames.JobNodes;
			MongoDatabase db = server.GetDatabase(MongoNames.Database);

			MongoCollection<Job> collection;
			if (!db.CollectionExists(collectionname))
			{
				db.CreateCollection(collectionname);
				collection = db.GetCollection<Job>(collectionname);
				collection.CreateIndex(new string[] { "JobID", "Name" });
			}

		}

		public static void CreateFaultsCollection(MongoServer server)
		{
			string collectionname = MongoNames.Faults;
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


		public static void CreateNodesCollection(MongoServer server)
		{
			string collectionname = MongoNames.PeachFarmNodes;
			MongoDatabase db = server.GetDatabase(MongoNames.Database);

			MongoCollection<Job> collection;
			if (!db.CollectionExists(collectionname))
			{
				db.CreateCollection(collectionname);
				collection = db.GetCollection<Job>(collectionname);
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
			MongoCollection<Job> collection = GetCollection<Job>(MongoNames.Jobs, connectionString);
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

			if (server.DatabaseExists(MongoNames.Database))
			{
				MongoDatabase db = server.GetDatabase(MongoNames.Database);
				MongoCollection<T> collection = null;

				if (db.CollectionExists(collectionname) == false)
				{
					CreateCollection(server, collectionname);
				}

				collection = db.GetCollection<T>(collectionname);

				return collection;
			}
			else
			{
				throw new ApplicationException("Database does not exist in MongoDB: " + MongoNames.Database);
			}
		}

		public static List<Heartbeat> GetAllErrors(string connectionString)
		{
			MongoCollection<Messages.Heartbeat> collection = GetCollection<Messages.Heartbeat>(MongoNames.PeachFarmErrors, connectionString);
			return collection.FindAll().ToList();
		}

		public static List<Heartbeat> GetAllNodes(string connectionString)
		{
			MongoCollection<Messages.Heartbeat> collection = GetCollection<Messages.Heartbeat>(MongoNames.PeachFarmNodes, connectionString);
			var allnodes = collection.FindAll().ToList();
			foreach (var node in allnodes)
			{
				node.Stamp = node.Stamp.ToLocalTime();
			}
			collection.Database.Server.Disconnect();
			return allnodes;
		}

		public static Heartbeat GetNodeByName(string name, string connectionString)
		{
			MongoCollection<Messages.Heartbeat> collection = GetCollection<Messages.Heartbeat>(MongoNames.PeachFarmNodes, connectionString);
			var query = Query.EQ("NodeName", name);
			return collection.FindOne(query);
		}

		public static Node GetJobNode(string nodeName, string jobID, string connectionString)
		{
			MongoCollection<Node> collection = GetCollection<Node>(MongoNames.JobNodes, connectionString);
			var query = Query.And(Query.EQ("Name", nodeName), Query.EQ("JobID", jobID));
			return collection.FindOne(query);
		}

		public static List<Fault> GetJobFaults(string jobID, string connectionString)
		{
			MongoCollection<Fault> collection = GetCollection<Fault>(MongoNames.Faults, connectionString);
			var query = Query.EQ("JobID", jobID);
			return collection.Find(query).ToList();
		}
	}

	public static class MongoNames
	{
		public const string Database = "PeachFarm";
		public const string Jobs = "jobs";
		public const string JobNodes = "job.nodes";
		public const string Faults = "node.faults";
		public const string PeachFarmErrors = "peachFarm.errors";
		public const string PeachFarmNodes = "peachFarm.nodes";
	}
}
