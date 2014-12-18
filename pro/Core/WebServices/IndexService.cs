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
			Get["/"] = _ =>
			{
				var response = new GenericFileResponse("public/index.html");
				response.Headers.Add("Cache-Control", "no-cache, must-revalidate");
				return response;
			};
			Get["/docs"] = _ => Response.AsRedirect("/docs/");
		}
	}
}
