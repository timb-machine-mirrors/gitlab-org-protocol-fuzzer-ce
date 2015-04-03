using System;
using System.Collections.Generic;
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
			Get["/{id}/faults/{fid}/data/{did}"] = _ => GetFaultFile(_.id, _.fid, _.did);
			Get["/{id}/faults/{fid}/archive"] = _ => GetFaultArchive(_.id, _.fid);

			// deprecated
			Get["/{id}/visualizer"] = _ => GetVisualizer(_.id);

			Get["/{id}/continue"] = _ => ContinueJob(_.id);
			Get["/{id}/pause"] = _ => PauseJob(_.id);
			Get["/{id}/stop"] = _ => StopJob(_.id);
			Get["/{id}/kill"] = _ => KillJob(_.id);

			Get["/{id}/result"] = _ => GetTestResult(_.id);

			// metrics
			Get["/{id}/metrics/faultTimeline"] = _ => Query<FaultTimelineMetric>(_.id);
			Get["/{id}/metrics/bucketTimeline"] = _ => Query<BucketTimelineMetric>(_.id);
			Get["/{id}/metrics/mutators"] = _ => Query<MutatorMetric>(_.id);
			Get["/{id}/metrics/elements"] = _ => Query<ElementMetric>(_.id);
			Get["/{id}/metrics/states"] = _ => Query<StateMetric>(_.id);
			Get["/{id}/metrics/dataset"] = _ => Query<DatasetMetric>(_.id);
			Get["/{id}/metrics/buckets"] = _ => Query<BucketMetric>(_.id);
			Get["/{id}/metrics/iterations"] = _ => Query<IterationMetric>(_.id);
		}

		Response Query<T>(Guid guid)
		{
			return WithJobDatabase(guid, db =>
			{
				if (!db.IsInitialized)
					return HttpStatusCode.NotFound;
				return Response.AsJson(db.LoadTable<T>());
			});
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

			var job = JobMonitor.Start(PitLibraryPath, pitFile, jobRequest);
			if (job == null)
				return HttpStatusCode.Forbidden;

			return Response.AsJson(LoadJob(JobMonitor.GetJob()));
		}

		Response DeleteJob(Guid id)
		{
			var liveJob = JobMonitor.GetJob();
			if (liveJob != null && liveJob.Guid == id)
				return HttpStatusCode.Forbidden;

			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(id);
				if (job == null)
					return HttpStatusCode.NotFound;

				if (Directory.Exists(job.LogPath))
					Directory.Delete(job.LogPath, true);

				var altPath = Path.Combine(Configuration.LogRoot, job.Id);
				if (Directory.Exists(altPath))
					Directory.Delete(altPath, true);

				db.DeleteJob(id);

				return HttpStatusCode.OK;
			}
		}

		Response PauseJob(Guid id)
		{
			return WithActiveJob(id, () => JobMonitor.Pause() ?
				HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		Response ContinueJob(Guid id)
		{
			return WithActiveJob(id, () => JobMonitor.Continue() ?
				HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		Response StopJob(Guid id)
		{
			return WithActiveJob(id, () => JobMonitor.Stop() ?
				HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		Response KillJob(Guid id)
		{
			return WithActiveJob(id, () => JobMonitor.Kill() ?
				HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		Response GetNodes(Guid id)
		{
			return WithActiveJob(id, () => Response.AsJson(new[]
			{
				string.Join("/", NodeService.Prefix, NodeGuid),
			}));
		}

		Response GetJobs()
		{
			using (var db = new NodeDatabase())
			{
				// in case a previous Peach crashed, 
				// reset all jobs status since we know 
				// we can only have a single liveJob running.
				// Also delete jobs when user manually deleted the job.LogPath

				var liveJob = JobMonitor.GetJob();

				var jobs = db.LoadTable<Job>()
					.Where(job =>
					{
						if (!File.Exists(job.DatabasePath))
						{
							DeleteJob(job.Guid);
							return false;
						}
						FixStaleJob(liveJob, job);
						return true;
					})
					.OrderByDescending(x => x.StartDate)
					.ToList();

				db.UpdateJobs(jobs);

				return Response.AsJson(jobs.Select(LoadJob));
			}
		}

		Response GetJob(Guid id)
		{
			var liveJob = JobMonitor.GetJob();

			Job job;

			using (var db = new NodeDatabase())
			{
				job = db.GetJob(id);
				if (FixStaleJob(liveJob, job))
					db.UpdateJob(job);
			}

			if (job != null && job.DatabasePath != null)
			{
				using (var db = new JobDatabase(job.DatabasePath))
				{
					job = db.GetJob(id);
					if (FixStaleJob(liveJob, job))
						db.UpdateJob(job);
				}
			}

			if (job == null)
				return HttpStatusCode.NotFound;

			return Response.AsJson(LoadJob(job));
		}

		Response GetTestResult(Guid id)
		{
			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(id);
				if (job == null)
					return HttpStatusCode.NotFound;

				var events = db.GetTestEventsByJob(id).ToList();
				var isActive = events.Any(x => x.Status == TestStatus.Active);
				var isFail = events.Any(x => x.Status == TestStatus.Fail);

				var preParsePath = Path.Combine(Configuration.LogRoot, job.Id, "debug.log");

				var result = new TestResult
				{
					Status = isActive
						? TestStatus.Active
						: isFail ? TestStatus.Fail : TestStatus.Pass,
					Events = events,
					Log = TryReadLog(preParsePath) ?? TryReadLog(job.DebugLogPath),
				};

				return Response.AsJson(result);
			}
		}

		Response GetFaults(Guid id)
		{
			return WithJobDatabase(id, db =>
			{
				var job = db.GetJob(id);
				if (job == null)
					return HttpStatusCode.NotFound;

				// ReSharper disable once RedundantEnumerableCastCall
				var faults = db.LoadTable<FaultDetail>()
					.OfType<FaultSummary>();
				return Response.AsJson(faults.Select(x => LoadFaultSummary(job, x)));
			});
		}

		Response GetFault(Guid id, long faultId)
		{
			return WithJobDatabase(id, db =>
			{
				var job = db.GetJob(id);
				if (job == null)
					return HttpStatusCode.NotFound;

				var fault = db.GetFaultById(faultId);
				if (fault == null)
					return HttpStatusCode.NotFound;

				return Response.AsJson(LoadFault(job, fault));
			});
		}

		Response GetFaultFile(Guid id, long faultId, long fileId)
		{
			return WithJobDatabase(id, db =>
			{
				var job = db.GetJob(id);
				if (job == null)
					return HttpStatusCode.NotFound;

				var fault = db.GetFaultById(faultId, false);
				if (fault == null)
					return HttpStatusCode.NotFound;

				var file = db.GetFaultFileById(fileId);
				if (file == null)
					return HttpStatusCode.NotFound;

				var path = Path.Combine(fault.FaultPath, file.FullName);
				return Response.AsFile(new System.IO.FileInfo(path));
			});
		}

		Response GetFaultArchive(Guid id, long faultId)
		{
			return WithJobDatabase(id, db =>
			{
				var job = db.GetJob(id);
				if (job == null)
					return HttpStatusCode.NotFound;

				var fault = db.GetFaultById(faultId, false);
				var filename = "Fault-{0}.zip".Fmt(fault.Iteration);
				var dir = new DirectoryInfo(fault.FaultPath);
				return Response.AsZip(filename, dir);
			});
		}

		[Obsolete]
		Response GetVisualizer(Guid id)
		{
			return HttpStatusCode.NotImplemented;
		}

		Response WithJobDatabase(Guid id, Func<JobDatabase, Response> fn)
		{
			Job job;

			using (var db = new NodeDatabase())
			{
				job = db.GetJob(id);
			}

			if (job == null || job.DatabasePath == null)
				return HttpStatusCode.NotFound;

			using (var db = new JobDatabase(job.DatabasePath))
			{
				return fn(db);
			}
		}

		Response WithActiveJob(Guid id, Func<Response> fn)
		{
			var job = JobMonitor.GetJob();
			if (job == null || job.Guid != id)
				return HttpStatusCode.NotFound;
			return fn();
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

			job.JobUrl = MakeUrl(id);
			job.FaultsUrl = MakeUrl(id, "faults");
			job.NodesUrl = MakeUrl(id, "nodes");
			//TargetUrl = "",
			//TargetConfigUrl = "",
			//PeachUrl = "",
			//ReportUrl = "",
			//PackageFileUrl = "",
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

			if (job.IsTest)
				job.TestUrl = MakeUrl(id, "result");

			return job;
		}

		FaultSummary LoadFaultSummary(Job job, FaultSummary fault)
		{
			fault.FaultUrl = MakeUrl(job.Id, "faults", fault.Id.ToString());
			fault.ArchiveUrl = MakeUrl(job.Id, "faults", fault.Id.ToString(), "archive");
			return fault;
		}

		FaultDetail LoadFault(Job job, FaultDetail fault)
		{
			LoadFaultSummary(job, fault);
			fault.PitUrl = job.PitUrl;
			fault.NodeUrl = NodeService.MakeUrl(NodeGuid);
			//PeachUrl = "",
			//TargetConfigUrl = "",
			//TargetUrl = "",
			fault.Files.ForEach(x => LoadFile(job, x));
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

		bool FixStaleJob(Job liveJob, Job job)
		{
			if (job != null && (liveJob == null || liveJob.Guid != job.Guid))
			{
				if (job.Status != JobStatus.Stopped)
				{
					job.Status = JobStatus.Stopped;
					job.StopDate = DateTime.UtcNow;
					return true;
				}
			}
			return false;
		}

		string TryReadLog(string path)
		{
			if (path == null)
				return null;

			try
			{
				using (var file = new FileStream(
					path,
					FileMode.Open,
					FileAccess.Read,
					FileShare.ReadWrite,
					64 * 1024,
					FileOptions.SequentialScan))
				using (var reader = new StreamReader(file))
				{
					return reader.ReadToEnd();
				}
			}
			catch (DirectoryNotFoundException)
			{
				return null;
			}
			catch (FileNotFoundException)
			{
				return null;
			}
		}
	}
}
