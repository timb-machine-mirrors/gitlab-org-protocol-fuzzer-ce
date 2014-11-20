using Nancy;

namespace Peach.Pro.Core.WebServices
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
