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
		public static Dictionary<string, Heartbeat> ToDictionary(this IEnumerable<Heartbeat> nodes)
		{
			Dictionary<string, Heartbeat> dictionary = new Dictionary<string, Heartbeat>();
			foreach (var node in nodes)
			{
				dictionary.Add(node.NodeName, node);
			}
			return dictionary;
		}
	}

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

	public partial class StartPeachResponse
	{
		public StartPeachResponse() { }

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
		public StopPeachResponse() { }

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
		public Job(Mongo.Job mongoJob)
		{
			this.JobID = mongoJob.JobID;
			this.Pit = new Pit(mongoJob.Pit);
			this.StartDate = mongoJob.StartDate;
			this.UserName = mongoJob.UserName;
			this.Tags = mongoJob.Tags;
		}

		[XmlIgnore]
		public string JobFolder
		{
			get
			{
				return String.Format(Formats.JobFolder, this.JobID, this.Pit.FileName);
			}
		}
	}

	public partial class Pit
	{
		public Pit() { }

		public Pit(Mongo.Pit mongoPit)
		{
			this.FileName = mongoPit.FileName;
			this.FullText = mongoPit.FullText;
			this.Version = mongoPit.Version;
		}
	}

	public static class Actions
	{
		public const string StartPeach = "StartPeach";
		public const string StopPeach = "StopPeach";
		public const string JobInfo = "JobInfo";
		public const string ListNodes = "ListNodes";
		public const string ListErrors = "ListErrors";
		public const string Monitor = "Monitor";
		public const string Heartbeat = "Heartbeat";
		public const string GenerateReport = "GenerateReport";
	}
}
