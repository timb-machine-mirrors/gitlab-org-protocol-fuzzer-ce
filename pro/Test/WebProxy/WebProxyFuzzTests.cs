using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class WebProxFuzzTests : BaseRunTester
	{
		protected override RunConfiguration GetRunConfiguration()
		{
			return new RunConfiguration { range = true, rangeStart = 1, rangeStop = 19 };
		}

		public override void SetUp()
		{
		}

		[Test]
		public void Test()
		{
			SetUpFixture.EnableDebug();

			var xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<Test name='Default' maxOutputSize='65000'>
		<WebProxy>
			<Route
				url='*/unknown?api/*' mutate='true'
				baseUrl='{0}'
			/> 
			<Route
				url='*' mutate='false'
				baseUrl='{0}'
			/> 
		</WebProxy>
		<Strategy class='WebProxy' />
		<Publisher class='WebApiProxy'>
			<Param name='Port' value='0' />
		</Publisher>
	</Test>
</Peach>".Fmt(Server.Uri);

			RunEngine(xml);

			for (var i = 0; i < 20; ++i)
			{
				var content = new FormUrlEncodedContent(new[] 
				{
					new KeyValuePair<string, string>("value", "Foo Bar")
				});

				var client = GetHttpClient();
				var headers = client.DefaultRequestHeaders;
				headers.Add("X-Peachy", "Testing 1..2..3..");

				var response = client.PutAsync(BaseUrl + "/unknown/api/values/5?filter=foo", content).Result;
				Assert.NotNull(response);
			}
		}

		[Test]
		[Ignore("Requires an openssl server to be running on port 44330")]
		public void TestClientCert()
		{
			// openssl req -x509 -newkey rsa:2048 -keyout key.pem -out cert.pem -days 365 -nodes
			// openssl s_server -key key.pem -cert cert.pem -accept 44330 -www -tls1_2 -verify 1

			SetUpFixture.EnableTrace();

			using (var tmp = new TempDirectory())
			{
				var certFile = Path.Combine(tmp.Path, "cert.pem");
				File.WriteAllText(certFile, SslTests.CertChain);

				var keyFile = Path.Combine(tmp.Path, "key.pem");
				File.WriteAllText(keyFile, SslTests.PrivateKey);

				SetUpFixture.EnableDebug();

				var xml = @"<?xml version='1.0' encoding='utf-8'?>
	<Peach>
		<Test name='Default' maxOutputSize='65000'>
			<WebProxy>
				<Route
					url='*' mutate='false'
					baseUrl='{0}'
				/> 
			</WebProxy>
			<Strategy class='WebProxy' />
			<Publisher class='WebApiProxy'>
				<Param name='ClientCert' value='{1}' />
				<Param name='ClientKey' value='{2}' />
				<Param name='Port' value='0' />
			</Publisher>
		</Test>
	</Peach>".Fmt("https://127.0.0.1:44330", certFile, keyFile);

				RunEngine(xml);

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

				var client = new HttpClient(handler);
				var headers = client.DefaultRequestHeaders;
				headers.Add("X-Peachy", "Testing 1..2..3..");

				var response = client.GetAsync("https://testhost/").Result;
				Assert.NotNull(response);
			}
		}
	}
}
