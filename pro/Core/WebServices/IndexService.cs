using Nancy;
using Nancy.Responses;

namespace Peach.Pro.Core.WebServices
{
	public class IndexService : NancyModule
	{
		public IndexService()
			: base("")
		{
			// Default Views
			Get["/"] = _ => new GenericFileResponse("public/index.html");
			Get["/docs"] = _ => Response.AsRedirect("/docs/");
		}
	}
}
