
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
		NancyHost host = null;
		int port = 8888;

		public RestServer(Peach.Core.Engine engine)
		{
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
	}
}
