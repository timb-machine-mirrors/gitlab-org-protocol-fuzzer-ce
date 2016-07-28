using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class WebProxyRunningTests : BaseRunTester
	{
		[Test]
		public void TestOnRequest()
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

			var dom = DataModelCollector.ParsePit(xml);
			var e = new Engine(null);

			var cfg = new RunConfiguration { singleIteration = true };

			Task.Run(() =>
			{
				e.startFuzzing(dom, cfg);
			});

			Thread.Sleep(2000);

			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		}
	}
}
