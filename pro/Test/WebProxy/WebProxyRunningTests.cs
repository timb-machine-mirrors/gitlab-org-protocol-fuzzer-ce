using System.Net;
using NUnit.Framework;

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
	}
}
