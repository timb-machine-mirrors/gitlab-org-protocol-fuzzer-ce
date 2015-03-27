using System;
using Ionic.Zip;
using Nancy;
using Nancy.ModelBinding;
using Peach.Core;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Core.Storage;
using System.Linq;
using System.IO;
using Peach.Pro.Core.WebServices.Utility;

namespace Peach.Pro.Core.WebServices
{
	public class JobService : WebService
	{
		public static readonly string Prefix = "/p/jobs";

		public JobService(WebContext context)
			: base(context, Prefix)
		{
			Post[""] = _ => CreateJob();
			Get[""] = _ => GetJobs();

			Get["/{id}"] = _ => GetJob(_.id);
			Delete["/{id}"] = _ => DeleteJob(_.id);

			Get["/{id}/nodes"] = _ => GetNodes(_.id);

			Get["/{id}/faults"] = _ => GetFaults(_.id);
			Get["/{id}/faults/{fid}"] = _ => GetFault(_.id, _.fid);
			Get["/{id}/faults/{fid}/data/{did}"] = _ => GetFaultData(_.id, _.did);
			Get["/{id}/faults/{fid}/archive"] = _ => GetFaultArchive(_.id, _.fid);

			// deprecated
			Get["/{id}/visualizer"] = _ => GetVisualizer(_.id);

			Get["/{id}/continue"] = _ => ContinueJob(_.id);
			Get["/{id}/pause"] = _ => PauseJob(_.id);
			Get["/{id}/stop"] = _ => StopJob(_.id);
			Get["/{id}/kill"] = _ => KillJob(_.id);
		}

		Response CreateJob()
		{
			var jobRequest = this.Bind<JobRequest>();
			if (string.IsNullOrEmpty(jobRequest.PitUrl))
				return HttpStatusCode.BadRequest;

			var pit = PitDatabase.GetPitByUrl(jobRequest.PitUrl);
			if (pit == null)
				return HttpStatusCode.NotFound;

			var pitFile = pit.Versions[0].Files[0].Name;

			var job = JobRunner.Instance.Start(PitLibraryPath, pitFile, jobRequest);
			if (job == null)
				return HttpStatusCode.Forbidden;

			return Response.AsJson(LoadJob(JobRunner.Instance.Job));
		}

		Response DeleteJob(Guid id)
		{
			var liveJob = JobRunner.Instance.Job;
			if (liveJob != null && liveJob.Guid == id)
				return HttpStatusCode.Forbidden;

			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(id);
				if (job == null)
					return HttpStatusCode.NotFound;

				db.DeleteJob(id);
				return HttpStatusCode.OK;
			}
		}

		Response PauseJob(Guid id)
		{
			return QueryJob(id, r => r.Pause() ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		Response ContinueJob(Guid id)
		{
			return QueryJob(id, r => r.Continue() ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		Response StopJob(Guid id)
		{
			return QueryJob(id, r => r.Stop() ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		Response KillJob(Guid id)
		{
			return QueryJob(id, r => r.Kill() ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		Response GetNodes(Guid id)
		{
			return QueryJob(id, x => Response.AsJson(new[] { NodeService.Prefix + "/" + NodeGuid }));
		}

		Response GetJobs()
		{
			using (var db = new NodeDatabase())
			{
				var jobs = db.LoadTable<Job>().ToList();

				var liveJob = JobRunner.Instance.Job;
				if (liveJob != null)
					jobs.Insert(0, liveJob);

				return Response.AsJson(jobs.Select(LoadJob));
			}
		}

		Response GetJob(Guid id)
		{
			// is it currently live?
			var liveJob = JobRunner.Instance.Job;
			if (liveJob != null && liveJob.Guid == id)
				return Response.AsJson(LoadJob(liveJob));

			// otherwise grab historical data
			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(id);
				if (job == null)
					return HttpStatusCode.NotFound;
				return Response.AsJson(LoadJob(job));
			}
		}

		Response GetFaults(Guid id)
		{
			var job = JobRunner.Instance.Job;
			if (job == null || job.Guid != id)
			{
				using (var db = new NodeDatabase())
				{
					job = db.GetJob(id);
					if (job == null)
						return HttpStatusCode.NotFound;
				}
			}

			using (var db = new JobDatabase(id))
			{
				// ReSharper disable once RedundantEnumerableCastCall
				var faults = db.LoadTable<FaultDetail>()
					.OfType<FaultSummary>();
				return Response.AsJson(faults.Select(LoadFaultSummary));
			}
		}

		Response GetFault(Guid id, long faultId)
		{
			using (var db = new JobDatabase(id))
			{
				var fault = db.GetFaultById(faultId);
				if (fault == null)
					return HttpStatusCode.NotFound;
				return Response.AsJson(LoadFault(fault));
			}
		}

		Response GetFaultData(Guid id, long fileId)
		{
			FaultFile file;
			using (var db = new JobDatabase(id))
			{
				file = db.GetFaultFileById(fileId);
				if (file == null)
					return HttpStatusCode.NotFound;
			}

			var dir = JobDatabase.GetStorageDirectory(id);
			var path = Path.Combine(dir, file.FullName);
			return Response.AsFile(new System.IO.FileInfo(path));
		}

		Response GetFaultArchive(Guid id, long faultId)
		{
			FaultDetail fault;
			using (var db = new JobDatabase(id))
			{
				fault = db.GetFaultById(faultId, false);
			}
	
			var dir = fault.Iteration.ToString("X8");
			var dirInArchive = "Fault-{0}".Fmt(fault.Iteration);
			var filename = "{0}.zip".Fmt(dirInArchive);

			return Response.AsZip(filename, () =>
			{
				var zip = new ZipFile();
				zip.AddDirectory(dir, dirInArchive);
				return zip;
			});
		}

		[Obsolete]
		Response GetVisualizer(Guid id)
		{
			return HttpStatusCode.NotImplemented;
		}

		Response QueryJob(Guid id, Func<JobRunner, Response> query)
		{
			var job = JobRunner.Instance.Job;
			if (job == null || job.Guid != id)
				return HttpStatusCode.NotFound;
			return query(JobRunner.Instance);
		}

		Job LoadJob(Job job)
		{
			var id = job.Id.ToString();

			job.JobUrl = MakeUrl(id);
			job.FaultsUrl = MakeUrl(id, "faults");
			job.TargetUrl = "";
			job.TargetConfigUrl = "";
			job.NodesUrl = MakeUrl(id, "nodes");
			job.PeachUrl = "";
			job.ReportUrl = "";
			job.PackageFileUrl = "";

			job.Commands = new JobCommands
			{
				StopUrl = MakeUrl(id, "stop"),
				ContinueUrl = MakeUrl(id, "continue"),
				PauseUrl = MakeUrl(id, "pause"),
				KillUrl = MakeUrl(id, "kill"),
			};

			job.Metrics = new JobMetrics
			{
				BucketTimeline = MakeUrl(id, "metrics", "bucketTimeline"),
				FaultTimeline = MakeUrl(id, "metrics", "faultTimeline"),
				Mutators = MakeUrl(id, "metrics", "mutators"),
				Elements = MakeUrl(id, "metrics", "elements"),
				States = MakeUrl(id, "metrics", "states"),
				Dataset = MakeUrl(id, "metrics", "dataset"),
				Buckets = MakeUrl(id, "metrics", "buckets"),
				Iterations = MakeUrl(id, "metrics", "iterations"),
			};

			return job;
		}

		FaultSummary LoadFaultSummary(FaultSummary fault)
		{
			fault.FaultUrl = "";
			return fault;
		}

		FaultDetail LoadFault(FaultDetail fault)
		{
			LoadFaultSummary(fault);
			fault.NodeUrl = "";
			fault.PeachUrl = "";
			fault.PitUrl = "";
			fault.TargetConfigUrl = "";
			fault.TargetUrl = "";
			foreach (var file in fault.Files)
			{
				LoadFile(file);
			}
			return fault;
		}

		FaultFile LoadFile(FaultFile file)
		{
			file.FileUrl = "";
			return file;
		}

		static string MakeUrl(params string[] args)
		{
			return string.Join("/", Prefix, string.Join("/", args));
		}
	}
}
