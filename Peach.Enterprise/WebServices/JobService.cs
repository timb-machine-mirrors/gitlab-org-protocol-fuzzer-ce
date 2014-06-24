using Nancy;
using Nancy.ModelBinding;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Enterprise.WebServices
{
	public class JobService : NancyModule
	{
		public static readonly string Prefix = "/p/jobs";

		WebLogger logger;

		public JobService(WebLogger logger)
			: base(Prefix)
		{
			this.logger = logger;

			Get[""] = _ => GetJobs();
			Get["/{id}"] = _ => GetJob(_.id);
			Get["/{id}/nodes"] = _ => GetNodes(_.id);
			Get["/{id}/faults"] = _ => GetFaults(_.id);
			Get["/{id}/visualizer"] = _ => GetVisualizer(_.id);

			Post[""] = _ => CreateJob();

			Get["/{id}/start"] = _ => StartJob(_.id);
			Get["/{id}/pause"] = _ => PauseJob(_.id);
			Get["/{id}/stop"] = _ => StopJob(_.id);
		}

		string PitLibraryPath
		{
			get
			{
				return (string)Context.Items["PitLibraryPath"];
			}
		}

		object CreateJob()
		{
			var job = this.Bind<Models.Job>();
			if (string.IsNullOrEmpty(job.PitUrl))
				return HttpStatusCode.BadRequest;

			var db = new PitDatabase(PitLibraryPath);
			var pit = db.GetPitByUrl(job.PitUrl);
			if (pit == null)
				return HttpStatusCode.NotFound;

			lock (logger)
			{
				if (logger.Busy)
					return HttpStatusCode.Forbidden;

				logger.RunJob(".", pit.Versions[0].Files[0].Name);
			}

			return HttpStatusCode.OK;
		}

		object PauseJob(string id)
		{
			lock (logger)
			{
				if (logger.Thread == null)
					return HttpStatusCode.Forbidden;

				if (logger.JobGuid != id)
					return HttpStatusCode.NotFound;

				logger.PauseJob();

				return HttpStatusCode.OK;
			}
		}

		object StartJob(string id)
		{
			lock (logger)
			{
				if (logger.Thread == null)
					return HttpStatusCode.Forbidden;

				if (logger.JobGuid != id)
					return HttpStatusCode.NotFound;

				logger.ResumeJob();

				return HttpStatusCode.OK;
			}
		}

		object StopJob(string id)
		{
			System.Threading.Thread th = null;

			lock (logger)
			{
				if (logger.Thread == null)
					return HttpStatusCode.Forbidden;

				if (logger.JobGuid != id)
					return HttpStatusCode.NotFound;

				th = logger.Thread;
				th.Abort();
			}

			if (th != null)
				th.Join();

			return HttpStatusCode.OK;
		}

		object GetJobs()
		{
			lock (logger)
			{
				if (logger.JobGuid == null)
					return new Job[0];

				return new[] { MakeJob() };
			}
		}

		object GetJob(string id)
		{
			lock (logger)
			{
				if (logger.JobGuid != id)
					return HttpStatusCode.NotFound;

				return MakeJob();
			}
		}

		object GetNodes(string id)
		{
			lock (logger)
			{
				if (logger.JobGuid != id)
					return HttpStatusCode.NotFound;

				return new[] { NodeService.Prefix + "/" + logger.NodeGuid };
			}
		}

		object GetFaults(string id)
		{
			lock (logger)
			{
				if (logger.JobGuid != id)
					return HttpStatusCode.NotFound;

				return logger.Faults;
			}
		}

		object GetVisualizer(string id)
		{
			lock (logger)
			{
				if (logger.JobGuid != id)
					return HttpStatusCode.NotFound;

				return logger.Visualizer;
			}
		}

		/// <summary>
		/// Make a job record.  Needs to be called with the logger locked!
		/// </summary>
		/// <returns>The resultant job record.</returns>
		Job MakeJob()
		{
			var elapsed = DateTime.UtcNow - logger.StartDate;

			var group = new Group()
			{
				Access = GroupAccess.Read | GroupAccess.Write,
				GroupUrl = "",
			};

			var job = new Job()
			{
				JobUrl = Prefix + "/" + logger.JobGuid,
				FaultsUrl = Prefix + "/" + logger.JobGuid + "/faults",
				TargetUrl = "",
				TargetConfigUrl = "",
				NodesUrl = Prefix + "/" + logger.JobGuid + "/nodes",
				PitUrl = "",
				PeachUrl = "",
				ReportUrl = "",
				PackageFileUrl = "",

				Name = logger.Name,
				Notes = "",
				User = Environment.UserName,
				Seed = logger.Seed,
				IterationCount = logger.CurrentIteration,
				StartDate = logger.StartDate,
				StopDate = new DateTime(),
				Runtime = (uint)elapsed.TotalSeconds,
				Speed = (uint)((logger.CurrentIteration - logger.StartIteration) / elapsed.TotalHours),
				FaultCount = logger.FaultCount,
				Tags = new List<Tag>(),
				Groups = new List<Group>(new[] { group }),
			};

			return job;
		}
	}
}
