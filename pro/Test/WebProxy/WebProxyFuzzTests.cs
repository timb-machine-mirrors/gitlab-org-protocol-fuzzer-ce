using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core.Agent.Monitors;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;
using FaultSummary = Peach.Pro.Core.WebServices.Models.FaultSummary;
using Monitor = System.Threading.Monitor;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class WebProxFuzzTests : BaseRunTester
	{
		protected override RunConfiguration GetRunConfiguration()
		{
			return new RunConfiguration { range = true, rangeStart = 1, rangeStop = 19, pitFile = "FuzzTests" };
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
		public void TestFault()
		{
			var xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<Agent name='Local'>
		<Monitor class='Syslog'>
			<Param name='Port' value='0' />
			<Param name='FaultRegex' value='the_fault' />
		</Monitor>
	</Agent>

	<Test name='Default' maxOutputSize='65000'>
		<WebProxy>
			<Route
				url='*' mutate='false'
				faultOnStatusCodes='404'
				baseUrl='{0}'
			/> 
		</WebProxy>
		<Logger class='File' />
		<Strategy class='WebProxy' />
		<Publisher class='WebApiProxy'>
			<Param name='Port' value='0' />
		</Publisher>
		<Agent ref='Local' />
	</Test>
</Peach>".Fmt(Server.Uri);

			Socket socket = null;
			var jobId = Guid.Empty;

			RunEngine(xml, hook: e => 
			{
				e.IterationStarting += (ctx, it, tot) =>
				{
					jobId = ctx.config.id;

					var mon = ctx.GetMonitor<SyslogMonitor>();
					Assert.NotNull(mon);

					if (socket == null)
					{
						socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

						mon.InternalEvent += (sender, args) =>
						{
							lock (socket)
							{
								Monitor.Pulse(socket);
							}
						};
					}

					var detail = it == 6 ? "the_fault" : "msg " + it;
					var buf = Encoding.ASCII.GetBytes("<30>Oct 12 12:49:06 host app[12345]: " + detail);

					lock (socket)
					{
						socket.SendTo(buf, new IPEndPoint(IPAddress.Loopback, mon.Port));
						Assert.True(Monitor.Wait(socket, 10000), "Syslog msg not received!");
					}
				};
			});

			for (var i = 0; i < 20; ++i)
			{
				var content = new FormUrlEncodedContent(new[] 
				{
					new KeyValuePair<string, string>("value", "Foo Bar")
				});

				var client = GetHttpClient();
				var headers = client.DefaultRequestHeaders;
				headers.Add("X-Peachy", "Testing 1..2..3..");

				var path = i == 6 ? "/unknown2" : "/unknown";
				var response = client.PutAsync(BaseUrl + path + "/api/values/5?filter=foo", content).Result;
				Assert.NotNull(response);
			}

			Assert.True(Engine.Wait(10000), "Engine should have completed!");

			Job job;
			using (var db = new NodeDatabase())
			{
				job = db.GetJob(jobId);
			}

			Assert.NotNull(job, "Job couldn't be found");

			using (var db = new JobDatabase(job.DatabasePath))
			{
				var faults = db.LoadTable<FaultSummary>().ToList();
				Assert.AreEqual(1, faults.Count);

				var f = db.GetFaultById(faults[0].Id, NameKind.Human);
				Assert.NotNull(f, "Fault should not be null");

				// Ensure monitor faults take precedence over faultOnStatusCode
				Assert.AreEqual("FaultRegex matched syslog message", f.Title);
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
