using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Http;
using System.Web.Http.Description;
using Peach.Core;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.WebApi2.Utility;
using Swashbuckle.Swagger.Annotations;
using FileInfo = System.IO.FileInfo;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.WebApi2.Controllers
{
	/// <summary>
	/// Job Functionality
	/// </summary>
	/// <remarks>
	/// Contains all functionality needed to control jobs
	/// </remarks>
	[RoutePrefix(Prefix)]
	public class JobsController : BaseController
	{
		public const string Prefix = "p/jobs";

		public static string MakeUrl(params string[] args)
		{
			return string.Join("/", "", Prefix, string.Join("/", args));
		}

		public JobsController()
			: base(null)
		{
		}

		#region Create / Read / Delete

		/// <summary>
		/// Gets the list of all jobs
		/// </summary>
		/// <example>
		/// GET /p/jobs
		/// </example>
		/// <remarks>
		/// Returns a list of all jobs in the database
		/// </remarks>
		/// <param name="dryrun">Include test runs</param>
		/// <param name="running">Include currently running jobs</param>
		/// <returns>List of jobs</returns>
		[Route("")]
		public IEnumerable<Job> Get([FromUri]bool? dryrun = null, [FromUri]bool? running = null)
		{
			using (var db = new NodeDatabase())
			{
				// in case a previous Peach crashed, 
				// reset all jobs status since we know 
				// we can only have a single liveJob running.

				var jobs = db.LoadTable<Job>()
					.Select(job => EnsureNotStale(db, job))
					.Where(job => job != null)
					.Where(job => !running.HasValue || (job.Status != JobStatus.Stopped) == running.Value)
					.Where(job => !dryrun.HasValue || job.DryRun == dryrun.Value)
					.Select(LoadJob)
					.ToList();

				return jobs;
			}
		}

		/// <summary>
		/// Create a new job
		/// </summary>
		/// <example>
		/// POST /p/jobs
		/// </example>
		/// <remarks>
		/// This is how you create a job
		/// </remarks>
		/// <param name="request">Options for the job to create</param>
		/// <returns>Newly created job</returns>
		[Route("")]
		[ResponseType(typeof(Job))]
		[SwaggerResponse(HttpStatusCode.BadRequest, Description = "Invalid job request")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		[SwaggerResponse(HttpStatusCode.Forbidden, Description = "Unable to start the job")]
		public IHttpActionResult Post([FromBody]JobRequest request)
		{
			if (string.IsNullOrEmpty(request.PitUrl))
				return BadRequest();

			var pit = PitDatabase.GetPitByUrl(request.PitUrl);
			if (pit == null)
				return NotFound();

			var pitFile = pit.Versions[0].Files[0].Name;

			var job = JobMonitor.Start(PitLibraryPath, pitFile, request);
			if (job == null)
				return Forbidden();

			return Ok(LoadJob(JobMonitor.GetJob()));
		}

		[Route("{id}")]
		[ResponseType(typeof(Job))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		public IHttpActionResult Get(Guid id)
		{
			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(id);
				if (job == null)
					return NotFound();

				job = EnsureNotStale(db, job);

				if (job == null)
					return NotFound();

				return Ok(LoadJob(job));
			}
		}

		[Route("{id}")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		public IHttpActionResult Delete(Guid id)
		{
			var liveJob = JobMonitor.GetJob();
			if (liveJob != null &&
				liveJob.Guid == id &&
				liveJob.Status != JobStatus.Stopped)
			{
				JobMonitor.Kill();
			}

			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(id);
				if (job == null)
					return NotFound();

				if (Directory.Exists(job.LogPath))
					Directory.Delete(job.LogPath, true);

				db.DeleteJob(id);

				return Ok();
			}
		}

		#endregion

		#region Faults / Test Results / Report

		[Obsolete]
		[Route("{id}/nodes")]
		[ResponseType(typeof(string[]))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		public IHttpActionResult GetNodes(Guid id)
		{
			return WithActiveJob(id, () => Ok(new[]
			{
				NodesController.MakeUrl(NodeGuid)
			}));
		}

		[Route("{id}/nodes/first")]
		[ResponseType(typeof(TestResult))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		public IHttpActionResult GetNodesFirst(Guid id)
		{
			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(id);
				if (job == null)
					return NotFound();

				var events = db.GetTestEventsByJob(id).ToList();

				var isActive = events.Any(x => x.Status == TestStatus.Active);
				var isFail = events.Any(x => x.Status == TestStatus.Fail);

				var sb = new StringBuilder();

				var logs = db.GetJobLogs(id);
				foreach (var log in logs)
					sb.AppendLine(log.Message);

				var debugLog = TryReadLog(job.DebugLogPath);
				if (debugLog != null)
					sb.Append(debugLog);

				var result = new TestResult
				{
					Status = isActive
						? TestStatus.Active
						: isFail ? TestStatus.Fail : TestStatus.Pass,
					Events = events,
					Log = sb.ToString(),
					LogUrl = MakeUrl(id.ToString(), "nodes", NodeGuid, "log"),
				};

				return Ok(result);
			}
		}

		[Obsolete]
		[Route("{id}/nodes/{nodeId}/log")]
		[ResultFile(".txt")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		public IHttpActionResult GetNodesLog(Guid id, Guid nodeId)
		{
			var streams = new List<Stream>();

			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(id);
				if (job == null)
					return NotFound();

				var logs = db.GetJobLogs(id).ToList();
				if (logs.Count > 0)
				{
					var ms = new MemoryStream();
					var writer = new StreamWriter(ms);
					foreach (var log in logs)
						writer.WriteLine(log.Message);
					streams.Add(ms);
				}

				if (File.Exists(job.DebugLogPath))
				{
					streams.Add(new FileStream(
						job.DebugLogPath,
						FileMode.Open,
						FileAccess.Read,
						FileShare.ReadWrite,
						64 * 1024,
						FileOptions.SequentialScan));
				}
			}

			return new StreamResult(new ConcatenatedStream(streams));
		}

		[Route("{id}/faults")]
		[ResponseType(typeof(IEnumerable<FaultSummary>))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		public IHttpActionResult GetFaults(Guid id)
		{
			return WithJobDatabase(id, (job, db) =>
			{
				var faults = db.LoadTable<FaultSummary>()
					.Select(x => LoadFaultSummary(job, x));
				return Ok(faults);
			}, Ok(Enumerable.Empty<FaultSummary>()));
		}

		[Route("{id}/faults/{faultId}")]
		[ResponseType(typeof(Fault))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job or fault does not exits")]
		public IHttpActionResult GetFault(Guid id, long faultId)
		{
			return WithJobDatabase(id, (job, db) =>
			{
				var fault = db.GetFaultById(faultId);
				if (fault == null)
					return NotFound();

				return Ok(LoadFault(job, fault));
			});
		}

		[Route("{id}/faults/{faultId}/data/{fileId}")]
		[ResultFile(".*")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job, fault or file does not exits")]
		public IHttpActionResult GetFaultFile(Guid id, long faultId, long fileId)
		{
			return WithJobDatabase(id, (job, db) =>
			{
				var fault = db.GetFaultById(faultId, false);
				if (fault == null)
					return NotFound();

				var file = db.GetFaultFileById(fileId);
				if (file == null)
					return NotFound();

				var path = Path.Combine(job.LogPath, fault.FaultPath, file.FullName);
				var info = new FileInfo(path);
				if (!info.Exists)
					return NotFound();

				return new FileResult(info);
			});
		}

		[Route("{id}/faults/{faultId}/archive")]
		[ResultFile(".zip")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job or fault does not exits")]
		public IHttpActionResult GetFaultArchive(Guid id, long faultId)
		{
			return WithJobDatabase(id, (job, db) =>
			{
				var fault = db.GetFaultById(faultId, false);
				var filename = "Fault-{0}.zip".Fmt(fault.Iteration);
				var dir = new DirectoryInfo(Path.Combine(job.LogPath, fault.FaultPath));
				return new ZipResult(filename, dir);
			});
		}

		[Route("{id}/report")]
		[ResultFile(".pdf")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		public IHttpActionResult GetReport(Guid id)
		{
			return WithJobDatabase(id, (job, db) =>
			{
				var file = new FileInfo(job.ReportPath);
				if (!file.Exists)
					return NotFound();

				return new FileResult(file);
			});
		}

		#endregion

		#region Metrics

		[Route("{id}/metrics/faultTimeline")]
		[ResponseType(typeof(IEnumerable<FaultTimelineMetric>))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		public IHttpActionResult GetFaultTimelineMetric(Guid id)
		{
			return Query<FaultTimelineMetric>(id);
		}

		[Route("{id}/metrics/bucketTimeline")]
		[ResponseType(typeof(IEnumerable<BucketTimelineMetric>))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		public IHttpActionResult GetBucketTimelineMetric(Guid id)
		{
			return Query<BucketTimelineMetric>(id);
		}

		[Route("{id}/metrics/mutators")]
		[ResponseType(typeof(IEnumerable<MutatorMetric>))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		public IHttpActionResult GetMutatorMetric(Guid id)
		{
			return Query<MutatorMetric>(id);
		}

		[Route("{id}/metrics/elements")]
		[ResponseType(typeof(IEnumerable<ElementMetric>))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		public IHttpActionResult GetElementMetric(Guid id)
		{
			return Query<ElementMetric>(id);
		}

		[Route("{id}/metrics/states")]
		[ResponseType(typeof(IEnumerable<StateMetric>))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		public IHttpActionResult GetStateMetric(Guid id)
		{
			return Query<StateMetric>(id);
		}

		[Route("{id}/metrics/dataset")]
		[ResponseType(typeof(IEnumerable<DatasetMetric>))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		public IHttpActionResult GetDatasetMetric(Guid id)
		{
			return Query<DatasetMetric>(id);
		}

		[Route("{id}/metrics/buckets")]
		[ResponseType(typeof(IEnumerable<BucketMetric>))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		public IHttpActionResult GetBucketMetric(Guid id)
		{
			return Query<BucketMetric>(id);
		}

		[Route("{id}/metrics/iterations")]
		[ResponseType(typeof(IEnumerable<IterationMetric>))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		public IHttpActionResult GetIterationMetric(Guid id)
		{
			return Query<IterationMetric>(id);
		}

		#endregion

		#region Pause / Continue / Stop / Kill

		[Route("{id}/pause")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		[SwaggerResponse(HttpStatusCode.Forbidden, Description = "Job state doesn't allow operation")]
		public IHttpActionResult GetPauseJob(Guid id)
		{
			return WithActiveJob(id, () => JobMonitor.Pause() ?
				(IHttpActionResult)Ok() : Forbidden());
		}

		[Route("{id}/continue")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		[SwaggerResponse(HttpStatusCode.Forbidden, Description = "Job state doesn't allow operation")]
		public IHttpActionResult GetContinueJob(Guid id)
		{
			return WithActiveJob(id, () => JobMonitor.Continue() ?
				(IHttpActionResult)Ok() : Forbidden());
		}

		[Route("{id}/stop")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		[SwaggerResponse(HttpStatusCode.Forbidden, Description = "Job state doesn't allow operation")]
		public IHttpActionResult GetStopJob(Guid id)
		{
			return WithActiveJob(id, () => JobMonitor.Stop() ?
				(IHttpActionResult)Ok() : Forbidden());
		}

		[Route("{id}/kill")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified job does not exits")]
		[SwaggerResponse(HttpStatusCode.Forbidden, Description = "Job state doesn't allow operation")]
		public IHttpActionResult GetKillJob(Guid id)
		{
			return WithActiveJob(id, () => JobMonitor.Kill() ?
				(IHttpActionResult)Ok() : Forbidden());
		}

		#endregion

		#region Helper Functions

		private IHttpActionResult Query<T>(Guid guid)
		{
			return WithJobDatabase(guid, (job, db) =>
			{
				if (!db.IsInitialized)
					return NotFound();
				return Ok(db.LoadTable<T>());
			}, Ok(Enumerable.Empty<T>()));
		}

		private IHttpActionResult WithJobDatabase(Guid id, Func<Job, JobDatabase, IHttpActionResult> fn, IHttpActionResult nodb = null)
		{
			Job job;

			using (var db = new NodeDatabase())
			{
				job = db.GetJob(id);
			}

			if (job == null)
				return NotFound();

			if (!File.Exists(job.DatabasePath))
				return nodb ?? NotFound();

			using (var db = new JobDatabase(job.DatabasePath))
			{
				db.Migrate();
				return fn(job, db);
			}
		}

		private IHttpActionResult WithActiveJob(Guid id, Func<IHttpActionResult> fn)
		{
			var job = JobMonitor.GetJob();
			if (job == null || job.Guid != id)
				return NotFound();
			return fn();
		}

		private static string TryReadLog(string path)
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

		private Job LoadJob(Job job)
		{
			var id = job.Id;

			job.JobUrl = MakeUrl(id);
			job.FaultsUrl = MakeUrl(id, "faults");
			job.NodesUrl = MakeUrl(id, "nodes");
			job.FirstNodeUrl = MakeUrl(id, "nodes", "first");

			if (File.Exists(job.ReportPath))
				job.ReportUrl = MakeUrl(id, "report");

			//TargetUrl = "",
			//TargetConfigUrl = "",
			//PeachUrl = "",
			//PackageFileUrl = "",

			if (JobMonitor.IsControlable && JobMonitor.IsTracking(job))
			{
				job.Commands = new JobCommands
				{
					StopUrl = MakeUrl(id, "stop"),
					ContinueUrl = MakeUrl(id, "continue"),
					PauseUrl = MakeUrl(id, "pause"),
					KillUrl = MakeUrl(id, "kill"),
				};
			}

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

		private static FaultSummary LoadFaultSummary(Job job, FaultSummary fault)
		{
			fault.FaultUrl = MakeUrl(job.Id, "faults", fault.Id.ToString());
			fault.ArchiveUrl = MakeUrl(job.Id, "faults", fault.Id.ToString(), "archive");
			return fault;
		}

		private FaultDetail LoadFault(Job job, FaultDetail fault)
		{
			LoadFaultSummary(job, fault);
			fault.PitUrl = job.PitUrl;
			fault.NodeUrl = NodesController.MakeUrl(NodeGuid);
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

		private Job EnsureNotStale(NodeDatabase db, Job job)
		{
			if (job.Status == JobStatus.Stopped)
				return job;

			if (job.Pid == JobMonitor.Pid)
			{
				if (!JobMonitor.IsTracking(job))
					MarkStale(db, job);
			}
			else
			{
				var pidExists = PidExists(job);

				// If the owner pid doesn't exist set stopped time to last heartbeat
				// This will update the job database which is single writer so we
				// have to wait for the pid to no longer exist.
				if (!pidExists)
					MarkStale(db, job);
			}

			return job;
		}

		private static void MarkStale(NodeDatabase db, Job job)
		{
			job.Status = JobStatus.Stopped;
			job.StopDate = job.HeartBeat;
			db.UpdateJob(job);
		}

		private static bool PidExists(Job job)
		{
			try
			{
				using (var p = SysProcess.GetProcessById((int)job.Pid))
				{
					// On OSX, invalid pids return a valid process object
					// but has existed is set to true.
					return !p.HasExited;
				}
			}
			catch (ArgumentException)
			{
				return false;
			}
		}

		#endregion
	}
}