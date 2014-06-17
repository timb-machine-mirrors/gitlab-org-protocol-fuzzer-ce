using Nancy;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Enterprise.WebServices
{
	public class PitService : NancyModule
	{
		public static readonly string Prefix = "/p/pits";

		public PitService()
			: base(Prefix)
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
			var pit = db.GetPitById(id);
			if (pit == null)
				return HttpStatusCode.NotFound;

			return pit;
		}

		object GetPitConfig(string id)
		{
			var db = new PitDatabase(".");
			var cfg = db.GetConfigById(id);
			if (cfg == null)
				return HttpStatusCode.NotFound;

			return cfg;
		}
	}
}
