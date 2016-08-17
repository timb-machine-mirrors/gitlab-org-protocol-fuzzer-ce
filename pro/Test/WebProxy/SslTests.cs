using System.Diagnostics;
using System.Net;
using System.Net.Http;
using NUnit.Framework;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class SslTests
	{
		private readonly ProxyServer _proxy = new ProxyServer();

		private int Port { get { return _proxy.ProxyEndPoints[0].Port; } }

		[SetUp]
		public void SetUp()
		{
			_proxy.Logger.TraceSource.Switch.Level = SourceLevels.All;
			_proxy.AddEndPoint(new ExplicitProxyEndPoint(IPAddress.Any, 0));
			_proxy.Start();
		}

		[TearDown]
		public void TearDown()
		{
			_proxy.Stop();
		}

		[Test]
		public void TestGoogle()
		{
			var cookies = new CookieContainer();
			var handler = new WebRequestHandler
			{
				CookieContainer = cookies,
				UseCookies = true,
				UseDefaultCredentials = false,
				Proxy = new System.Net.WebProxy("http://127.0.0.1:" + Port, false, new string[] { }),
				UseProxy = true,
				ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
			};

			var cli = new HttpClient(handler);
			var result = cli.GetStringAsync("https://www.google.com/").Result;

			Assert.NotNull(result);
		}
	}
}