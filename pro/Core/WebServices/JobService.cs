using System;
using System.Collections.Generic;
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

			var runner = JobRunner.Run(
				PitLibraryPath,
				pit.Versions[0].Files[0].Name,
				pit.PitUrl,
				(uint?)job.Seed,
				(uint)job.RangeStart,
				(uint)job.RangeStop
			);

			return MakeJob(runner);
		}

		object PauseJob(string id)
		{
			return QueryJob(id, r => r.Pause() ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		object ContinueJob(string id)
		{
			return QueryJob(id, r => r.Continue() ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		object StopJob(string id)
		{
			return QueryJob(id, r => r.Stop() ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		object KillJob(string id)
		{
			return QueryJob(id, r => r.Kill() ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
		}

		object GetJobs()
		{
			// TODO: return list of jobs from datastore
			return null;

			//lock (Mutex)
			//{
			//	if (Runner == null)
			//		return new Job[0];

			//	return new[] { MakeJob() };
			//}
		}

		object GetJob(string id)
		{
			return QueryJob(id, MakeJob);
		}

		object GetNodes(string id)
		{
			return QueryJob(id, x => new[] { NodeService.Prefix + "/" + NodeGuid });
		}

		object GetFaults(string id)
		{
			// TODO: read faults from datastore
			return null;
			//return QueryJob(id, () => Logger.Faults);
		}

		object GetVisualizer(string id)
		{
			// TODO: read visulizer from datastore
			return null;
			//return QueryJob(id, () => Logger.Visualizer);
		}

		object QueryJob(string id, Func<JobRunner, object> query)
		{
			var runner = JobRunner.Get(id);
			if (runner == null)
				return HttpStatusCode.NotFound;
			return query(runner);
		}

		/// <summary>
		/// Make a job record.
		/// </summary>
		/// <returns>The resultant job record.</returns>
		Job MakeJob(JobRunner runner)
		{
			var elapsed = runner.Runtime;

			//var group = new Group
			//{
			//	Access = GroupAccess.Read | GroupAccess.Write,
			//	GroupUrl = "",
			//};

			// TODO: read current job status from datastore

			var job = new Job
			{
				JobUrl = Prefix + "/" + runner.Guid,
				FaultsUrl = Prefix + "/" + runner.Guid + "/faults",
				TargetUrl = "",
				TargetConfigUrl = "",
				NodesUrl = Prefix + "/" + runner.Guid + "/nodes",
				PitUrl = runner.PitUrl,
				PeachUrl = "",
				ReportUrl = "",
				PackageFileUrl = "",

				Commands = new JobCommands
				{
					StopUrl = Prefix + "/" + runner.Guid + "/stop",
					ContinueUrl = Prefix + "/" + runner.Guid + "/continue",
					PauseUrl = Prefix + "/" + runner.Guid + "/pause",
					KillUrl = Prefix + "/" + runner.Guid + "/kill",
				},

				Metrics = new JobMetrics
				{
					BucketTimeline = Prefix + "/" + runner.Guid + "/metrics/bucketTimeline",
					FaultTimeline = Prefix + "/" + runner.Guid + "/metrics/faultTimeline",
					Mutators = Prefix + "/" + runner.Guid + "/metrics/mutators",
					Elements = Prefix + "/" + runner.Guid + "/metrics/elements",
					States = Prefix + "/" + runner.Guid + "/metrics/states",
					Dataset = Prefix + "/" + runner.Guid + "/metrics/dataset",
					Buckets = Prefix + "/" + runner.Guid + "/metrics/buckets",
					Iterations = Prefix + "/" + runner.Guid + "/metrics/iterations",
				},

				Status = runner.Status,
				//Mode = Logger.Mode,
				Name = runner.Name,
				Notes = "",
				User = Environment.UserName,
				Seed = runner.Seed,
				//IterationCount = Logger.CurrentIteration,
				StartDate = runner.StartDate,
				Runtime = (uint)elapsed.TotalSeconds,
				//Speed = (uint)((Logger.CurrentIteration - Logger.StartIteration) / elapsed.TotalHours),
				//FaultCount = Logger.FaultCount,
				//Tags = new List<Tag>(),
				//Groups = new List<Group>(new[] { group }),
				HasMetrics = runner.HasMetrics,
			};

			if (runner.Status == JobStatus.Stopped)
			{
				job.StopDate = runner.StopDate;
				job.Result = runner.Result;
			}

			return job;
		}
	}
}
