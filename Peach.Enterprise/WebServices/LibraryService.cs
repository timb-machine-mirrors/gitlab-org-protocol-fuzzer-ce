using Nancy;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Enterprise.WebServices
{
	public class LibraryService : NancyModule
	{
		public LibraryService()
			: base("/p/libraries")
		{
			Get[""] = _ => GetLibraries();
			Get["/{id}"] = _ => GetLibrary(_.id);
		}

		object GetLibraries()
		{
			var db = new PitDatabase(".");
			return db.Libraries.ToArray();
		}

		object GetLibrary(string id)
		{
			var db = new PitDatabase(".");
			var lib = db.GetLibrary(id);
			if (lib == null)
				return HttpStatusCode.NotFound;

			return lib;
		}
	}
}
