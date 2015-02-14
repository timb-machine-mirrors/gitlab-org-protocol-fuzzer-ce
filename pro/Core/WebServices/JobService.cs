using System;
using System.Collections.Generic;
using System.Diagnostics;
using Nancy;
using Nancy.ModelBinding;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core.WebServices
{
	public class JobService : WebService
	{
		public static readonly string Prefix = "/p/jobs";

		public JobService(WebContext context)
			: base(context, Prefix)
		{
			Get[""] = _ => GetJobs();
			Get["/{id}"] = _ => GetJob(_.id);
			Get["/{id}/nodes"] = _ => GetNodes(_.id);
			Get["/{id}/faults"] = _ => GetFaults(_.id);
			Get["/{id}/visualizer"] = _ => GetVisualizer(_.id);

			Post[""] = _ => CreateJob();

			Get["/{id}/continue"] = _ => ContinueJob(_.id);
			Get["/{id}/pause"] = _ => PauseJob(_.id);
			Get["/{id}/stop"] = _ => StopJob(_.id);
			Get["/{id}/kill"] = _ => KillJob(_.id);
		}

		object CreateJob()
		{
			var job = this.Bind<Job>();
			if (string.IsNullOrEmpty(job.PitUrl))
				return HttpStatusCode.BadRequest;

			var pit = PitDatabase.GetPitByUrl(job.PitUrl);
			if (pit == null)
				return HttpStatusCode.NotFound;

			lock (Mutex)
			{
				if (IsEngineRunning)
					return HttpStatusCode.Forbidden;

				StartJob(pit, job.Seed, job.RangeStart, job.RangeStop);

				return MakeJob();
			}
		}

		object PauseJob(string id)
		{
			return QueryJob(id, () => Runner.Pause() ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		object ContinueJob(string id)
		{
			return QueryJob(id, () => Runner.Continue() ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		object StopJob(string id)
		{
			return QueryJob(id, () => Runner.Stop() ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		object KillJob(string id)
		{
			return QueryJob(id, () => Runner.Kill() ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		object GetJobs()
		{
			lock (Mutex)
			{
				if (Runner == null)
					return new Job[0];

				return new[] { MakeJob() };
			}
		}

		object GetJob(string id)
		{
			return QueryJob(id, MakeJob);
		}

		object GetNodes(string id)
		{
			return QueryJob(id, () => new[] { NodeService.Prefix + "/" + NodeGuid });
		}

		object GetFaults(string id)
		{
			return QueryJob(id, () => Logger.Faults);
		}

		object GetVisualizer(string id)
		{
			return QueryJob(id, () => Logger.Visualizer);
		}

		object QueryJob(string id, Func<object> query)
		{
			lock (Mutex)
			{
				if (Runner  == null || Runner.Guid != id)
					return HttpStatusCode.NotFound;

				return query();
			}
		}

		/// <summary>
		/// Make a job record.  Needs to be called with the logger locked!
		/// </summary>
		/// <returns>The resultant job record.</returns>
		Job MakeJob()
		{
			Debug.Assert(Runner != null);

			var elapsed = Runner.Runtime;

			var group = new Group()
			{
				Access = GroupAccess.Read | GroupAccess.Write,
				GroupUrl = "",
			};

			var job = new Job()
			{
				JobUrl = Prefix + "/" + Runner.Guid,
				FaultsUrl = Prefix + "/" + Runner.Guid + "/faults",
				TargetUrl = "",
				TargetConfigUrl = "",
				NodesUrl = Prefix + "/" + Runner.Guid + "/nodes",
				PitUrl = Runner.PitUrl,
				PeachUrl = "",
				ReportUrl = "",
				PackageFileUrl = "",

				Commands = new JobCommands
				{
					StopUrl = Prefix + "/" + Runner.Guid + "/stop",
					ContinueUrl = Prefix + "/" + Runner.Guid + "/continue",
					PauseUrl = Prefix + "/" + Runner.Guid + "/pause",
					KillUrl = Prefix + "/" + Runner.Guid + "/kill",
				},

				Metrics = new JobMetrics
				{
					BucketTimeline = Prefix + "/" + Runner.Guid + "/metrics/bucketTimeline",
					FaultTimeline = Prefix + "/" + Runner.Guid + "/metrics/faultTimeline",
					Mutators = Prefix + "/" + Runner.Guid + "/metrics/mutators",
					Elements = Prefix + "/" + Runner.Guid + "/metrics/elements",
					States = Prefix + "/" + Runner.Guid + "/metrics/states",
					Dataset = Prefix + "/" + Runner.Guid + "/metrics/dataset",
					Buckets = Prefix + "/" + Runner.Guid + "/metrics/buckets",
					Iterations = Prefix + "/" + Runner.Guid + "/metrics/iterations",
				},

				Status = Runner.Status,
				Mode = Logger.Mode,
				Name = Runner.Name,
				Notes = "",
				User = Environment.UserName,
				Seed = Runner.Seed,
				IterationCount = Logger.CurrentIteration,
				StartDate = Runner.StartDate,
				Runtime = (uint)elapsed.TotalSeconds,
				Speed = (uint)((Logger.CurrentIteration - Logger.StartIteration) / elapsed.TotalHours),
				FaultCount = Logger.FaultCount,
				Tags = new List<Tag>(),
				Groups = new List<Group>(new[] { group }),
				HasMetrics = Runner.HasMetrics,
			};

			if (Runner.Status == JobStatus.Stopped)
			{
				job.StopDate = Runner.StopDate;
				job.Result = Runner.Result;
			}

			return job;
		}
	}
}
