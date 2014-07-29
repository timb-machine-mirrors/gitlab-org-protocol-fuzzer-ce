using Nancy;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Enterprise.WebServices
{
	public class LibraryService : WebService
	{
		public static readonly string Prefix = "/p/libraries";

		public LibraryService(WebContext context)
			: base(context, Prefix)
		{
			Get[""] = _ => GetLibraries();
			Get["/{id}"] = _ => GetLibrary(_.id);
		}

		object GetLibraries()
		{
			return PitDatabase.Libraries.ToArray();
		}

		object GetLibrary(string id)
		{
			var lib = PitDatabase.GetLibraryById(id);
			if (lib == null)
				return HttpStatusCode.NotFound;

			return lib;
		}
	}
}
