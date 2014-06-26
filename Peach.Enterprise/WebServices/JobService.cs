using Nancy;
using Nancy.ModelBinding;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Enterprise.WebServices
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
			var job = this.Bind<Models.Job>();
			if (string.IsNullOrEmpty(job.PitUrl))
				return HttpStatusCode.BadRequest;

			var pit = PitDatabase.GetPitByUrl(job.PitUrl);
			if (pit == null)
				return HttpStatusCode.NotFound;

			lock (Mutex)
			{
				if (IsEngineRunning)
					return HttpStatusCode.Forbidden;

				StartJob(pit);

				return Response.AsJson(new { JobUrl = Prefix + "/" + Runner.Guid });
			}
		}

		object PauseJob(string id)
		{
			return QueryJob(id, () => { return Runner.Pause() ? HttpStatusCode.OK : HttpStatusCode.Forbidden; });
		}

		object ContinueJob(string id)
		{
			return QueryJob(id, () => { return Runner.Continue() ? HttpStatusCode.OK : HttpStatusCode.Forbidden; });
		}

		object StopJob(string id)
		{
			return QueryJob(id, () => { return Runner.Stop() ? HttpStatusCode.OK : HttpStatusCode.Forbidden; });
		}

		object KillJob(string id)
		{
			return QueryJob(id, () => { return Runner.Kill() ? HttpStatusCode.OK : HttpStatusCode.Forbidden; });
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
			return QueryJob(id, () => { return MakeJob(); });
		}

		object GetNodes(string id)
		{
			return QueryJob(id, () => { return new[] { NodeService.Prefix + "/" + NodeGuid }; });
		}

		object GetFaults(string id)
		{
			return QueryJob(id, () => { return Logger.Faults; });
		}

		object GetVisualizer(string id)
		{
			return QueryJob(id, () => { return Logger.Visualizer; });
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
			System.Diagnostics.Debug.Assert(Runner != null);

			var end = Runner.Status == JobStatus.Stopped ? Runner.StopDate : DateTime.UtcNow;
			var elapsed = end - Runner.StartDate;

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

				Status = Runner.Status,
				Name = Runner.Name,
				Notes = "",
				User = Environment.UserName,
				Seed = Runner.Seed,
				IterationCount = Logger.CurrentIteration,
				StartDate = Runner.StartDate,
				StopDate = Runner.StopDate,
				Runtime = (uint)Runner.Runtime.TotalSeconds,
				Speed = (uint)((Logger.CurrentIteration - Logger.StartIteration) / elapsed.TotalHours),
				FaultCount = Logger.FaultCount,
				Tags = new List<Tag>(),
				Groups = new List<Group>(new[] { group }),
			};

			return job;
		}
	}
}
