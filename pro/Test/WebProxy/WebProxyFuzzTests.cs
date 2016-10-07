using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Castle.Core.Internal;
using NUnit.Framework;
using Org.BouncyCastle.Asn1.Cmp;
using Org.BouncyCastle.Utilities.Collections;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core.Agent.Monitors;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebApi;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Test.WebProxy.TestTarget.Controllers;
using Titanium.Web.Proxy;
using Encoding = Peach.Core.Encoding;
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

		const string CaCert = @"-----BEGIN CERTIFICATE-----
MIIDbjCCAlagAwIBAgIQAOPfn033/QJ67ldh8wPIyzANBgkqhkiG9w0BAQsFADBB
MREwDwYDVQQKDAhUaXRhbml1bTEsMCoGA1UEAwwjVGl0YW5pdW0gUm9vdCBDZXJ0
aWZpY2F0ZSBBdXRob3JpdHkwIBcNMTYwODMwMTQ0OTI1WhgPMjExNjA5MDYxNDQ5
MjVaMEExETAPBgNVBAoMCFRpdGFuaXVtMSwwKgYDVQQDDCNUaXRhbml1bSBSb290
IENlcnRpZmljYXRlIEF1dGhvcml0eTCCASIwDQYJKoZIhvcNAQEBBQADggEPADCC
AQoCggEBAKz722xUKz7Axvs7y9GzJW7vb7M0HnALIZZv3dtWz/4CXAiyu6sGUIAK
0E4hpCIOfENpZOzE0dxNhn6a10OIYTBmd+ys1NSLFZVtPu7sueydyab4MfflBeKS
KGdFpYSJJc7PstuyLvm4oaDQMQ++aqP90OzCMvrhq5KpSzXAaI5Tgi8XjoXpiZW5
sD32EYOP7dCUKjqD5zuyjzOgHrF/a9hIDdREqhdbPYlj7/Ck1WPARqEhNjxRmGtS
rR9dnoNrLGWA2ysK9uNDSc3gfRRUi0XcRxhyIi0/qQPEjKg1Zc391aCF8BZjmNGm
9BZc1GmEdlgQo8adqsozyGcnLMydl90CAwEAAaNgMF4wDwYDVR0TAQH/BAUwAwEB
/zALBgNVHQ8EBAMCAgQwHQYDVR0OBBYEFA5nhiTiSWo6j7pBXGC+abAnasgqMB8G
A1UdIwQYMBaAFA5nhiTiSWo6j7pBXGC+abAnasgqMA0GCSqGSIb3DQEBCwUAA4IB
AQAbwg0vdIdv/Qu+GfiYbtTrFrq4h2HWg68OgsLCnF+5HDcUjMtwcxKN1L6Vzz/z
p0VwSgpkOEWPoL+U91COMedMavAmlRJquEgsyd5BQxnwWbF/Ibp2fwKljHiBBqxV
ZnfJ/vKrH2b0+nu6hp2NUiCcGEc0Z/RhHW8SLIT9JlOLgsQaxLcivMxTdNHvEoWT
gK95NMacwi/iuPulPNbeUkE2cRZ2KiM8KBcxmpe44oREExanNk1+7yrsrTDGxQy6
7xuG7HKwzVOZGFs86oye3AtyrtrYKp8WCwqv6LR+w27E0hegwBcITKCt3PbHJz2e
5VFiSN7XFuZGFUnEU4uAriHW
-----END CERTIFICATE-----";

		const string CaKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEArPvbbFQrPsDG+zvL0bMlbu9vszQecAshlm/d21bP/gJcCLK7
qwZQgArQTiGkIg58Q2lk7MTR3E2GfprXQ4hhMGZ37KzU1IsVlW0+7uy57J3Jpvgx
9+UF4pIoZ0WlhIklzs+y27Iu+bihoNAxD75qo/3Q7MIy+uGrkqlLNcBojlOCLxeO
hemJlbmwPfYRg4/t0JQqOoPnO7KPM6AesX9r2EgN1ESqF1s9iWPv8KTVY8BGoSE2
PFGYa1KtH12eg2ssZYDbKwr240NJzeB9FFSLRdxHGHIiLT+pA8SMqDVlzf3VoIXw
FmOY0ab0FlzUaYR2WBCjxp2qyjPIZycszJ2X3QIDAQABAoIBAAsLKT8JQJmJBSsU
7jY9Ea67ao4uTaMpksNf8PIH4M9+pxGHF6QbixghmJrkWu4xro0/NUpIZn2CFYwP
pp6RHmNQp2dcwVmwZ3hauSHXyyboC++0+Luyy2Vjn2C4eZC0kM1bRTfxcD5RI0B5
CijPJ0/A7I2w+EA8GBAM7thHqGUtpMX4MyPWr5XqIOHyhsePrAIqG+oZ49qiDKDz
e+S9f5vSGGLrogcGfdBp+rXbsLARp/rNtO/MVnF7QbHt774w+WFmsLc5ziVYcwB1
7dPKv9R5DamL3NxkpILtnowHM1vAn+B4cSmUqdiSt5P3XVjXoovTOYrStx2zgoeO
pNhO6QECgYEA2vOqhAaPlwX846RfFKPpReIICW3MDtRijrz0rHjG5eECiFdl2qoq
+P7az7dVEiwnHwsIkVIN08/GdO8sJUO0GrsWgqsGObeDI9Zrsu22meEOwfgWMwSk
btJkW9HRFelQlPTDeieEkfm8x2xdXVSEGj+FQPrikttq/cBug5mR9uUCgYEAykD+
1trF6fVx+1dNCAEH2EdCVWgE0cN2t1jN5zHIaQFyS+lKMaZtKDfTVhO4HJANKMgq
SBDbIEZGB2l8QycycfLazCVPTSGOsG9SoWscAhVz0FkLwZ74LIN6iFqwtYZzws5+
UpEF+cvjT/zJDVdEMM9CH9siQYNtPnijWAJbVZkCgYEAtj0hrAoZ8cXWG1NwoCld
vADKV66/rYgxEEVOEU/lnOiPxxOXf00rv1vAnCsn75w4Y6o3U11MjQPfVuzLfajA
e49EyTW4VMndqTKLKb6ZwbKFKgVn1McEZglP5uzYbrhnjdO78Cx8N1P6QLJ9c3Up
Dv/X1aH8e+eNQe+tDHQB/dECgYBqKXOak3sPMyaBJ3HnoaLcg4ruCYt+D3thAdwS
Xq8zbu7vqaSoKxNg6OylYfRiCbrejTKkYTSj3D8l5Ni05v4zkaYHQg4+Dj83nTdi
QzaQYXJJCnSBTVA3DUxkBjZ7EffxaTIlZLoeREcs1SMzPXsto5yx2/Q/Lx1IjUow
CAypAQKBgQCvY13t6pBQIXuBZOC66Hu8FJt3T9nyLSA+39YOFe1D+SZQ8yLUKotp
pexOR8utwzmVPtEhxD10TSOxxychbBx8bGsxi2EB15WNRgwdvLnBIC8Kpfvc3HIX
+AnlFNSy06tf4aI7ENmZZG9ov6OMpFxUzRWViCaVnQEbL1pydC4/FQ==
-----END RSA PRIVATE KEY-----";

		const string ServerKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEA2FbUhXA7dO6LDN9gqNogJoODIbjWXF0BMxhHwn7DQsEvqxfN
4fbzXfZmRRtJuzIWwOLjJRG+lrtVlR2Wx73m+6xCO2SoQ685HQW7TqyhpOngXNm7
hpEwL4iiLpglJFNs6L7oMnLBonco5j60n6A/fUdhdBquwPra7vcgh+ep72BmH7dj
+HEHFhTtgaao953JqkwmC+TObYadmvmPLqlv+FRECzY6OWhExdx+aoiCInJDt6lS
ebxRUpQncf7VJTDfZUA+rh0cNdXOaDydp7z9ngnPxKQ1yiXWhcHyS33vn9l5EB2P
x9ZiRb3eHQSP/Qgz+ioakcJ75rdKCHn7H8O4hwIDAQABAoIBADMc8MFGLwYFhhzY
egHq50WyNL026o6P+sSTrACr6n5PHnASA7arnfxJRwh01grAXtfbw0by1lDOmf3N
K96tY3F61Xsq4QZ808WjoQmJh/dqunO1jbTRgMz6Pzc7Ayt9+nFTCZFBw7Ya6TLd
BdfhCJ8UylWgKEG4AxoKkUkK1v+TG9aXcB3+KZUpF/jB9INGKWGAhb97D2b1hac0
PgFHxRZ24bCVw+O0FWbL+xzvoOnpb28SRt81Hm9FmBiYWLfZyLd75IUW45VVyaam
wHFUnYnsZXgWd2mJMwAiwWTG7BJ40gq8gb2e6wOR0uUocSpa3af0YuUf9QWyzRBX
bNPJWgECgYEA7r90iCci/Gmq8dYDSE/vubzxUftBwXM1XbceDa/32Uw1fbAMtmP/
l7IyUgRMcK2DQfmnwpNfTBDTgsSOJ9W5SQloAfZVj2m9VRGy5B0pRthMzyXuLb/4
g88RFpUzoXYiimdgS8XXWvU6QEkhf5PgeMXBPGR2UHj5Kvb9G0oMYscCgYEA5/jX
agupk5RdEYehTnK6v7aymVbKWh8Sac5mKpI9WqcpJ7ZeR/WMxCz4gltX8hWY8iBi
JmS+RlM2ChbIR/3iUPbqfWEnwxars8rK8lkXmfZyN16yEls+59IsTYjJIks6qQdV
TApzR43wCJU0PJt7UqsVj0FrvuCeRAz7kMDIPEECgYAx87zd/9JCHZm2n6BwMMln
gzC6hmdroj84LGgNVRP2UwueEIeYYbPIeXAt2NhleuhLlYCUJWF6+MpIQfe8PetW
bLFmN7QPHYCPm/Rh5fgM6pSngrgiule0vE5G+1CiJ6Vyb86mC+7TCRv291Ya60W7
/yQ/DoXysFzxsFukqgmNYQKBgQCRjcCM19iFs5haQYJjmPW5CcgzExRRTCHfphTR
LYW19iGKu5GZEWhMR/N+yBX83rRjaRJtCNWjht7nobf2BEYXi3dDSM0MSpNecya4
vlJi1xJ/z7lobzyfdW87D1M1Y4LhQKqy1fPTuCofGI/4X48YJiWXB/O1h3eHN6Y6
A90ggQKBgQCdNFyK1imKRlDtmUR1cUTG1PlQOqduLWCDIx77iCI6KbXibFhea011
Cgx9pQkSXnS1kznLKYeDsRJFljCGOns2EyKtSh/nE3g+panNdUEOe+QP9KIEAvrj
QN2CJgB1sNtKNTOAbKHcGxgk6hQPaM5SYzEh8R888ei/vxj12O6Qow==
-----END RSA PRIVATE KEY-----";

		[Test]
		public void Test()
		{
			SetUpFixture.EnableDebug();

			File.WriteAllText(Path.Combine(TempDir.Path, "CaCert.pem"), CaCert);
			File.WriteAllText(Path.Combine(TempDir.Path, "CaKey.pem"), CaKey);
			File.WriteAllText(Path.Combine(TempDir.Path, "ServerKey.pem"), ServerKey);

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
			<Param name='CaCert' value='{1}/CaCert.pem' />
			<Param name='CaKey' value='{1}/CaKey.pem' />
			<Param name='ServerKey' value='{1}/ServerKey.pem' />
		</Publisher>
	</Test>
</Peach>".Fmt(Server.Uri, TempDir.Path);

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
		public void TestMutations()
		{
			SetUpFixture.EnableDebug();

			File.WriteAllText(Path.Combine(TempDir.Path, "CaCert.pem"), CaCert);
			File.WriteAllText(Path.Combine(TempDir.Path, "Cakey.pem"), CaKey);
			File.WriteAllText(Path.Combine(TempDir.Path, "ServerKey.pem"), ServerKey);

			var xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<Test name='Default' maxOutputSize='65000'>
		<WebProxy>
			<Route
				url='*/unknown?api/*' mutate='true'
				baseUrl='{0}'
			/> 
			<Route
				url='*' mutate='true'
				baseUrl='{0}'
			/> 
		</WebProxy>
		<Strategy class='WebProxy' />
		<Publisher class='WebApiProxy'>
			<Param name='Port' value='0' />
			<Param name='CaCert' value='{1}/CaCert.pem' />
			<Param name='CaKey' value='{1}/CaKey.pem' />
			<Param name='ServerKey' value='{1}/ServerKey.pem' />
		</Publisher>
	</Test>
</Peach>".Fmt(Server.Uri, TempDir.Path);
			RunEngine(xml);

			for (var i = 0; i < 20; i++)
			{

				var content = new FormUrlEncodedContent(new[]
				{
					new KeyValuePair<string, string>("value", "Foo Bar") 
				});

				var client = GetHttpClient();
				var headers = client.DefaultRequestHeaders;
				headers.Add("X-Peachy", "Testing 1..2..3..");

				var response = client.PutAsync(BaseUrl + "/api/values/5?filter=foo", content).Result;
				Assert.NotNull(response);

				var paramList = new List<WebApiParameter>();

				if (i == 0)
				{
					Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

					var op = GetOp();
					paramList = op.Parameters;
					Assert.NotNull(op);
					Assert.AreEqual("PUT", op.Method);
					Assert.NotNull(op.Path);
					Assert.AreEqual("/{api}/{values}/{5}", op.Path.Path);
					Assert.AreEqual("PUTapivalues5", op.Name);
					
					var param = op.Parameters.First(j => j.In == WebApiParameterIn.Path);
					Assert.AreEqual(WebApiParameterIn.Path, param.In);
					Assert.AreEqual("api", (string)param.DataElement.DefaultValue);
					Assert.AreEqual(5, ValuesController.Id);
				}
				else if (i == 1)
				{
					try
					{
						Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
					}
					catch (Exception)
					{
						//Fuzzing so weird requests happen, if not caught, tests halt. 
						continue; 
					}
					var op = GetOp();
					Assert.NotNull(op);
					Assert.AreNotEqual("/{api}/{values}/{5}/", op.Path.Path);
					
					// if the two lists are identical means no fuzzing 
					Assert.AreNotSame(paramList, op.Parameters);
				}
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
