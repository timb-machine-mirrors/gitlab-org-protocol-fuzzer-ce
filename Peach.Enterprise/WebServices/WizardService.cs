using Nancy;
using Nancy.ModelBinding;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Enterprise.WebServices
{
	public class WizardService : NancyModule
	{
		public static readonly string Prefix = "/p/conf/wizard";

		WebLogger logger;

		public WizardService(WebLogger logger)
			: base(Prefix)
		{
			this.logger = logger;

			Post["/config"] = _ => PostConfig();
		}

		object PostConfig()
		{
			lock (logger)
			{
				if (logger.JobGuid != null)
					return HttpStatusCode.Conflict;

				var agents = this.Bind<List<Models.Agent>>();

				if (agents.Count == 0)
					return HttpStatusCode.ImATeapot;

				return HttpStatusCode.OK;
			}
		}
	}
}
