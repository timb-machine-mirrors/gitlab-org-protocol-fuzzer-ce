using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using Peach.Core.Test;
using Peach.Pro.Core.WebServices;
using Peach.Pro.WebApi;

namespace Peach.Pro.Test.WebApi
{
	[TestFixture]
	[Quick]
	class WebServerTests
	{
		[Test]
		public void MultipleServers()
		{
			var listener = new TcpListener(IPAddress.Any, 0);

			try
			{
				listener.Start();
				var port = ((IPEndPoint)listener.LocalEndpoint).Port;
				Assert.AreNotEqual(0, port);

				using (var web = new WebServer("", new InternalJobMonitor()))
				{
					web.Start("localhost", port);

					var actualPort = web.Uri.Port;
					Assert.Greater(actualPort, port);

					using (var web2 = new WebServer("", new InternalJobMonitor()))
					{
						web2.Start("localhost", actualPort);
						Assert.Greater(web2.Uri.Port, actualPort);
					}
				}
			}
			finally
			{
				listener.Stop();
			}
		}
	}
}
