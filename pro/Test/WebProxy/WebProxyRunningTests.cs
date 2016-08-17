using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core.WebApi.Proxy;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class WebProxyRunningTests : BaseRunTester
	{
		[Test]
		public void TestOnRequest()
		{
			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		}

/*		[Test]
		public void TestFaulting()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<Test name=""Default"">
		<WebProxy>
			<Route url='*' />
		</WebProxy>
		<Strategy class='WebProxy' />
		<Publisher class='WebApiProxy' />
	</Test>
</Peach>";

			var task = Task.Run(() =>
			{
				try
				{
					RunEngine(xml, false, 2);
				}
				catch (Exception)
				{
					System.Diagnostics.Debugger.Break();
				}
			});

			Thread.Sleep(2000);

			var client = GetHttpClient();
			
			var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;
			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			
			response = client.GetAsync(BaseUrl + "/unknown/api/errors/500").Result;
			Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);

			task.Wait();
		} */

		[Test]
		public void TestProxy()
		{
			using (var p = new ProxyServer())
			{
				p.AddEndPoint(new ExplicitProxyEndPoint(IPAddress.Loopback, 0));
				p.Start();

				var count = 0;

				p.BeforeRequest += (o, e) => 
				{
					var req = e.WebSession.Request;

					req.RequestUri = req.RequestUri.Rewrite(Server.Uri.ToString());

					++count;

					return Task.FromResult(0);
				};

				var xml = @"<?xml version='1.0' encoding='utf-8'?>
	<Peach>
		<Test name='Default' maxOutputSize='65000'>
			<WebProxy proxy='http://127.0.0.1:{0}'>
				<Route
					url='*' mutate='false'
				/> 
			</WebProxy>
			<Strategy class='WebProxy' />
			<Publisher class='WebApiProxy'>
				<Param name='Port' value='0' />
			</Publisher>
		</Test>
	</Peach>".Fmt(p.ProxyEndPoints[0].Port);

				RunEngine(xml);

				var client = GetHttpClient();
				var headers = client.DefaultRequestHeaders;
				headers.Add("X-Peachy", "Testing 1..2..3..");

				var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;
				Assert.NotNull(response);

				Assert.AreEqual(1, count);
			}
		}
	}
}
