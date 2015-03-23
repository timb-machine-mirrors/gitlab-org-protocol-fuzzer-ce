using System;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Peach.Pro.Core.WebServices.Models;
using Peach.Core;
using Peach.Pro.Core.Storage;
using System.Linq;

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
			Delete["/{id}"] = _ => DeleteJob(_.id);
			Get["/{id}/nodes"] = _ => GetNodes(_.id);
			Get["/{id}/faults"] = _ => GetFaults(_.id);
			// deprecated
			Get["/{id}/visualizer"] = _ => GetVisualizer(_.id);

			Post[""] = _ => CreateJob();

			Get["/{id}/continue"] = _ => ContinueJob(_.id);
			Get["/{id}/pause"] = _ => PauseJob(_.id);
			Get["/{id}/stop"] = _ => StopJob(_.id);
			Get["/{id}/kill"] = _ => KillJob(_.id);
		}

		Response CreateJob()
		{
			var job = this.Bind<Job>();
			if (string.IsNullOrEmpty(job.PitUrl))
				return HttpStatusCode.BadRequest;

			var pit = PitDatabase.GetPitByUrl(job.PitUrl);
			if (pit == null)
				return HttpStatusCode.NotFound;

			var runner = JobRunner.Run(Prefix, PitLibraryPath, pit, job);
			return Response.AsJson(runner.Job);
		}

		Response DeleteJob(Guid id)
		{
			var runner = JobRunner.Get(id);
			if (runner != null)
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
			return QueryJob(id, r => { r.Pause(); return HttpStatusCode.OK; });
		}

		Response ContinueJob(Guid id)
		{
			return QueryJob(id, r => { r.Continue(); return HttpStatusCode.OK; });
		}

		Response StopJob(Guid id)
		{
			return QueryJob(id, r => { r.Stop(); return HttpStatusCode.OK; });
		}

		Response KillJob(Guid id)
		{
			return QueryJob(id, r => { r.Kill(); return HttpStatusCode.OK; });
		}

		Response GetNodes(Guid id)
		{
			return QueryJob(id, x => Response.AsJson(new[] { NodeService.Prefix + "/" + NodeGuid }));
		}

		Response GetJobs()
		{
			using (var db = new NodeDatabase())
			{
				var jobs = db.LoadTable<Job>();
				return Response.AsJson(jobs.Select(x => LoadJob(x)));
			}
		}

		Response GetJob(Guid id)
		{
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
			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(id);
				if (job == null)
					return HttpStatusCode.NotFound;
			}

			using (var db = new JobDatabase(id))
			{
				var faults = db.LoadTable<FaultDetail>()
					.OfType<FaultSummary>();
				return Response.AsJson(faults);
			}
		}

		[Obsolete]
		Response GetVisualizer(Guid id)
		{
			return HttpStatusCode.NotImplemented;
		}

		Response QueryJob(Guid id, Func<JobRunner, Response> query)
		{
			var runner = JobRunner.Get(id);
			if (runner == null)
				return HttpStatusCode.NotFound;
			return query(runner);
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

		static string MakeUrl(params string[] args)
		{
			return string.Join("/", Prefix, string.Join("/", args));
		}

		///// <summary>
		///// Make a job record.
		///// </summary>
		///// <returns>The resultant job record.</returns>
		//Job LoadJob(JobRunner runner)
		//{
		//var elapsed = runner.Runtime;

		//var group = new Group
		//{
		//	Access = GroupAccess.Read | GroupAccess.Write,
		//	GroupUrl = "",
		//};

		// TODO: read current job status from datastore

		//var job = new Job
		//{
		//	JobUrl = Prefix + "/" + runner.Guid,
		//	FaultsUrl = Prefix + "/" + runner.Guid + "/faults",
		//	TargetUrl = "",
		//	TargetConfigUrl = "",
		//	NodesUrl = Prefix + "/" + runner.Guid + "/nodes",
		//	PitUrl = runner.PitUrl,
		//	PeachUrl = "",
		//	ReportUrl = "",
		//	PackageFileUrl = "",

		//	Commands = new JobCommands
		//	{
		//		StopUrl = Prefix + "/" + runner.Guid + "/stop",
		//		ContinueUrl = Prefix + "/" + runner.Guid + "/continue",
		//		PauseUrl = Prefix + "/" + runner.Guid + "/pause",
		//		KillUrl = Prefix + "/" + runner.Guid + "/kill",
		//	},

		//	Metrics = new JobMetrics
		//	{
		//		BucketTimeline = Prefix + "/" + runner.Guid + "/metrics/bucketTimeline",
		//		FaultTimeline = Prefix + "/" + runner.Guid + "/metrics/faultTimeline",
		//		Mutators = Prefix + "/" + runner.Guid + "/metrics/mutators",
		//		Elements = Prefix + "/" + runner.Guid + "/metrics/elements",
		//		States = Prefix + "/" + runner.Guid + "/metrics/states",
		//		Dataset = Prefix + "/" + runner.Guid + "/metrics/dataset",
		//		Buckets = Prefix + "/" + runner.Guid + "/metrics/buckets",
		//		Iterations = Prefix + "/" + runner.Guid + "/metrics/iterations",
		//	},

		//	//Status = runner.Status,
		//	//Mode = Logger.Mode,
		//	//Name = runner.Name,
		//	Notes = "",
		//	User = Environment.UserName,
		//	//Seed = runner.Seed,
		//	//IterationCount = Logger.CurrentIteration,
		//	StartDate = runner.StartDate,
		//	Runtime = (uint)elapsed.TotalSeconds,
		//	//Speed = (uint)((Logger.CurrentIteration - Logger.StartIteration) / elapsed.TotalHours),
		//	//FaultCount = Logger.FaultCount,
		//	//Tags = new List<Tag>(),
		//	//Groups = new List<Group>(new[] { group }),
		//	//HasMetrics = runner.HasMetrics,
		//};

		//if (runner.Status == JobStatus.Stopped)
		//{
		//	job.StopDate = runner.StopDate;
		//	job.Result = runner.Result;
		//}

		//return job;
		//}
	}
}
