using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Pro.Core.MutationStrategies;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.WebApi;
using Peach.Pro.Test.WebProxy.TestTarget;
using Peach.Pro.Test.WebProxy.TestTarget.Controllers;
using Titanium.Web.Proxy.EventArguments;
using ASCIIEncoding = Peach.Core.ASCIIEncoding;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class WebProxyRouteTests : BaseTester
	{
		[Test]
		public void TestNonMutable()
		{
			_proxy.Options.Routes.Clear();
			_proxy.Options.Routes.Add(new WebProxyRoute
			{
				Url = "*",
				Mutate = false
			});

			var content = new FormUrlEncodedContent(new[] 
			{
				new KeyValuePair<string, string>("value", "Foo Bar")
			});

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.PutAsync(BaseUrl + "/unknown/api/values/5?filter=foo", content).Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(_opPre);
			Assert.Null(_opPre.ShadowOperation);
			Assert.LessOrEqual(4, _opPre.Parameters.Count);

			foreach (var param in _opPre.Parameters)
			{
				Assert.IsFalse(param.DataElement.isMutable);
			}
		}

		[Test]
		public void TestRouteMatch()
		{
			_proxy.Options.Routes.Clear();
			_proxy.Options.Routes.Add(new WebProxyRoute
			{
				Url = "*/unknown?api/*",
				Mutate = true
			});

			_proxy.Options.Routes.Add(new WebProxyRoute
			{
				Url = "*",
				Mutate = false
			});

			var content = new FormUrlEncodedContent(new[] 
			{
				new KeyValuePair<string, string>("value", "Foo Bar")
			});

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.PutAsync(BaseUrl + "/unknown/api/values/5?filter=foo", content).Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(_opPre);
			Assert.Null(_opPre.ShadowOperation);
			Assert.LessOrEqual(4, _opPre.Parameters.Count);

			foreach (var param in _opPre.Parameters)
			{
				Assert.IsTrue(param.DataElement.isMutable);
			}
		}

		[Test]
		public void TestRewriteUrl()
		{
			const string rewriteUrl = "http://location:9999";
			var url = string.Empty;

			_proxy.Options.Routes.Clear();
			_proxy.Options.Routes.Add(new WebProxyRoute
			{
				Url = "*/unknown?api/*",
				Mutate = true,
				BaseUrl = rewriteUrl
			});

			var client = GetHttpClient(null, (sender, e, op) =>
			{
				url = e.ProxySession.Request.Url;
				e.ProxySession.Request.RequestUri = new Uri(e.ProxySession.Request.Url.Replace(rewriteUrl, BaseUrl.Replace(".", "")));
			});

			var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.AreEqual("http://location:9999/unknown/api/values/5", url);
		}

		[Test]
		public void TestOnRequest()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">
		<Block/>
	</DataModel>

	<StateModel name=""TheStateModel"" initialState=""State1"">
		<State name=""State1"">
			<Action type=""output"">
				<DataModel ref=""TheDataModel""/>
			</Action>
		</State>
	</StateModel>

	<Test name=""Default"">
		<StateModel ref=""TheStateModel""/>
		<Publisher class=""Null""/>
	</Test>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var context = new RunContext {test = dom.tests[0],config = new RunConfiguration()};
			var engine = new Engine(new ConsoleWatcher());
			context.test.strategy = new WebProxyStrategy(new Dictionary<string, Variant>());
			context.test.strategy.Initialize(context, engine);

			var version = string.Empty;

			_proxy.Context = context;
			_proxy.Options.Routes.Clear();
			_proxy.Options.Routes.Add(new WebProxyRoute
			{
				Url = "*/unknown?api/*",
				Mutate = true,
				OnRequest= "setattr(request, 'HttpVersion', '1.6')"
			});

			var client = GetHttpClient(null, (sender, e, op) =>
			{
				version = e.ProxySession.Request.HttpVersion;
				e.ProxySession.Request.HttpVersion = "HTTP/1.1";
			});

			var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.AreEqual("1.6", version);
		}
	}
}
