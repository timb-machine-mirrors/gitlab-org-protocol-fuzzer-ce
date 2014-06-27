using Nancy;
using Nancy.Responses;
using System;

namespace Peach.Enterprise.WebServices
{
	public class IndexService : NancyModule
	{
		public IndexService()
			: base("")
		{
			// Redirect / to /app/
			Get["/"] = _ => { return Response.AsRedirect("app/"); };

			// Return index.html for /app/
			Get["/app/"] = _ => View["app/index"];

			// Return index.html for /doc/
			Get["/doc/"] = _ => View["doc/index"];
		}
	}
}
