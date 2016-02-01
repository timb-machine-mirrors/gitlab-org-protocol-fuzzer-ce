using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
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
using Swashbuckle.Swagger;

namespace Peach.Pro.WebApi2
{
	public class WebServer : IWebStatus
	{
		// http://msdn.microsoft.com/en-us/library/windows/desktop/ms681382.aspx
		// ReSharper disable InconsistentNaming
		private const int ERROR_ACCESS_DENIED = 5;
		private const int ERROR_SHARING_VIOLATION = 32;
		private const int ERROR_ALREADY_EXISTS = 183;
		// ReSharper restore InconsistentNaming

		readonly WebContext _context;
		IDisposable _server;

		public WebServer(string pitLibraryPath, IJobMonitor jobMonitor)
		{
			_context = new WebContext(pitLibraryPath, jobMonitor);
		}

		public void Start(int? port)
		{
			if (port.HasValue)
				Start(port.Value, false);
			else
				Start(8888, true);
		}

		public void Start(int port, bool keepGoing)
		{
			var added = false;

			while (_server == null)
			{
				var url = "http://+:{0}/".Fmt(port);

				try
				{
					_server = WebApp.Start(url, OnStartup);

					Uri = new Uri("http://{0}:{1}/".Fmt(GetLocalIp(), port));
				}
				catch (Exception ex)
				{
					var inner = ex.GetBaseException();

					var lex = inner as HttpListenerException;
					if (lex != null)
					{
						if (lex.ErrorCode == ERROR_ACCESS_DENIED)
						{
							var error = added;

							if (!added)
								error = !UacHelpers.AddUrl(url);

							if (!error)
							{
								// UAC reservation added, don't increment port
								added = true;
								continue;
							}

							var sb = new StringBuilder();

							sb.AppendFormat("Access was denied when starts the web server at url '{0}'.", url);
							sb.AppendLine();
							sb.AppendLine();
							sb.AppendLine("Please create the url reservations by executing the following");
							sb.AppendLine("from a command prompt with elevated privileges:");
							sb.AppendFormat("{0} {1}", UacHelpers.Command, UacHelpers.GetArguments(url));

							throw new PeachException(sb.ToString(), ex);
						}

						// Windows gives ERROR_SHARING_VIOLATION when port in use
						// Windows gives ERROR_ALREADY_EXISTS when two http instances are running
						// Mono raises "Prefix already in use" message
						if (lex.ErrorCode == ERROR_SHARING_VIOLATION ||
							lex.ErrorCode == ERROR_ALREADY_EXISTS ||
							lex.Message == "Prefix already in use.")
						{
							if (!keepGoing)
								throw new PeachException("Unable to start the web server at http://localhost:{0}/ because the port is currently in use.".Fmt(port));

							// Try the next port
							++port;
							added = false;
							continue;
						}
					}

					// Mono gives AddressAlreadyInUse when port in use
					var sex = inner as SocketException;
					if (sex != null && sex.SocketErrorCode == SocketError.AddressAlreadyInUse)
					{
						if (!keepGoing)
							throw new PeachException("Unable to start the web server at http://localhost:{0}/ because the port is currently in use.".Fmt(port));

						// Try the next port
						++port;
						added = false;
						continue;
					}

					throw new PeachException("Unable to start the web server: " + inner.Message + ".", ex);
				}
			}
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
				c.MapType<TimeSpan>(() => new Schema { type = "integer", format = "int64" });

			}).EnableSwaggerUi();

			app.UseWebApi(cfg);

			// We don't need to do any favicon.ico specific stuff.
			// It will properly get served off disk as static content.

			AddStaticContent(app, "", "public");

			AddStaticContent(app, "/docs", "docs/webhelp");

			// TODO: Replace this with dependency injection
			cfg.Properties["WebContext"] = _context;

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

		private static string GetLocalIp()
		{
			try
			{
				using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
				{
					s.Connect(new IPAddress(0x01010101), 1);
					
					return ((IPEndPoint)s.LocalEndPoint).Address.ToString();
				}
			}
			catch
			{
				return "localhost";
			}
		}
	}
}
