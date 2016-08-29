using System.Collections.Generic;
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
			return new RunConfiguration { rangeStart = 1, rangeStop = 20 };
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
	}
}
