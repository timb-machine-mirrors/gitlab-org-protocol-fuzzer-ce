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
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Peach.Enterprise.WebServices
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

			var ranges = enumerable.OrderByDescending(o => o.Item2).Select(o => MediaRange.FromString(o.Item1)).ToList();
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

		public void Handle(Nancy.HttpStatusCode statusCode, NancyContext context)
		{
			var error = context.Response as ErrorResponse;

			if (error == null)
			{
				// NotFoundResponse and 404s returned from routes are not
				// contained in an ErrorResponse
				System.Diagnostics.Debug.Assert(statusCode == Nancy.HttpStatusCode.NotFound);

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

		public bool HandlesStatusCode(Nancy.HttpStatusCode statusCode, NancyContext context)
		{
			return statusCode == Nancy.HttpStatusCode.NotFound
				|| statusCode == Nancy.HttpStatusCode.InternalServerError;
		}
	}

	internal class Bootstrapper : DefaultNancyBootstrapper
	{
		WebContext context;

		public Bootstrapper(WebContext context)
		{
			this.context = context;

			// Do this here since RootNamespaces is static, and
			// ConfigureApplicationContainer can be called more than once.
			ResourceViewLocationProvider.RootNamespaces.Add(GetType().Assembly, "Peach.Enterprise.WebServices");
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

		protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
		{
			base.ConfigureRequestContainer(container, context);
		}

		protected override void ConfigureConventions(NancyConventions nancyConventions)
		{
			base.ConfigureConventions(nancyConventions);

			// Need to go before the default '/Content' handler in nancy
			nancyConventions.StaticContentsConventions.Insert(0, StaticContentConventionBuilder.AddDirectory("/", @"web"));
			nancyConventions.StaticContentsConventions.Insert(0, StaticContentConventionBuilder.AddDirectory("/docs", @"docs"));
		}

		protected override void RequestStartup(TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines, NancyContext context)
		{
			pipelines.OnError.AddItemToEndOfPipeline((ctx, ex) =>
			{
				// Wrap all exceptions in an error response
				return ErrorResponse.FromException(ex);
			});

			base.RequestStartup(container, pipelines, context);
		}

		protected override NancyInternalConfiguration InternalConfiguration
		{
			get
			{
				// Tell Nancy views are embedded resources
				return NancyInternalConfiguration.WithOverrides(c => c.ViewLocationProvider = typeof(ResourceViewLocationProvider));
			}
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
			this.StatusCode = Nancy.HttpStatusCode.InternalServerError;
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

			bootstrapper = new Bootstrapper(Context);
			config = new HostConfiguration()
			{
				UrlReservations = new UrlReservations()
				{
					CreateAutomatically = true,
				},
			};
		}

		public void Start()
		{
			Start("localhost", 8888);
		}

		public void Start(string hostname, int port)
		{
			while (host == null)
			{
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
			{
				host.Stop();
				host = null;
			}
		}

		public void Dispose()
		{
			Stop();

			config = null;
			bootstrapper = null;
			Uri = null;
			Context = null;
		}

		public static int Run(string pitLibraryPath)
		{
			using (var evt = new AutoResetEvent(false))
			{
				ConsoleCancelEventHandler handler = (s, e) => { evt.Set(); e.Cancel = true; };

				using (var svc = new WebServer(pitLibraryPath))
				{
					svc.Start();

					try
					{
						System.Diagnostics.Process.Start(svc.Uri.ToString());
					}
					catch
					{
					}

					Core.Runtime.ConsoleWatcher.WriteInfoMark();
					Console.WriteLine("Web site running at: {0}", svc.Uri);

					Peach.Core.Runtime.ConsoleWatcher.WriteInfoMark();
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
