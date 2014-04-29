
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

namespace Peach.Enterprise.WebServices
{
	public class RestServer
	{
		NancyHost _host = null;
		public int port = 8888;
		Peach.Core.Engine _engine { get; set; }

		public RestServer(Peach.Core.Engine engine)
		{
			_engine = engine;
			port = findNextAvailablePort(8888);
			_host = new NancyHost(new PeachBootstrapper(engine), new Uri("http://localhost:" + port));
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
			if(_host != null)
				_host.Stop();
		}

		public void Start()
		{
			_host.Start();
		}

		public void Stop()
		{
			_host.Stop();
		}
	}

	public class PeachBootstrapper : DefaultNancyBootstrapper
	{
		Peach.Core.Engine _engine = null;
		RestService _service { get; set; }

		public PeachBootstrapper(Peach.Core.Engine engine) : base()
		{
			_engine = engine;
			_service = new RestService();
			RestService.Initialize(_engine);
		}

		protected override void ConfigureApplicationContainer(TinyIoCContainer container)
		{
			container.Register(typeof(Nancy.Serialization.JsonNet.JsonNetSerializer));
			container.Register<RestService>();
		}

		protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
		{
			//container.Register<RestService>(_service);

			//// Get our session manager - this will "bubble up" to the parent container
			//// and get our application scope singleton
			//var session = container.Resolve<IRavenSessionManager>().GetSession();

			//// We can put this in context.items and it will be disposed when the request ends
			//// assuming it implements IDisposable.
			//context.Items["RavenSession"] = session;

			//// Just guessing what this type is called
			//container.Register<IRavenSession>(session);

			//container.Register<ISearchRepository, SearchRepository>();
			//container.Register<IResponseFactory, ResponseFactory>();
		}
	}

}
