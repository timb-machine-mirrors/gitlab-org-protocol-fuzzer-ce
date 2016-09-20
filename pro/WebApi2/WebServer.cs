using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Owin;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting.Tracing;
using Peach.Core;
using Peach.Pro.Core;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.WebServices;
using Peach.Pro.WebApi2.Utility;
using Swashbuckle.Application;
using Swashbuckle.Swagger;
using Peach.Pro.Core.License;
using Autofac;
using Autofac.Integration.WebApi;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Owin.Builder;
using Nowin;

namespace Peach.Pro.WebApi2
{
	public class WebServer : BaseWebServer
	{
		public WebServer(ILicense license, string pitLibraryPath, IJobMonitor jobMonitor)
			: base(new WebStartup(
					license,
					new WebContext(pitLibraryPath),
					jobMonitor,
					ctx =>
					{
						var pitdb = new PitDatabase(license);
						if (!string.IsNullOrEmpty(pitLibraryPath))
							pitdb.Load(pitLibraryPath);
						return pitdb;
					}
				))
			{
			}
	}

	public class BaseWebServer : IWebStatus
	{
		// http://msdn.microsoft.com/en-us/library/windows/desktop/ms681382.aspx
		// ReSharper disable InconsistentNaming
		private const int ERROR_SHARING_VIOLATION = 32;
		private const int ERROR_ALREADY_EXISTS = 183;
		// ReSharper restore InconsistentNaming

		readonly IWebStartup _startup;
		IDisposable _server;

		public BaseWebServer(IWebStartup startup)
		{
			_startup = startup;
		}

		public void Start(int? port, string certPath)
		{
			if (port.HasValue)
				Start(port.Value, certPath, false);
			else
				Start(8888, certPath, true);
		}

		class NullTraceOutputFactory : ITraceOutputFactory
		{
			public TextWriter Create(string outputFile)
			{
				return StreamWriter.Null;
			}
		}

		public void Start(int port, string certPath, bool keepGoing)
		{
			while (_server == null)
			{
				try
				{
					// Owin adds a TextWriterTraceListener during startup
					// we need to replace it to avoid spewing to console

					//var options = new StartOptions(url);
					//options.Settings.Add(
					//	typeof(ITraceOutputFactory).FullName,
					//	typeof(NullTraceOutputFactory).AssemblyQualifiedName
					//);
					//_server = WebApp.Start(options, _startup.OnStartup);

					X509Certificate2 cert = null;
					if (certPath != null)
						cert = new X509Certificate2(certPath);

					var appBuilder = new AppBuilder();
					OwinServerFactory.Initialize(appBuilder.Properties);
					_startup.OnStartup(appBuilder);
					_server = ServerBuilder.New()
						.SetAddress(IPAddress.Any)
						.SetPort(port)
						.SetOwinApp(appBuilder.Build())
						.SetCertificate(cert)
						.Start();

					Uri = new UriBuilder()
					{
						Scheme = cert == null ? "http" : "https",
						Host = GetLocalIp(),
						Port = port
					}.Uri;
				}
				catch (Exception ex)
				{
					var inner = ex.GetBaseException();

					var lex = inner as HttpListenerException;
					if (lex != null)
					{
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
						continue;
					}

					throw new PeachException("Unable to start the web server: " + inner.Message + ".", ex);
				}
			}
		}

		public Uri Uri { get; private set; }

		public void Dispose()
		{
			if (_startup != null)
				_startup.Dispose();

			if (_server != null)
				_server.Dispose();
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

	public interface IWebStartup : IDisposable
	{
		void OnStartup(IAppBuilder app);
	}

	public class WebStartup : IWebStartup
	{
		readonly ILicense _license;
		readonly IWebContext _context;
		readonly IJobMonitor _jobMonitor;
		readonly Func<IComponentContext, IPitDatabase> _pitDatabaseFactory;

		public WebStartup(
			ILicense license, 
			IWebContext context,
			IJobMonitor jobMonitor,
			Func<IComponentContext, IPitDatabase> pitDatabaseFactory)
		{
			_license = license;
			_context = context;
			_jobMonitor = jobMonitor;
			_pitDatabaseFactory = pitDatabaseFactory;
		}

		public void OnStartup(IAppBuilder app)
		{
			var cfg = new HttpConfiguration();

			cfg.Formatters.JsonFormatter.SerializerSettings = JsonUtilities.GetSettings();

			cfg.MapHttpAttributeRoutes();

			cfg.EnableSwagger(c =>
			{
				c.SingleApiVersion("v1", "Peach Fuzzer API")
					.Description("The REST API used for controlling the fuzzer.")
					.TermsOfService("End User License Agreement")
					.Contact(cc => cc
						.Name("Peach Fuzzer")
						.Url("http://www.peachfuzzer.com/contact/")
						.Email("support@peachfuzzer.com"))
					.License(lc => lc
						.Name("EULA")
						.Url("http://www.peachfuzzer.com/contact/eula/"));

				c.DescribeAllEnumsAsStrings(true);
				c.IncludeXmlComments(Utilities.GetAppResourcePath("Peach.Pro.xml"));
				c.IncludeXmlComments(Utilities.GetAppResourcePath("Peach.Pro.WebApi2.xml"));
				c.OperationFilter<CommonResponseFilter>();
				c.SchemaFilter<RequiredParameterFilter>();
				c.MapType<TimeSpan>(() => new Schema { type = "integer", format = "int64" });
			}).EnableSwaggerUi(c =>
			{
				// Prevent "Error" badge from showing up when the UI tries to
				// GET http://online.swagger.io/validator?url=http://localhost:8888/swagger/docs/v1
				c.DisableValidator();
			});

			var builder = new ContainerBuilder();

			builder.RegisterInstance(_context).As<IWebContext>();
			builder.RegisterInstance(_license).As<ILicense>();
			builder.RegisterInstance(_jobMonitor).As<IJobMonitor>();
			builder.Register(_pitDatabaseFactory).As<IPitDatabase>();

			builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
			builder.RegisterWebApiFilterProvider(cfg);

			var container = builder.Build();
			cfg.DependencyResolver = new AutofacWebApiDependencyResolver(container);

			app.UseAutofacMiddleware(container);
			app.UseAutofacWebApi(cfg);
			app.UseWebApi(cfg);

			// We don't need to do any favicon.ico specific stuff.
			// It will properly get served off disk as static content.

			AddStaticContent(app, "", "public");

			AddStaticContent(app, "/docs/user", "docs/webhelp");
			AddStaticContent(app, "/docs/dev", "sdk/docs/webhelp");
		}

		private static void AddStaticContent(IAppBuilder app, string requestPath, string fileSystem)
		{
			var fullPath = Path.Combine(Utilities.ExecutionDirectory, fileSystem);

			if (!Directory.Exists(fullPath))
				return;

			var opts = new FileServerOptions
			{
				RequestPath = new PathString(requestPath),
				FileSystem = new PhysicalFileSystem(fullPath),
				EnableDefaultFiles = true,
				EnableDirectoryBrowsing = false,
			};

			// We want to tell the client to always revalidate all static assets.
			// This should work now that ETags are being properly generated.
			// The client should get a 304 if nothing has changed, otherwise they get new content.
			// This prevents stale assets from being invalidated when new server content is available,
			// like when a new version of Peach Fuzzer is installed.

			opts.StaticFileOptions.OnPrepareResponse = ctx =>
			{
				ctx.OwinContext.Response.Headers.Add("Cache-Control", new[] { "no-cache" });
			};

			app.UseFileServer(opts);
		}

		public void Dispose()
		{
			if (_jobMonitor != null)
				_jobMonitor.Dispose();
		}
	}
}
