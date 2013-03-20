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
using System.IO;

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

			iteration = UpdateDataPaths(iteration, connectionString);

			collection.Insert(iteration);

			collection.Database.Server.Disconnect();

			Debug.WriteLine("******* WRITING ITERATION TO DATABASE ******");

			return iteration;
		}

		private static Iteration UpdateDataPaths(Iteration iteration, string connectionString)
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

			Job job = DatabaseHelper.GetJob(iteration.JobID, connectionString);

			jobFolder = String.Format("Job_{0}_{1}", job.JobID, job.PitFileName);
			nodeFolder = Path.Combine(jobFolder, "Node_" + iteration.NodeName);
			testFolder = Path.Combine(nodeFolder, String.Format("{0}_{1}_{2}", job.PitFileName, iteration.TestName, FormatDate(job.StartDate)));
			faultsFolder = Path.Combine(testFolder, "Faults");

			foreach (Fault fault in iteration.Faults)
			{
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

				iterationFolder = Path.Combine(faultFolder, iteration.IterationNumber.ToString());

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

					action.DataPath = actionFile;
				}
				#endregion


				#region write collected data files
				foreach (CollectedData cd in fault.CollectedData)
				{
					collectedDataFile = System.IO.Path.Combine(iterationFolder,
						fault.DetectionSource + "_" + cd.Key);

					cd.DataPath = collectedDataFile;
				}
				#endregion
			}

			return iteration;
		}

		private static string FormatDate(DateTime dateTime)
		{
			return String.Format("{0:yyyyMMddhhmmss}", dateTime);
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

		public static long GetFaultCount(this Job job, string connectionString)
		{
			MongoCollection<Iteration> collection = DatabaseHelper.GetCollection<Iteration>(MongoNames.Iterations, connectionString);
			var query = Query.EQ("JobID", job.JobID);
			return collection.Count(query);
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
					return _id.ToString();
			}
		}

		[XmlIgnore]
		[BsonIgnore]
		public List<Iteration> Iterations { get; set; }
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
					return _id.ToString();
			}
		}
	}

	public class JobComparer : EqualityComparer<Mongo.Job>
	{
		public override bool Equals(Common.Mongo.Job x, Common.Mongo.Job y)
		{
			return x.JobID == y.JobID;
		}

		public override int GetHashCode(Mongo.Job obj)
		{
			return obj.JobID.GetHashCode();
		}
	}

}
