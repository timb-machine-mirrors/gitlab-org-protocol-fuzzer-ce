using System;
using Nancy;
using Nancy.ModelBinding;
using Peach.Core;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Core.Storage;
using System.Linq;
using System.IO;
using Peach.Pro.Core.WebServices.Utility;
using System.Collections.Generic;
using System.Text;

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
			Get["/{id}/nodes/first"] = _ => GetFirstNode(_.id);
			Get["/{id}/nodes/{nid}/log"] = _ => GetLog(_.id, _.nid);

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

		Response GetFirstNode(Guid id)
		{
			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(id);
				if (job == null)
					return HttpStatusCode.NotFound;

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

				return Response.AsJson(result);
			}
		}

		Response GetLog(Guid jobId, Guid nodeId)
		{
			var streams = new List<Stream>();

			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(jobId);
				if (job == null)
					return HttpStatusCode.NotFound;

				var logs = db.GetJobLogs(jobId).ToList();
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

			return Response.FromStream(new ConcatenatedStream(streams), "text/plain");
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

			var job = JobResolver.GetJob(id);
			if (job == null)
				return HttpStatusCode.NotFound;

			if (FixStaleJob(liveJob, job))
			{
				using (var db = new NodeDatabase())
				{
					db.UpdateJob(job);
				}

				if (File.Exists(job.DatabasePath))
				{
					using (var db = new JobDatabase(job.DatabasePath))
					{
						db.UpdateJob(job);
					}
				}
			}

			return Response.AsJson(LoadJob(job));
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

			if (job == null || !File.Exists(job.DatabasePath))
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
			job.FirstNodeUrl = MakeUrl(id, "nodes", "first");

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

	class ConcatenatedStream : Stream
	{
		Queue<Stream> _streams;
		long _position;

		public ConcatenatedStream(IEnumerable<Stream> streams)
		{
			_streams = new Queue<Stream>(streams);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			var result = 0;

			while (count > 0 && _streams.Count > 0)
			{
				int bytesRead = _streams.Peek().Read(buffer, offset, count);
				result += bytesRead;
				offset += bytesRead;
				count -= bytesRead;
				_position += bytesRead;

				if (count > 0)
					_streams.Dequeue();
			}

			return result;
		}

		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return false; }
		}

		public override void Flush()
		{
			foreach (var stream in _streams)
				stream.Flush();
		}

		public override long Length
		{
			get
			{
				var result = 0L;
				foreach (var stream in _streams)
					result += stream.Length;
				return result;
			}
		}

		public override long Position
		{
			get { return _position; }
			set { throw new NotImplementedException(); }
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}
	}
}
