using System;
using System.IO;
using Owin;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.FileSystems;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Peach.Core;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.WebServices;
using Peach.Pro.WebApi2.Utility;
using Swashbuckle.Application;

namespace Peach.Pro.WebApi2
{
	public class WebServer : IWebStatus
	{
		readonly WebContext _context;
		IDisposable _server;

		public WebServer(string pitLibraryPath, IJobMonitor jobMonitor)
		{
			_context = new WebContext(pitLibraryPath, jobMonitor);
		}

		public void Start()
		{
			// TODO: Handle non-admin trying to start the listener

			// Mirror Nancy's self host code that tries to start the web listener
			// and if it gets an access denied error, runs the following as admin:
			// netsh http add urlacl url=\"{0}\" user=\"{1}\"

			// TODO: Handle port in use and increment until first available port is found

			// TODO: Ensure mono uses '*' and windows uses '+' for any prefix

			// On mono, the HttpListener tries to resolve "+" which makes
			// startup/shutdown extremly slow.

			// Mono tries to resolve any string that is not a parsable IP or '*'.
			// In order to receive requests on all interfaces with mono the
			// prefix needs to be '*'.

			_server = WebApp.Start("http://+:8888/", OnStartup);

			Uri = new Uri("http://localhost:8888");
		}

		public Uri Uri
		{
			get;
			private set;
		}

		public void Dispose()
		{
			if (_server != null)
				_server.Dispose();

			if (_context != null)
				_context.Dispose();
		}

		private void OnStartup(IAppBuilder app)
		{
			var cfg = new HttpConfiguration();

			var json = cfg.Formatters.JsonFormatter.SerializerSettings;

			json.ContractResolver = new CamelCasePropertyNamesContractResolver
			{
				IgnoreSerializableAttribute = true
			};

			json.NullValueHandling = NullValueHandling.Ignore;
			json.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
			json.DateTimeZoneHandling = DateTimeZoneHandling.Utc;

			json.Converters.Insert(0, new StringEnumConverter { CamelCaseText = true });
			json.Converters.Insert(0, new TimeSpanJsonConverter());

			cfg.MapHttpAttributeRoutes();

			cfg.EnableSwagger(c =>
			{
				c.SingleApiVersion("v1", "A title for your API")
					.Description("A sample API for testing and prototyping Swashbuckle features")
					.TermsOfService("Some terms")
					.Contact(cc => cc
						.Name("Some contact")
						.Url("http://tempuri.org/contact")
						.Email("some.contact@tempuri.org"))
					.License(lc => lc
						.Name("Some License")
						.Url("http://tempuri.org/license"));

				c.DescribeAllEnumsAsStrings(true);
				c.IncludeXmlComments(Utilities.GetAppResourcePath("Peach.Pro.xml"));
				c.IncludeXmlComments(Utilities.GetAppResourcePath("Peach.Pro.WebApi2.xml"));
				c.OperationFilter<CommonResponseFilter>();
				c.SchemaFilter<RequiredParameterFilter>();

			}).EnableSwaggerUi();

			app.UseWebApi(cfg);

			AddStaticContent(app, "", "public");

			AddStaticContent(app, "/docs", "docs/webhelp");

			// TODO: Replace this with dependency injection
			cfg.Properties["WebContext"] = _context;

			// TODO: Ensure favicon.ico works
			// TODO: Ensure Response.ContentType = "text/html; charset=utf8"
			// TODO: Implelemt Response.AsZip()
			// TODO: Implelemt Response.AsFile()

			// TODO: For REST responses, veryfy caching.  With NancyFX we needed to:
			// TODO: Ensure Response.Headers["Cache-Control"] = "no-cache";
			// TODO: Ensure Response.Headers["Pragma"] = "no-cache";

			// TODO: Do we need to redirect / to /{version}/ to fix caching issues still?
			// TODO: For /version/index.html response, verify caching. With NancyFX we needed to:
			// TODO: Ensure Response.Headers["Cache-Control"] = "no-cache, must-revalidate";
		}

		private static void AddStaticContent(IAppBuilder app, string requestPath, string fileSystem)
		{
			var fullPath = Path.Combine(Utilities.ExecutionDirectory, fileSystem);

			if (!Directory.Exists(fullPath))
				return;

			app.UseFileServer(new FileServerOptions
			{
				RequestPath = new PathString(requestPath),
				FileSystem = new PhysicalFileSystem(fullPath),
				EnableDefaultFiles = true,
				EnableDirectoryBrowsing = false,
			});
		}
	}
}
