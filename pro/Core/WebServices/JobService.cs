using System;
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

			Get["/{id}/result"] = _ => GetTestResult(_.id);
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

			var job = JobMonitor.Instance.Start(PitLibraryPath, pitFile, jobRequest);
			if (job == null)
				return HttpStatusCode.Forbidden;

			return Response.AsJson(LoadJob(JobMonitor.Instance.Job));
		}

		Response DeleteJob(Guid id)
		{
			var liveJob = JobMonitor.Instance.Job;
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
			return WithJobMonitor(id, r => r.Pause() ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		Response ContinueJob(Guid id)
		{
			return WithJobMonitor(id, r => r.Continue() ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		Response StopJob(Guid id)
		{
			return WithJobMonitor(id, r => r.Stop() ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		Response KillJob(Guid id)
		{
			return WithJobMonitor(id, r => r.Kill() ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		Response GetNodes(Guid id)
		{
			return WithJobMonitor(id, x => Response.AsJson(new[] { NodeService.Prefix + "/" + NodeGuid }));
		}

		Response GetJobs()
		{
			using (var db = new NodeDatabase())
			{
				var jobs = db.LoadTable<Job>().ToList();

				var liveJob = JobMonitor.Instance.Job;
				if (liveJob != null)
					jobs.Insert(0, liveJob);

				return Response.AsJson(jobs.Select(LoadJob));
			}
		}

		Response GetJob(Guid id)
		{
			// is it currently live?
			var liveJob = JobMonitor.Instance.Job;
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

		Response GetTestResult(Guid id)
		{
			var logPath = JobDatabase.GetStorageDirectory(id);
			var debugLog = Path.Combine(logPath, "debug.log");

			using (var db = new JobDatabase(id))
			{
				var events = db.LoadTable<TestEvent>().ToList();
				var isActive = events.Any(x => x.Status == TestStatus.Active);
				var isFail = events.Any(x => x.Status == TestStatus.Fail);

				var result = new TestResult
				{
					Status = isActive ? TestStatus.Active : 
						isFail ? TestStatus.Fail : TestStatus.Pass,
					Events = events,
				};

				if (File.Exists(debugLog))
				{
					using (var file = new FileStream(
						debugLog, 
						FileMode.Open, 
						FileAccess.Read, 
						FileShare.ReadWrite, 
						64 * 1024,
						FileOptions.SequentialScan))
					using (var reader = new StreamReader(file))
					{
						result.Log = reader.ReadToEnd();
					}
				}

				return Response.AsJson(result);
			}
		}

		Response GetFaults(Guid id)
		{
			var job = JobMonitor.Instance.Job;
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
				return Response.AsJson(faults.Select(x => LoadFaultSummary(job, x)));
			}
		}

		Response GetFault(Guid id, long faultId)
		{
			using (var db = new JobDatabase(id))
			{
				var job = db.GetJob(id);
				if (job == null)
					return HttpStatusCode.NotFound;

				var fault = db.GetFaultById(faultId);
				if (fault == null)
					return HttpStatusCode.NotFound;

				return Response.AsJson(LoadFault(job, fault));
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

			var filename = "Fault-{0}.zip".Fmt(fault.Iteration);

			var dir = new DirectoryInfo(Path.Combine(
				JobDatabase.GetStorageDirectory(id),
				fault.Iteration.ToString("X8")
			));

			return Response.AsZip(filename, dir);
		}

		[Obsolete]
		Response GetVisualizer(Guid id)
		{
			return HttpStatusCode.NotImplemented;
		}

		Response WithJobMonitor(Guid id, Func<JobMonitor, Response> fn)
		{
			var job = JobMonitor.Instance.Job;
			if (job == null || job.Guid != id)
				return HttpStatusCode.NotFound;
			return fn(JobMonitor.Instance);
		}

		Job LoadJob(Job job)
		{
			var id = job.Id;

			TimeSpan elapsed;
			if (job.StopDate.HasValue)
				elapsed = job.StopDate.Value - job.StartDate;
			else
				elapsed = DateTime.UtcNow - job.StartDate;
			job.Runtime = (long)elapsed.TotalSeconds;

			job.Speed = (long)(
				job.IterationCount / elapsed.TotalSeconds * 3600.0
			);

			job.Links = new JobLinks
			{
				JobUrl = MakeUrl(id),
				FaultsUrl = MakeUrl(id, "faults"),
				NodesUrl = MakeUrl(id, "nodes"),
				//TargetUrl = "",
				//TargetConfigUrl = "",
				//PeachUrl = "",
				//ReportUrl = "",
				//PackageFileUrl = "",
				Commands = new JobCommands
				{
					StopUrl = MakeUrl(id, "stop"),
					ContinueUrl = MakeUrl(id, "continue"),
					PauseUrl = MakeUrl(id, "pause"),
					KillUrl = MakeUrl(id, "kill"),
				},
				Metrics = new JobMetrics
				{
					BucketTimeline = MakeUrl(id, "metrics", "bucketTimeline"),
					FaultTimeline = MakeUrl(id, "metrics", "faultTimeline"),
					Mutators = MakeUrl(id, "metrics", "mutators"),
					Elements = MakeUrl(id, "metrics", "elements"),
					States = MakeUrl(id, "metrics", "states"),
					Dataset = MakeUrl(id, "metrics", "dataset"),
					Buckets = MakeUrl(id, "metrics", "buckets"),
					Iterations = MakeUrl(id, "metrics", "iterations"),
				},
			};

			if (job.IsTest)
				job.Links.TestUrl = MakeUrl(id, "result");

			return job;
		}

		FaultSummary LoadFaultSummary(Job job, FaultSummary fault)
		{
			fault.FaultUrl = MakeUrl(
				job.Guid.ToString(), "faults", fault.Id.ToString());
			return fault;
		}

		FaultDetail LoadFault(Job job, FaultDetail fault)
		{
			LoadFaultSummary(job, fault);
			fault.Links = new FaultLinks
			{
				PitUrl = job.PitUrl,
				NodeUrl = NodeService.MakeUrl(NodeGuid),
				ArchiveUrl = MakeUrl(job.Id, "faults", fault.Id.ToString(), "archive"),
				//PeachUrl = "",
				//TargetConfigUrl = "",
				//TargetUrl = "",
			};
			foreach (var file in fault.Files)
			{
				LoadFile(job, file);
			}
			return fault;
		}

		void LoadFile(Job job, FaultFile file)
		{
			file.FileUrl = MakeUrl(
				job.Guid.ToString(), 
				"faults", 
				file.FaultDetailId.ToString(),
				"data",
				file.Id.ToString()
			);
		}

		static string MakeUrl(params string[] args)
		{
			return string.Join("/", Prefix, string.Join("/", args));
		}
	}
}
