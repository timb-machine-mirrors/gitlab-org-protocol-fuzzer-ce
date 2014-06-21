using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.IO;
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

			Get["/state"] = _ => GetState();

			Post["/config"] = _ => PostConfig();
			Post["/monitors"] = _ => PostMonitors();

			Get["/test/start"] = _ => StartTest();
			Get["/test/{id}"] = _ => GetTest(_.id);
			Get["/test/{id}/raw"] = _ => GetTestRaw(_.id);
		}

		
		object GetState()
		{
			var ret = new List<KeyValuePair<string, string>>();

			ret.Add(new KeyValuePair<string, string>("OS", Peach.Core.Platform.GetOS().ToString()));

			return ret;
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
				return HttpStatusCode.Forbidden;

			PitDatabase.SaveConfig(pit, cfg.Config);

			return HttpStatusCode.OK;
		}

		object PostMonitors()
		{
			var monitors = this.Bind<PitMonitors>();
			var db = new PitDatabase(".");
			var pit = db.GetPitByUrl(monitors.PitUrl);
			if (pit == null)
				return HttpStatusCode.NotFound;

			// Don't allow changing configuration values
			// of locked pits
			if (pit.Locked)
				return HttpStatusCode.Forbidden;

			PitDatabase.SaveMonitors(pit, monitors.Monitors);

			return HttpStatusCode.OK;
		}

		object StartTest()
		{
			var pitUrl = Request.Query.pitUrl;

			var db = new PitDatabase(".");
			var pit = db.GetPitByUrl(pitUrl);
			if (pit == null)
				return HttpStatusCode.NotFound;

			lock (logger)
			{
				if (logger.Busy)
					return HttpStatusCode.Forbidden;

				logger.Tester = new PitTester(".", pit.Versions[0].Files[0].Name);

				return Response.AsJson(new { TestUrl = Prefix + "/test/" + logger.Tester.Guid });
			}
		}

		object GetTest(string id)
		{
			lock (logger)
			{
				if (logger.Tester == null || logger.Tester.Guid != id)
					return HttpStatusCode.NotFound;

				return logger.Tester.Result;
			}
		}

		object GetTestRaw(string id)
		{
			IList<string> lines;

			lock (logger)
			{
				if (logger.Tester == null || logger.Tester.Guid != id)
					return HttpStatusCode.NotFound;

				lines = logger.Tester.Log;
			}

			var writer = new StreamWriter(new MemoryStream());
				
			foreach (var line in logger.Tester.Log)
				writer.WriteLine(line);

			writer.Flush();

			writer.BaseStream.Position = 0;

			return Response.FromStream(writer.BaseStream, "text/plain");
		}
	}
}
