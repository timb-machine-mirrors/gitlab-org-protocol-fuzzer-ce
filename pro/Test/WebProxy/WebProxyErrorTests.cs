using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class WebProxErrorTests : BaseRunTester
	{
		public override void SetUp()
		{
			Server.Dispose();
			Server = null;
		}

		[Test]
		public void TestNoServer()
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
</Peach>".Fmt("http://127.0.0.1:2");

			Exception ex = null;

			RunEngine(xml, hook: e =>
			{
				e.TestError += (ctx, exception) =>
				{
					ex = exception;
				};
			});

			//for (var i = 0; i < 20; ++i)
			{
				var content = new FormUrlEncodedContent(new[] 
				{
					new KeyValuePair<string, string>("value", "Foo Bar")
				});

				var client = GetHttpClient();
				var headers = client.DefaultRequestHeaders;
				headers.Add("X-Peachy", "Testing 1..2..3..");

				Assert.Throws<AggregateException>(() =>
				{
					client.PutAsync(BaseUrl + "/unknown/api/values/5?filter=foo", content).Wait();
				});

				Assert.True(Engine.Wait(10000));

				Assert.NotNull(ex);

				Assert.IsInstanceOf<PeachException>(ex);
				Assert.IsInstanceOf<SoftException>(ex.InnerException);

				StringAssert.StartsWith("Error sending request", ex.InnerException.Message);
			}
		}
	}
}
