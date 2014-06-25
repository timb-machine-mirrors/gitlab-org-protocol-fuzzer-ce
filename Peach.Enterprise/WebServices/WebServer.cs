using Nancy;
using Nancy.Conventions;
using Nancy.Hosting.Self;
using Nancy.Serialization.JsonNet;
using Nancy.TinyIoc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
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

	internal class Bootstrapper : DefaultNancyBootstrapper
	{
		WebContext context;

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
		}

		protected override void ApplicationStartup(TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines)
		{
			base.ApplicationStartup(container, pipelines);

			this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
			{
				return string.Concat("web/", viewName);
			}); 
		}
	}

	public class WebServer : IDisposable
	{
		Bootstrapper bootstrapper;
		HostConfiguration config;
		NancyHost host;

		public Uri Uri { get; private set; }
		public WebContext Context { get; private set; }

		public WebServer(string pitLibraryPath)
		{
			Uri = new Uri("http://localhost:8888");
			Context = new WebContext(pitLibraryPath);

			bootstrapper = new Bootstrapper(Context);
			config = new HostConfiguration()
			{
				UrlReservations = new UrlReservations()
				{
					CreateAutomatically = true,
				},
			};
			host = new NancyHost(bootstrapper, config, Uri);
		}

		public void Start(string[] args)
		{
			host.Start();
		}

		public void Stop()
		{
			host.Stop();
		}

		public void Dispose()
		{
			host = null;
			config = null;
			bootstrapper = null;
			Uri = null;
			Context = null;
		}

		public static int Run(string[] args)
		{
			using (var evt = new AutoResetEvent(false))
			{
				ConsoleCancelEventHandler handler = (s, e) => { evt.Set(); e.Cancel = true; };

				using (var svc = new WebServer("."))
				{
					svc.Start(args);

					try
					{
						System.Diagnostics.Process.Start(svc.Uri.ToString());
					}
					catch
					{
					}

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
