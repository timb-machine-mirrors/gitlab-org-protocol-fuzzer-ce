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
			foreach (var kvp in collections)
			{
				MongoDatabase db = server.GetDatabase(MongoNames.Database);

				MongoCollection<Job> collection;
				if (!db.CollectionExists(kvp.Key))
				{
					db.CreateCollection(kvp.Key);
					collection = db.GetCollection<Job>(kvp.Key);
					collection.CreateIndex(kvp.Value.ToArray());
				}
			}
		}

		private static Dictionary<string, string[]> collections = new Dictionary<string, string[]>()
		{
			{MongoNames.Faults, new string[3]{"JobID", "NodeName", "Group"}},
			{MongoNames.JobNodes, new string[2]{"JobID", "Name"}},
			{MongoNames.Jobs, new string[1]{"JobID"}},
			{MongoNames.PeachFarmErrors, new string[2]{"JobID","NodeName"}},
			{MongoNames.PeachFarmNodes, new string[1]{"NodeName"}}
		};

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

		public static void ClearJobsWithZeroIterations(string connectionString)
		{
			var jobs = DatabaseHelper.GetAllJobs(connectionString);
			List<Job> jobsToDelete = new List<Job>();
			foreach (var job in jobs)
			{
				job.FillNodes(connectionString);
				var iterationcount = (from n in job.Nodes select Convert.ToDecimal(n.IterationCount)).Sum();
				if (iterationcount == 0)
				{
					jobsToDelete.Add(job);
				}
			}

			MongoCollection<Job> jobsCollection = GetCollection<Job>(MongoNames.Jobs, connectionString);
			MongoCollection<Node> nodesCollection = GetCollection<Node>(MongoNames.JobNodes, connectionString);

			foreach (var job in jobsToDelete)
			{
				var jobsQuery = Query.EQ("_id", job._id);
				jobsCollection.Remove(jobsQuery);

				var nodesQuery = Query.EQ("JobID", job.JobID);
				nodesCollection.Remove(nodesQuery);
			}
			jobsCollection.Database.Server.Disconnect();
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
					CreateCollections(server);
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
			var results = collection.FindAll().SetSortOrder(SortBy.Descending("Stamp")).ToList();
			foreach (var result in results)
			{
				result.Stamp = result.Stamp.ToLocalTime();
			}
			return results;
		}

		public static List<Heartbeat> GetAllNodes(string connectionString)
		{
			MongoCollection<Messages.Heartbeat> collection = GetCollection<Messages.Heartbeat>(MongoNames.PeachFarmNodes, connectionString);
			var allnodes = collection.FindAll().SetFlags(QueryFlags.NoCursorTimeout).ToList();
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

		public static IQueryable<Fault> GetJobFaultsAsQueryable(string jobID, string connectionString)
		{
			MongoCollection<Fault> collection = GetCollection<Fault>(MongoNames.Faults, connectionString);
			var query = Query.EQ("JobID", jobID);
			return collection.Find(query).AsQueryable();
		}

		public static void SaveToGridFS(byte[] p, string remoteFileName, string connectionString)
		{
			remoteFileName = remoteFileName.Replace('\\', '/');
			MongoServer server = new MongoClient(connectionString).GetServer();
			MongoDatabase db = server.GetDatabase(MongoNames.Database);
			using (var stream = new MemoryStream(p))
			{
				var gridFsInfo = db.GridFS.Upload(stream, remoteFileName);
				System.Diagnostics.Debug.Assert(gridFsInfo != null);
			}
			server.Disconnect();
		}

		#region unused
		/*
		public static void SaveToGridFS(string s, string remoteFileName, string connectionString)
		{
			byte[] p = encoding.GetBytes(s);
			SaveToGridFS(p, remoteFileName, connectionString);
		}

		public static Stream GetGridFSStream(string remoteFileName, string connectionString)
		{
			MongoServer server = new MongoClient(connectionString).GetServer();
			MongoDatabase db = server.GetDatabase(MongoNames.Database);

			return db.GridFS.Create(remoteFileName);
		}
		//*/
		#endregion

		public static string SaveFileToGridFS(string localFileName, string remoteFileName, string connectionString)
		{
			remoteFileName = remoteFileName.Replace('\\', '/');
			MongoServer server = new MongoClient(connectionString).GetServer();
			MongoDatabase db = server.GetDatabase(MongoNames.Database);
			var gridFsInfo = db.GridFS.Upload(localFileName, remoteFileName);
			server.Disconnect();
			return gridFsInfo.Id.ToString();
		}

		public static StreamWriter CreateFileGridFS(string remoteFileName, string connectionString)
		{
			remoteFileName = remoteFileName.Replace('\\', '/');
			MongoServer server = new MongoClient(connectionString).GetServer();
			MongoDatabase db = server.GetDatabase(MongoNames.Database);
			
			return db.GridFS.CreateText(remoteFileName);

		}

		public static void DownloadFromGridFS(string localFile, string remoteFile, string connectionString)
		{
			MongoServer server = new MongoClient(connectionString).GetServer();
			MongoDatabase db = server.GetDatabase(MongoNames.Database);

			MongoDB.Bson.BsonObjectId remoteFileID = MongoDB.Bson.BsonObjectId.Empty;
			bool isid = MongoDB.Bson.BsonObjectId.TryParse(remoteFile, out remoteFileID);

			if (isid)
			{
				if (db.GridFS.ExistsById(remoteFileID))
				{
					var fsinfo = db.GridFS.FindOneById(remoteFileID);
					db.GridFS.Download(localFile, fsinfo.Name);
				}
			}
			else
			{
				remoteFile = remoteFile.Replace('\\', '/');
				if (db.GridFS.Exists(remoteFile))
				{
					db.GridFS.Download(localFile, remoteFile);
				}
			}

			server.Disconnect();
			return;
		}

		public static string ReadFromGridFS(string remoteFile, string connectionString)
		{
			MongoServer server = new MongoClient(connectionString).GetServer();
			MongoDatabase db = server.GetDatabase(MongoNames.Database);

			if (db.GridFS.Exists(remoteFile))
			{
				using (StreamReader reader = db.GridFS.OpenText(remoteFile))
				{
					return reader.ReadToEnd();
				}
			}
			else
			{
				throw new Exception("File not found: " + remoteFile);
			}

			//server.Disconnect();
		}

		public static bool GridFSFileExists(string remoteFile, string connectionString)
		{
			remoteFile = remoteFile.Replace('\\', '/');
			MongoServer server = new MongoClient(connectionString).GetServer();
			MongoDatabase db = server.GetDatabase(MongoNames.Database);
			bool result = db.GridFS.Exists(remoteFile);
			server.Disconnect();
			return result;
		}

		public static string GetJobID(string connectionString)
		{
			string jobID = CreateJobID();
			while (Common.Mongo.DatabaseHelper.GetJob(jobID, connectionString) != null)
			{
				jobID = CreateJobID();
			}
			return jobID;
		}

		private static string CreateJobID()
		{
			using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
			{
				// change the size of the array depending on your requirements
				var rndBytes = new byte[6];
				rng.GetBytes(rndBytes);
				return BitConverter.ToString(rndBytes).Replace("-", "");
			}
		}

		public static void DeleteGridFSFile(string remoteFile, string connectionString)
		{
			MongoServer server = new MongoClient(connectionString).GetServer();
			MongoDatabase db = server.GetDatabase(MongoNames.Database);
			if (db.GridFS.Exists(remoteFile))
			{
				db.GridFS.Delete(remoteFile);
			}
			server.Disconnect();
		}

#if DEBUG
		public static void TruncateAllCollections(string connectionString)
		{
			MongoServer server = new MongoClient(connectionString).GetServer();
			MongoDatabase db = server.GetDatabase(MongoNames.Database);

			var jobs = GetCollection<Job>(MongoNames.Jobs, connectionString);
			jobs.RemoveAll(WriteConcern.Acknowledged);

			var nodes = GetCollection<Heartbeat>(MongoNames.PeachFarmNodes, connectionString);
			nodes.RemoveAll(WriteConcern.Acknowledged);

			var errors = GetCollection<Heartbeat>(MongoNames.PeachFarmErrors, connectionString);
			errors.RemoveAll(WriteConcern.Acknowledged);

			var jobnodes = GetCollection<Node>(MongoNames.JobNodes, connectionString);
			jobnodes.RemoveAll(WriteConcern.Acknowledged);

			var faults = GetCollection<Fault>(MongoNames.Faults, connectionString);
			faults.RemoveAll(WriteConcern.Acknowledged);

			var files = db.GridFS.FindAll();
			foreach (var file in files)
			{
				file.Delete();
			}

			server.Disconnect();
		}

		public static void DeleteAllPDFs(string connectionString)
		{
			MongoServer server = new MongoClient(connectionString).GetServer();
			MongoDatabase db = server.GetDatabase(MongoNames.Database);
			var pdfs = db.GridFS.Find(Query.Matches("filename", ".pdf"));
			foreach (var mongoGridFsFileInfo in pdfs)
			{
				mongoGridFsFileInfo.Delete();
			}

			var jobs = DatabaseHelper.GetAllJobs(connectionString);
			foreach (var job in jobs)
			{
				job.ReportLocation = "";
				job.SaveToDatabase(connectionString);
			}

			server.Disconnect();
		}

#endif
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
