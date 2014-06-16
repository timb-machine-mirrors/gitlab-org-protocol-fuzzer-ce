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
			Post["/monitors"] = _ => PostMonitors();

			// /test/start?pitUrl=xxxx -> return /test/{id}
			// /test/{id}
			// /test/{id}/raw
		}

		object PostConfig()
		{
			/*var cfg = */this.Bind<PitConfig>();
			return HttpStatusCode.OK;
		}

		object PostMonitors()
		{
			/*var monitors = */this.Bind<PitMonitors>();
			return HttpStatusCode.OK;
		}
	}
}
