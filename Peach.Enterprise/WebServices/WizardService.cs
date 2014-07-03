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
	public class WizardService : WebService
	{
		public static readonly string Prefix = "/p/conf/wizard";

		public WizardService(WebContext context)
			: base(context, Prefix)
		{
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

			ret.Add(new KeyValuePair<string, string>("LocalOS", Peach.Core.Platform.GetOS().ToString().ToLower()));

			return ret;
		}

		object PostConfig()
		{
			var cfg = this.Bind<PitConfig>();
			var pit = PitDatabase.GetPitByUrl(cfg.PitUrl);
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
			var pit = PitDatabase.GetPitByUrl(monitors.PitUrl);
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

			var pit = PitDatabase.GetPitByUrl(pitUrl);
			if (pit == null)
				return HttpStatusCode.NotFound;

			lock (Mutex)
			{
				if (IsEngineRunning)
					return HttpStatusCode.Forbidden;

				StartTest(pit);

				return Response.AsJson(new { TestUrl = Prefix + "/test/" + Tester.Guid });
			}
		}

		object GetTest(string id)
		{
			lock (Mutex)
			{
				if (Tester == null || Tester.Guid != id)
					return HttpStatusCode.NotFound;

				return Tester.Result;
			}
		}

		object GetTestRaw(string id)
		{
			IEnumerable<string> lines;

			lock (Mutex)
			{
				if (Tester == null || Tester.Guid != id)
					return HttpStatusCode.NotFound;

				lines = Tester.Log;
			}

			var writer = new StreamWriter(new MemoryStream());

			foreach (var line in lines)
				writer.WriteLine(line);

			writer.Flush();

			writer.BaseStream.Position = 0;

			return Response.FromStream(writer.BaseStream, "text/plain");
		}
	}
}
