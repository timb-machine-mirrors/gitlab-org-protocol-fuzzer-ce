using Nancy;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Enterprise.WebServices
{
	public class JobService : NancyModule
	{
		public JobService()
			: base("/p/jobs")
		{
			Get[""] = _ => GetJobs();
			Get["/{id}"] = _ => GetJob(_.id);
		}

		Job[] GetJobs()
		{
			return new Job[0];
		}

		Job GetJob(string id)
		{
			Context.Response.StatusCode = HttpStatusCode.NotFound;
			return null;
		}
	}
}
