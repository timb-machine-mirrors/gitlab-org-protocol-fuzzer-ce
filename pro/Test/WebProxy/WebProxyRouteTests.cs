using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.MutationStrategies;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.WebApi;
using Peach.Pro.Core.WebApi.Proxy;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;
using Assert = NUnit.Framework.Assert;

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
		public void TestScript()
		{
			using (var d = new TempDirectory())
			{
				const string script = @"
from peach import webproxy

def on_request(context, request, body):
	request.__peach_req.Method = 'FOOBAR'

webproxy.register_event(webproxy.EVENT_ACTION, on_request)
";

				var module = Path.Combine(d.Path, "mymodule.py");
				File.WriteAllText(module, script);

				var xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<Test name='Default' maxOutputSize='65000'>
		<WebProxy>
			<Route
				url='*/unknown?api/*' mutate='true'
				script='{1}'
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
</Peach>".Fmt(Server.Uri, module);

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


		[Test]
		public void TestRequest()
		{
			const string script = @"
from peach import webproxy

def map_req(req):
	return '%s' % dir(webproxy.Request(req))

def to_h(k, v):
	return webproxy.Header(k, v)

def to_p(req):
	return webproxy.Request(req)
";

			using (var d = new TempDirectory())
			{
				var p = new PythonScripting();
				p.AddSearchPath(Configuration.ScriptsPath);

				File.WriteAllText(Path.Combine(d.Path, "mymodule.py"), script);
				p.AddSearchPath(d.Path);
				p.ImportModule("mymodule");

				var req = new Request
				{
					Method = "PUT",
					RequestUri = new Titanium.Web.Proxy.Uri("https://the.host:9999/path/to/item?query&string&goes=here"),
					ContentType = "application/json"
				};

				req.NonUniqueRequestHeaders.Add("x-peachy", new List<HttpHeader>
				{
					new HttpHeader("X-Peachy", "Foo"),
					new HttpHeader("X-Peachy", "Bar"),
				});

				var scope = new Dictionary<string, object> { { "req", req } };

				p.Exec("mymodule.to_p(req).method = 'BBB'", scope);

				Assert.AreEqual("BBB", req.Method);

				Assert.AreEqual("https", p.Eval("mymodule.to_p(req).uri.scheme", scope));
				Assert.AreEqual("the.host", p.Eval("mymodule.to_p(req).uri.host", scope));
				Assert.AreEqual(9999, p.Eval("mymodule.to_p(req).uri.port", scope));
				Assert.AreEqual("/path/to/item", p.Eval("mymodule.to_p(req).uri.path", scope));
				Assert.AreEqual("?query&string&goes=here", p.Eval("mymodule.to_p(req).uri.query", scope));

				Console.WriteLine(p.Eval("mymodule.to_p(req).headers", scope));
				Console.WriteLine(p.Eval("str(mymodule.to_p(req).headers)", scope));
				Console.WriteLine(p.Eval("str(mymodule.to_p(req).headers.keys())", scope));
				Console.WriteLine(p.Eval("str(mymodule.to_p(req).headers.values())", scope));
				Console.WriteLine(p.Eval("str([x for x in mymodule.to_p(req).headers])", scope));
				Console.WriteLine(p.Eval("str([str(item) for _,sublist in mymodule.to_p(req).headers.iteritems() for item in sublist])", scope));

				p.Exec("mymodule.to_p(req).headers['X-pEaChY'][1].value = 'aaabbb'", scope);

				Assert.AreEqual("aaabbb", req.NonUniqueRequestHeaders["x-peachy"][1].Value);

				p.Exec("mymodule.to_p(req).headers['foo1'] = 'bar'", scope);

				Assert.That(req.RequestHeaders, Contains.Key("foo1"));
				Assert.That(req.NonUniqueRequestHeaders, !Contains.Key("foo1"));
				Assert.That(req.RequestHeaders["foo1"].Name, Is.EqualTo("foo1"));
				Assert.That(req.RequestHeaders["foo1"].Value, Is.EqualTo("bar"));

				p.Exec("mymodule.to_p(req).headers['foo2'] = [ mymodule.to_h('foo2', 'baz') ]", scope);

				Assert.That(req.RequestHeaders, Contains.Key("foo2"));
				Assert.That(req.NonUniqueRequestHeaders, !Contains.Key("foo2"));
				Assert.That(req.RequestHeaders["foo2"].Name, Is.EqualTo("foo2"));
				Assert.That(req.RequestHeaders["foo2"].Value, Is.EqualTo("baz"));

				p.Exec("mymodule.to_p(req).headers['foo1'] = [ mymodule.to_h('foo1', 'qux'), mymodule.to_h('foo1', 'quux') ]", scope);

				Assert.That(req.RequestHeaders, !Contains.Key("foo1"));
				Assert.That(req.NonUniqueRequestHeaders, Contains.Key("foo1"));

				var items = req.NonUniqueRequestHeaders["foo1"];
				Assert.AreEqual(2, items.Count);

				Assert.That(items[0].Name, Is.EqualTo("foo1"));
				Assert.That(items[1].Name, Is.EqualTo("foo1"));
				Assert.That(items[0].Value, Is.EqualTo("qux"));
				Assert.That(items[1].Value, Is.EqualTo("quux"));
			}
		}

		[Test]
		public void TestAwsAuth()
		{
			const string script = @"
from peach import webproxy

import base64
import hmac

from hashlib import sha1
from email.Utils import formatdate

AWS_ACCESS_KEY_ID = '44CF9590006BF252F707'
AWS_SECRET_KEY = 'OtxrzxIsfpFjA7SwPzILwy8Bw21TLhquhboDYROV'

def aws_auth(ctx, req, body):
    XAmzDate = formatdate()

    h = hmac.new(AWS_SECRET_KEY, '%s\n\n%s\n\nx-amz-date:%s\n/?policy' % (req.method, req.contentType, XAmzDate), sha1)
    authToken = base64.encodestring(h.digest()).strip()

    req.headers['x-amz-date'] = XAmzDate
    req.headers['Authorization'] = 'AWS %s:%s' % (AWS_ACCESS_KEY_ID, authToken)

webproxy.register_event(webproxy.EVENT_ACTION, aws_auth)

";

			using (var d = new TempDirectory())
			{
				var module = Path.Combine(d.Path, "aws.py");

				File.WriteAllText(module, script);

				var xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<Test name='Default' maxOutputSize='65000'>
		<WebProxy>
			<Route
				url='*/unknown?api/*' mutate='true'
				script='{1}'
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
</Peach>".Fmt(Server.Uri, module);

				string req = null;

				RunEngine(xml, null, (e, context, op) =>
				{
					req = e.WebSession.Request.ToString();
				});

				var client = GetHttpClient();
				var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;

				Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
				StringAssert.Contains("x-amz-date: ", req);
				StringAssert.Contains("Authorization: ", req);
			}
		}
	}
}
