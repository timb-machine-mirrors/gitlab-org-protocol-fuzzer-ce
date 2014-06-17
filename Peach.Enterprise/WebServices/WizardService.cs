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
			var cfg = this.Bind<PitConfig>();
			var db = new PitDatabase(".");
			var pit = db.GetPitByUrl(cfg.PitUrl);
			if (pit == null)
				return HttpStatusCode.NotFound;

			// Don't allow changing configuration values
			// of locked pits
			if (pit.Locked)
				return HttpStatusCode.Unauthorized;

			var fileName = pit.Versions[0].Files[0].Name + ".config";
			var defines = PitDefines.Parse(fileName);

			// For now, read in the current defines off disk and
			// apply any applicable changes. We don't currently expect
			// this api to add new defines or delete old defines.
			foreach (var item in cfg.Config)
			{
				foreach (var def in defines.Where(d => d.Key == item.Key))
					def.Value = item.Value;
			}

			var final = new PitDefines()
			{
				Platforms = new List<PitDefines.Collection>(new[] {
					new PitDefines.All()
					{
						Defines = defines,
					}
				}),
			};

			XmlTools.Serialize(fileName, final);

			return HttpStatusCode.OK;
		}

		object PostMonitors()
		{
			/*var monitors = */this.Bind<PitMonitors>();
			return HttpStatusCode.OK;
		}
	}
}
