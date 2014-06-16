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

		object GetPits()
		{
			var db = new PitDatabase(".");
			return db.Entries.ToArray();
		}

		object GetPit(string id)
		{
			var db = new PitDatabase(".");
			var pit = db.GetPit(id);
			if (pit == null)
				return HttpStatusCode.NotFound;

			return pit;
		}

		object GetPitConfig(string id)
		{
			var db = new PitDatabase(".");
			var cfg = db.GetConfig(id);
			if (cfg == null)
				return HttpStatusCode.NotFound;

			return cfg;
		}
	}
}
