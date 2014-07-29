using Nancy;
using Nancy.ModelBinding;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Enterprise.WebServices
{
	public class PitService : WebService
	{
		public static readonly string Prefix = "/p/pits";

		public PitService(WebContext context)
			: base(context, Prefix)
		{
			Get[""] = _ => GetPits();
			Get["/{id}"] = _ => GetPit(_.id);
			Get["/{id}/config"] = _ => GetPitConfig(_.id);

			Post[""] = _ => CopyPit();
		}

		object GetPits()
		{
			return PitDatabase.Entries.ToArray();
		}

		object GetPit(string id)
		{
			var pit = PitDatabase.GetPitById(id);
			if (pit == null)
				return HttpStatusCode.NotFound;

			return pit;
		}

		object GetPitConfig(string id)
		{
			var cfg = PitDatabase.GetConfigById(id);
			if (cfg == null)
				return HttpStatusCode.NotFound;

			return cfg;
		}

		object CopyPit()
		{
			var data = this.Bind<PitCopy>();
			var db = PitDatabase;
			var newUrl = "";

			try
			{
				newUrl = db.CopyPit(data.LibraryUrl, data.Pit.PitUrl, data.Pit.Name, data.Pit.Description);
			}
			catch (KeyNotFoundException)
			{
				return HttpStatusCode.NotFound;
			}
			catch (UnauthorizedAccessException)
			{
				return HttpStatusCode.Forbidden;
			}
			catch (ArgumentException)
			{
				return HttpStatusCode.BadRequest;
			}
			catch (Exception)
			{
				throw;
			}

			var newPit = db.GetPitByUrl(newUrl);

			return newPit;
		}
	}
}
