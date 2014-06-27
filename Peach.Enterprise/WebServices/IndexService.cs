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
			// Redirects
			Get["/"] = _ => { return Response.AsRedirect("/app/index.html"); };
			Get["/app"] = _ => { return Response.AsRedirect("/app/index.html"); };
			Get["/docs"] = _ => { return Response.AsRedirect("/docs/index.html"); };
		}
	}
}
