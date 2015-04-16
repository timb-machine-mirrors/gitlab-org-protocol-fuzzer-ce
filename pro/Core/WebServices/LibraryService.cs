using System.Linq;
using Nancy;

namespace Peach.Pro.Core.WebServices
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

		Response GetLibraries()
		{
			return Response.AsJson(PitDatabase.Libraries.ToArray());
		}

		Response GetLibrary(string id)
		{
			var lib = PitDatabase.GetLibraryById(id);
			if (lib == null)
				return HttpStatusCode.NotFound;

			return Response.AsJson(lib);
		}
	}
}
