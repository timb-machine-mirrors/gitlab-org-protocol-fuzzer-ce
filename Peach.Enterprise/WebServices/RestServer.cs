
using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Net.NetworkInformation;

using Nancy;
using Nancy.TinyIoc;
using Nancy.Bootstrapper;
using Nancy.Hosting.Self;
using Nancy.Diagnostics;
using Nancy.Conventions;

namespace Peach.Enterprise.WebServices
{
	public class RestServer
	{
		NancyHost host = null;
		int port = 8888;

		public RestServer(Peach.Core.Engine engine)
		{
			// Lets store faults here.
			// TODO: This needs to be moved someplace better later.
			engine.context.stateStore["Peach.Faults"] = new List<Peach.Core.Fault>();
			engine.context.stateStore["Peach.StartTime"] = DateTime.Now;
			engine.context.stateStore["Peach.Rest.Faults"] = new List<Peach.Enterprise.WebServices.Fault>();

			// Collect fault information
			// TODO: This should happen someplace else I think :)
			RestService.Initialize(engine);

			host = new NancyHost(
				new PeachBootstrapper(engine),
				new HostConfiguration()
				{
					UrlReservations = new UrlReservations()
					{
						CreateAutomatically = true,
					},
				},
				new Uri("http://localhost:" + port.ToString())
			);
		}

		public static int findNextAvailablePort(int port)
		{
			while (!isPortAvailable(port))
				port++;

			return port;
		}

		public static bool isPortAvailable(int port)
		{
			bool isAvailable = true;

			// Evaluate current system tcp connections. This is the same information provided
			// by the netstat command line application, just in .Net strongly-typed object
			// form.  We will look through the list, and if our port we would like to use
			// in our TcpClient is occupied, we will set isAvailable to false.
			IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
			TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

			foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
			{
				if (tcpi.LocalEndPoint.Port == port)
				{
					isAvailable = false;
					break;
				}
			}

			return isAvailable;
		}

		~RestServer()
		{
			if(host != null)
				host.Stop();
		}

		public void Start()
		{
			host.Start();
		}

		public void Stop()
		{
			host.Stop();
		}

	}

	public class PeachBootstrapper : DefaultNancyBootstrapper
	{
		Peach.Core.Engine engine = null;

		public PeachBootstrapper(Peach.Core.Engine engine)
			: base()
		{
			this.engine = engine;
		}

		protected override void ApplicationStartup(TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines)
		{
			this.Conventions.ViewLocationConventions.Add((viewName, model, context) =>
			{
				return string.Concat("web/", viewName);
			});
		}

		protected override void ConfigureApplicationContainer(TinyIoCContainer container)
		{
			container.Register<Peach.Core.Engine>(this.engine);
			container.Register<Nancy.Serialization.JsonNet.JsonNetSerializer>();
			container.Register<RestService>();
		}

		protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
		{
			base.ConfigureRequestContainer(container, context);
		}

		/*
		protected override void ConfigureConventions(NancyConventions nancyConventions)
		{
			nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("web", @"web"));
			base.ConfigureConventions(nancyConventions);
		}*/

		protected override void ConfigureConventions(NancyConventions nancyConventions)
		{
			//hacky only supports jobs/1/...
			nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("/p/jobs/1/visualizer", "/web/visualizer"));
			nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("/p/jobs/1", "/web/visualizer"));

			nancyConventions.StaticContentsConventions.Add(
				StaticContentConventionBuilder.AddDirectory("/", @"web"));

			base.ConfigureConventions(nancyConventions);
		}
	}
}
