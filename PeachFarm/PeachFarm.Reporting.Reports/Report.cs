using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web;
using MongoDB.Driver;
using PeachFarm.Common.Mongo;
using PeachFarm.Reporting;
using MongoDB.Driver.Builders;

namespace PeachFarm.Reporting.Reports
{
	public class ReportData
	{
		public static List<ReportJob> GetJobDetailReport(string jobID, string connectionString)
		{
			Job mongojob = DatabaseHelper.GetJob(jobID, connectionString);

			var collection = DatabaseHelper.GetCollection<Fault>(MongoNames.Faults, connectionString);

			var buckets = collection.Distinct("FolderName", Query.EQ("JobID", jobID));
			List<FaultBucket> faultBuckets = new List<FaultBucket>();

			foreach (var bucket in buckets)
			{
				var faultquery = Query.And(Query.EQ("JobID", jobID), Query.EQ("FolderName", bucket));
				Fault faultBucket = collection.FindOne(faultquery);
				FaultBucket fbvm = new FaultBucket(faultBucket)
					{
						FaultCount =
							collection.Distinct("_id", faultquery)
							          .Count()
					};
				faultBuckets.Add(fbvm);
			}
			collection.Database.Server.Disconnect();

			ReportJob job = new ReportJob()
				{
					JobID = mongojob.JobID,
					Pit = mongojob.Pit.FileName,
					StartDate = mongojob.StartDate,
					UserName = mongojob.UserName,
					Faults = faultBuckets
				};

			return new List<ReportJob>() { job };
		}

		internal static System.Drawing.Bitmap GetEmbeddedImage(string p)
		{
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			var file = assembly.GetManifestResourceStream("PeachFarm.Reporting.Reports." + p);
			if (file == null)
			{
				return null;
			}
			else
			{
				return new System.Drawing.Bitmap(file);
			}
		}
	}

	public class ReportJob
	{
		public string JobID { get; set; }
		public DateTime StartDate { get; set; }
		public string UserName { get; set; }
		public string Pit { get; set; }
		public List<FaultBucket> Faults { get; set; }
	}

	public class FaultBucket : Fault
	{
		public FaultBucket(Fault fault)
		{
			this.ControlIteration = fault.ControlIteration;
			this.ControlRecordingIteration = fault.ControlRecordingIteration;
			this.DetectionSource = fault.DetectionSource;
			this.Exploitability = fault.Exploitability;
			this.FaultType = fault.FaultType;
			this.FolderName = fault.FolderName;
			this.IsReproduction = fault.IsReproduction;
			this.Iteration = fault.Iteration;
			this.JobID = fault.JobID;
			this.MajorHash = fault.MajorHash;
			this.MinorHash = fault.MinorHash;
			this.NodeName = fault.NodeName;
			this.SeedNumber = fault.SeedNumber;
			this.Stamp = fault.Stamp;
			this.TestName = fault.TestName;
			this.Title = fault.Title;
			this.Description = fault.Description;
			this.GeneratedFiles = fault.GeneratedFiles;
		}

		public int FaultCount { get; set; }
	}
}