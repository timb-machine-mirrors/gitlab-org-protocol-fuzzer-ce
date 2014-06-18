using Nancy;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Enterprise.WebServices
{
	public class LibraryService : NancyModule
	{
		public static readonly string Prefix = "/p/libraries";

		public LibraryService()
			: base(Prefix)
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
			var lib = db.GetLibraryById(id);
			if (lib == null)
				return HttpStatusCode.NotFound;

			return lib;
		}
	}
}
