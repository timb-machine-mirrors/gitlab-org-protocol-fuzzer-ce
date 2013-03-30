using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Xml.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Builders;
using System.Security.Cryptography;

namespace PeachFarm.Common.Messages
{
	public static class ExtensionMethods
	{
		public static List<Mongo.Job> ToMongoJobs(this List<Messages.Job> jobs)
		{
			List<Mongo.Job> mongoJobs = new List<Mongo.Job>();
			foreach (Messages.Job job in jobs)
			{
				mongoJobs.Add(new Mongo.Job(job));
			}
			return mongoJobs;
		}

	}
	public partial class StartPeachResponse
	{

		public StartPeachResponse(StartPeachRequest request)
		{
			this.JobID = request.JobID;
			this.PitFileName = request.PitFileName;
			this.Success = true;
			this.UserName = request.UserName;
		}
	}

	public partial class StopPeachResponse
	{
		public StopPeachResponse(StopPeachRequest request)
		{
			this.JobID = request.JobID;
			this.PitFileName = request.PitFileName;
			this.Success = true;
			this.UserName = request.UserName;
		}
	}

	public partial class Job
	{
		public Job() { }

		public Job(Mongo.Job mongoJob)
		{
			this.JobID = mongoJob.JobID;
			this.PitFileName = mongoJob.PitFileName;
			this.StartDate = mongoJob.StartDate;
			this.UserName = mongoJob.UserName;
			this.FaultCount = (from i in mongoJob.Iterations select i.Faults.Count).Sum();
		}
	}
}
