using Nancy;
using Nancy.Responses;
using System.Reflection;
using Peach.Core;

namespace Peach.Pro.Core.WebServices
{
	public class IndexService : NancyModule
	{
		public IndexService()
			: base("")
		{
			var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			var versionRoot = "/{0}/".Fmt(version);

			// Default Views
			Get["/"] = _ =>
			{
				return Response.AsRedirect(versionRoot);
			};

			Get[versionRoot] = _ =>
			{
				var response = new GenericFileResponse("public/index.html");
				response.Headers.Add("Cache-Control", "no-cache, must-revalidate");
				return response;
			};
			Get["/docs"] = _ => Response.AsRedirect("/docs/index.html");
		}
	}
}
