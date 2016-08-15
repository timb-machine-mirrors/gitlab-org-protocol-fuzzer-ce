using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using NUnit.Framework;
using Peach.Core;

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
		public void TestStartIterationEvent()
		{
			var xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<Agent name='local'>
		<Monitor class='WebClient'>
			<Param name='SetUpCommand' value='' />
			<Param name='SetUpArguments' value='' />
			<Param name='SetUpTimeout' value='' />

			<Param name='TestCommand' value='' />
			<Param name='TestArguments' value='' />
			<Param name='TestTimeout' value='' />

			<Param name='TearDownCommand' value='' />
			<Param name='TearDownArguments' value='' />
			<Param name='TearDownTimeout' value='' />

			<Param name='Proxy' value='127.0.0.1:0' />
		</Monitor>
	</Agent>

	<Test name='Default' maxOutputSize='65000'>
		<WebProxy>
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

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.GetAsync(BaseUrl + "/start").Result;
			Assert.NotNull(response);
		}
	}
}
