using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;

namespace Peach.Pro.Test.WebProxy
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var proxy = new ProxyServer
			{
				RootCertificateIssuerName = "PF",
				RootCertificateName = "Peach CA"
			};

			proxy.Logger.TraceSource.Listeners.Clear();
			proxy.Logger.TraceSource.Listeners.Add(new ConsoleTraceListener());
			proxy.Logger.TraceSource.Switch.Level = SourceLevels.All;

			if (!File.Exists("ca-cert.pem") || !File.Exists("ca-key.pem") || !File.Exists("server-key.pem"))
			{
				proxy.Start();

				File.WriteAllText("ca-cert.pem", proxy.CaCert);
				File.WriteAllText("ca-key.pem", proxy.CaKey);
				File.WriteAllText("server-key.pem", proxy.ServerKey);

				proxy.Stop();

				Console.WriteLine("Please add 'ca-cert.pem' to the trusted root store.");
				Console.WriteLine("On Windows, this can be done by running:");
				Console.WriteLine("  certutil -addstore -f -enterprise Root \"{0}{1}ca-cert.pem\"",
					Environment.CurrentDirectory, Path.DirectorySeparatorChar);

				Console.WriteLine();
			}
			else
			{
				proxy.CaCert = File.ReadAllText("ca-cert.pem");
				proxy.CaKey = File.ReadAllText("ca-key.pem");
				proxy.ServerKey = File.ReadAllText("server-key.pem");
			}

			var ep = new ExplicitProxyEndPoint(IPAddress.Any, 8008);

			proxy.AddEndPoint(ep);

			try
			{
				proxy.Start();
				proxy.SetAsSystemHttpsProxy(ep);

				Console.WriteLine("Proxy is running at 127.0.0.1:{0}", proxy.ProxyEndPoints[0].Port);
				Console.WriteLine("Press any key to exit...");

				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				proxy.DisableAllSystemProxies();
			}
		}
	}
}

