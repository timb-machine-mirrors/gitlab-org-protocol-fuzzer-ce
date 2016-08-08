using System;
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

			if (!File.Exists("ca-cert.pem") || !File.Exists("ca-key.pem"))
			{
				proxy.Start();

				File.WriteAllText("ca-cert.pem", proxy.CaCert);
				File.WriteAllText("ca-key.pem", proxy.CaKey);

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
			}

			proxy.AddEndPoint(new ExplicitProxyEndPoint(IPAddress.Any, 8008));
			proxy.Start();

			Console.WriteLine("Proxy is running at 127.0.0.1:{0}", proxy.ProxyEndPoints[0].Port);
			Console.WriteLine("Press any key to exit...");

			Console.ReadLine();
		}
	}
}

