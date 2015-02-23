using System.Diagnostics;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.ErrorHandling;
using Nancy.Hosting.Self;
using Nancy.Responses;
using Nancy.Responses.Negotiation;
using Nancy.Serialization.JsonNet;
using Nancy.TinyIoc;
using Nancy.ViewEngines;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Peach.Core;
using Peach.Pro.Core.Runtime;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using HttpStatusCode = Nancy.HttpStatusCode;

namespace Peach.Pro.Core.WebServices
{
	internal class CustomJsonSerializer : JsonSerializer
	{
		public CustomJsonSerializer()
		{
			ContractResolver = new CamelCasePropertyNamesContractResolver();
			NullValueHandling = NullValueHandling.Ignore;
		}
	}

	internal class CamelCaseStringEnumConverter : StringEnumConverter
	{
		public CamelCaseStringEnumConverter()
		{
			CamelCaseText = true;
		}
	}

	public class ErrorStatusCodeHandler : DefaultViewRenderer, IStatusCodeHandler
	{
		static bool IsHtml(NancyContext context)
		{
			var enumerable = context.Request.Headers.Accept;

			var ranges = enumerable.OrderByDescending(o => o.Item2).Select(o => new MediaRange(o.Item1)).ToList();
			foreach (var item in ranges)
			{
				if (item.Matches("application/json"))
					return false;
				if (item.Matches("text/json"))
					return false;
				if (item.Matches("text/html"))
					return true;
			}

			return true;
		}

		public ErrorStatusCodeHandler(IViewFactory factory)
			: base(factory)
		{
		}

		public void Handle(HttpStatusCode statusCode, NancyContext context)
		{
			var error = context.Response as ErrorResponse;

			if (error == null)
			{
				// NotFoundResponse and 404s returned from routes are not
				// contained in an ErrorResponse
				Debug.Assert(statusCode == HttpStatusCode.NotFound);

				error = ErrorResponse.FromMessage("The resource you have requested cannot be found.");
				error.StatusCode = statusCode;

				// Ensure the Response is an ErrorResponse
				context.Response = error;
			}

			if (IsHtml(context))
			{
				// Render the HTML error view
				context.Response = RenderView(context, "Error", error).WithStatusCode(statusCode);
			}
		}

		public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
		{
			return statusCode == HttpStatusCode.NotFound
				|| statusCode == HttpStatusCode.InternalServerError;
		}
	}

	internal class Bootstrapper : DefaultNancyBootstrapper
	{
		WebContext context;

		static Bootstrapper()
		{
			// Do this here since RootNamespaces is static, and
			// ConfigureApplicationContainer can be called more than once.
			ResourceViewLocationProvider.RootNamespaces.Add(Assembly.GetExecutingAssembly(), "Peach.Pro.Core.WebServices.Views");
		}

		public Bootstrapper(WebContext context)
		{
			this.context = context;
		}

		protected override void ConfigureApplicationContainer(TinyIoCContainer container)
		{
			container.Register<JsonSerializer, CustomJsonSerializer>();
			container.Register<JsonNetSerializer>();
			container.Register<WebContext>(context);
			container.Register<PitService>();
			container.Register<LibraryService>();
			container.Register<NodeService>();
			container.Register<JobService>();
			container.Register<FaultService>();
			container.Register<WizardService>();
			container.Register<IndexService>();
			container.Register<ErrorStatusCodeHandler>();
			container.Register<ResourceViewLocationProvider>();
		}

		protected override void ConfigureConventions(NancyConventions nancyConventions)
		{
			base.ConfigureConventions(nancyConventions);

			// Need to go before the default '/Content' handler in nancy
			nancyConventions.StaticContentsConventions.Insert(0,
				StaticContentConventionBuilder.AddDirectory("/", @"public")
			);
			nancyConventions.StaticContentsConventions.Insert(0,
				StaticContentConventionBuilder.AddDirectory("/docs", @"docs/webhelp")
			);
		}

		protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
		{
			// NOTE: The pipelies do not get called when serving static content.
			// Need to investigate disabling this if we want the back end to do
			// global redirects to the EULA.
			// https://github.com/NancyFx/Nancy/pull/982

			base.RequestStartup(container, pipelines, context);

			// Enable static content
			StaticContent.Enable(pipelines);

			// Ensure these get insterted after all default handlers
			pipelines.BeforeRequest.AddItemToStartOfPipeline((ctx) =>
			{
				if (!License.EulaAccepted)
				{
					if (ctx.Request.Path == "/favicon.ico")
						return null;

					if (ctx.Request.Path != "/eula")
						return new RedirectResponse("/eula");
				}

				return null;
			});

			// Wrap all exceptions in an error response
			pipelines.OnError.AddItemToEndOfPipeline((ctx, ex) => ErrorResponse.FromException(ex));

			// Make default be utf-8
			pipelines.AfterRequest.AddItemToEndOfPipeline(ctx =>
			{
				if (ctx.Response.ContentType == "text/html")
					ctx.Response.ContentType = "text/html; charset=utf8";
			});
		}

		protected override NancyInternalConfiguration InternalConfiguration
		{
			get
			{
				// Allow overriding default nancy configuration
				return NancyInternalConfiguration.WithOverrides(OnConfigurationBuilder);
			}
		}

		void OnConfigurationBuilder(NancyInternalConfiguration c)
		{
			// Tell Nancy views are embedded resources
			c.ViewLocationProvider = typeof(ResourceViewLocationProvider);

			// Tell nancy to send all static content thru the request pipeline
			c.StaticContentProvider = typeof(DisabledStaticContentProvider);
		}
	}

	internal class ErrorResponse : JsonResponse
	{
		private class Error
		{
			public string ErrorMessage { get; set; }
			public string FullException { get; set; }
		}

		private Error error;

		private ErrorResponse(Error error)
			: base(error, new JsonNetSerializer(new CustomJsonSerializer()))
		{
			this.error = error;
			StatusCode = HttpStatusCode.InternalServerError;
		}

		public string ErrorMessage
		{
			get
			{
				return error.ErrorMessage;
			}
		}

		public string FullException
		{
			get
			{
				return error.FullException;
			}
		}

		public bool HasDetails
		{
			get
			{
				return !string.IsNullOrEmpty(FullException);
			}
		}

		public static ErrorResponse FromMessage(string message)
		{
			return new ErrorResponse(new Error()
			{
				ErrorMessage = message,
			});
		}

		public static ErrorResponse FromException(Exception ex)
		{
			return new ErrorResponse(new Error()
			{
				ErrorMessage = ex.GetBaseException().Message,
				FullException = ex.ToString(),
			});
		}
	}

	public class WebServer : IDisposable
	{
		// http://msdn.microsoft.com/en-us/library/windows/desktop/ms681382.aspx
		const int ERROR_SHARING_VIOLATION = 32;
		const int ERROR_ALREADY_EXISTS = 183;

		Bootstrapper bootstrapper;
		HostConfiguration config;
		NancyHost host;

		public Uri Uri { get; private set; }
		public WebContext Context { get; private set; }

		public WebServer(string pitLibraryPath)
		{
			Context = new WebContext(pitLibraryPath);
		}

		public void Start()
		{
			Start("localhost", 8888);
		}

		public void Start(string hostname, int port)
		{
			while (host == null)
			{
				// Need to make a new Bootstrapper every time.
				// If tmoHost.Start() fails, we don't want to
				// reinitialize an already initialized bootstrapper!

				bootstrapper = new Bootstrapper(Context);
				config = new HostConfiguration()
				{
					UrlReservations = new UrlReservations()
					{
						CreateAutomatically = true,
					},
				};

				try
				{
					var tmpUri = new Uri(string.Format("http://{0}:{1}", hostname, port++));
					var tmpHost = new NancyHost(bootstrapper, config, tmpUri);

					tmpHost.Start();

					Uri = tmpUri;
					host = tmpHost;
				}
				catch (HttpListenerException ex)
				{
					// Windows gives ERROR_SHARING_VIOLATION when port in use
					// Windows gives ERROR_ALREADY_EXISTS when two http instances are running
					// Mono raises "Prefix already in use" message
					if (ex.ErrorCode != ERROR_SHARING_VIOLATION && ex.ErrorCode != ERROR_ALREADY_EXISTS && ex.Message != "Prefix already in use.")
						throw;
				}
				catch (SocketException ex)
				{
					// Mono gives AddressAlreadyInUse
					if (ex.SocketErrorCode != SocketError.AddressAlreadyInUse)
						throw;
				}
			}
		}

		public void Stop()
		{
			if (host != null)
				host.Stop();

			host = null;
			config = null;
			bootstrapper = null;
			Uri = null;
		}

		public void Dispose()
		{
			Stop();

			Context = null;
		}

		public static int Run(string pitLibraryPath, bool shouldStartBrowser)
		{
			using (var evt = new AutoResetEvent(false))
			{
				ConsoleCancelEventHandler handler = (s, e) => { evt.Set(); e.Cancel = true; };

				using (var svc = new WebServer(pitLibraryPath))
				{
					svc.Start();

					try
					{
						if (!Debugger.IsAttached && shouldStartBrowser)
						{
							Process.Start(svc.Uri.ToString());
						}
					}
					catch
					{
					}

					ConsoleWatcher.WriteInfoMark();
					Console.WriteLine("Web site running at: {0}", svc.Uri);

					ConsoleWatcher.WriteInfoMark();
					Console.WriteLine("Press Ctrl-C to exit.");

					try
					{
						Console.CancelKeyPress += handler;
						evt.WaitOne();
					}
					finally
					{
						Console.CancelKeyPress -= handler;
					}

					svc.Stop();

					return 0;
				}
			}
		}
	}
}
