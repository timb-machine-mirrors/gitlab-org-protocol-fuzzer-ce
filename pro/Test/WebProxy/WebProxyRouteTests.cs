using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core.MutationStrategies;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.WebApi;
using Peach.Pro.Core.WebApi.Proxy;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class WebProxyRouteTests : BaseRunTester
	{
		[SetUp]
		public override void SetUp()
		{
			// Skip base setup
		}

		[Test]
		public void TestNonMutable()
		{
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
			<Param name='Port' value='0' />
		</Publisher>
	</Test>
</Peach>".Fmt(Server.Uri);

			RunEngine(xml);

			var content = new FormUrlEncodedContent(new[] 
			{
				new KeyValuePair<string, string>("value", "Foo Bar")
			});

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.PutAsync(BaseUrl + "/unknown/api/values/5?filter=foo", content).Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.Null(op.ShadowOperation);
			Assert.LessOrEqual(4, op.Parameters.Count);

			foreach (var param in op.Parameters)
			{
				Assert.IsFalse(param.DataElement.isMutable);
			}
		}

		[Test]
		public void TestNonMutableHeaders()
		{
			var xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<Test name='Default' maxOutputSize='65000'>
		<WebProxy>
			<Route url='*' mutate='true' baseUrl='{0}'>
				<ExcludeHeader>*</ExcludeHeader>
				<IncludeHeader>X-*</IncludeHeader>
				<ExcludeHeader>X-Peachy-Keen</ExcludeHeader>
			</Route>
		</WebProxy>
		<Strategy class='WebProxy' />
		<Publisher class='WebApiProxy'>
			<Param name='Port' value='0' />
		</Publisher>
	</Test>
</Peach>".Fmt(Server.Uri);

			RunEngine(xml);

			var content = new FormUrlEncodedContent(new[] 
			{
				new KeyValuePair<string, string>("value", "Foo Bar")
			});

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");
			headers.Add("X-Peachy-Keen", "Testing 1..2..3..");

			var response = client.PutAsync(BaseUrl + "/unknown/api/values/5?filter=foo", content).Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.Null(op.ShadowOperation);
			Assert.LessOrEqual(4, op.Parameters.Count);

			foreach (var param in op.Parameters)
			{
				if (param.In != WebApiParameterIn.Header)
				{
					Assert.IsTrue(param.DataElement.isMutable);
					continue;
				}

				if (param.Key == "X-Peachy-Keen")
					Assert.IsFalse(param.DataElement.isMutable);
				else if (param.Key.StartsWith("X-"))
					Assert.True(param.DataElement.isMutable);
				else
					Assert.IsFalse(param.DataElement.isMutable);
			}
		}

		[Test]
		public void TestRouteMatch()
		{
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

			var content = new FormUrlEncodedContent(new[] 
			{
				new KeyValuePair<string, string>("value", "Foo Bar")
			});

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.PutAsync(BaseUrl + "/unknown/api/values/5?filter=foo", content).Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.Null(op.ShadowOperation);
			Assert.LessOrEqual(4, op.Parameters.Count);

			foreach (var param in op.Parameters)
			{
				if(param.Name == "content-length" || param.Name == "host")
					Assert.IsFalse(param.DataElement.isMutable);
				else
					Assert.IsTrue(param.DataElement.isMutable);
			}
		}

		[Test]
		public void TestRewriteUrl()
		{
			// All tests require rewrite to work.
		}

		[Test]
		public void TestOnRequest()
		{
			var xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<Test name='Default' maxOutputSize='65000'>
		<WebProxy>
			<Route
				url='*/unknown?api/*' mutate='true'
				onRequest=""setattr(request, 'Method', 'FOOBAR')""
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

			string method = null;

			RunEngine(xml, null, (e, context, op) =>
			{
				method = e.WebSession.Request.Method;
			});

			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;

			Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
			Assert.AreEqual("FOOBAR", method);
		}

		[Test]
		public void TestNoRoutes()
		{
			// Default route gets added when the proxy runs
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<Test name='Default' maxOutputSize='65000'>
		<WebProxy />
		<Strategy class='WebProxy' />
		<Publisher class='WebApiProxy'>
			<Param name='Port' value='0' />
		</Publisher>
	</Test>
</Peach>";

			WebProxyOptions options = null;

			RunEngine(xml,
				hookRequestEventPost: (e, context, op) =>
				{
					// Since no routes are specified, we have to manually rewrite the url
					e.WebSession.Request.RequestUri = e.WebSession.Request.RequestUri.Rewrite(Server.Uri.ToString());
				},
				hook: e =>
				{
					e.IterationFinished += (ctx, it) =>
					{
						options = ((WebProxyStateModel)ctx.test.stateModel).Options;
					};
				});

			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(options);
			Assert.NotNull(options.Routes);
			Assert.AreEqual(1, options.Routes.Count);

			var r = options.Routes[0];

			Assert.AreEqual("*", r.Url);
			Assert.False(r.Mutate);
			Assert.AreEqual("500,501", r.FaultOnStatusCodesAttr);
		}
	}
}
