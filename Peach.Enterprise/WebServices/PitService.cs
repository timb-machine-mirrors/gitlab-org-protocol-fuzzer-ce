using Nancy;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Enterprise.WebServices
{
	public class PitService : NancyModule
	{
		public PitService()
			: base("/p/pits")
		{
			Get[""] = _ => GetPits();
			Get["/{id}"] = _ => GetPit(_.id);
			Get["/{id}/config"] = _ => GetPitConfig(_.id);
		}

		Pit[] GetPits()
		{
			var db = new PitDatabase();
			return db.Entries.ToArray();
		}

		Pit GetPit(string id)
		{
			var db = new PitDatabase();
			var pit = db.GetPit(id);
			if (pit == null)
				Context.Response.StatusCode = HttpStatusCode.NotFound;
			return pit;
		}

		PitConfig GetPitConfig(string id)
		{
			var pit = GetPit(id);
			if (pit == null)
				return null;

			var fileName = pit.Versions[0].Files[0].Name + ".config";

			var defines = Peach.Core.Analyzers.PitParser.parseDefines(fileName);

			var ret = new PitConfig()
			{
				PitUrl = pit.PitUrl,
				Config = new List<ConfigItem>(),
			};

			foreach (var d in defines)
			{
				ret.Config.Add(new ConfigItem()
				{
					Key = d.Key,
					Value = d.Value,
					Name = d.Key,
					Type = ConfigType.String,
					Defaults = new List<string>(),
				});
			}

			return ret;
		}
	}
}
