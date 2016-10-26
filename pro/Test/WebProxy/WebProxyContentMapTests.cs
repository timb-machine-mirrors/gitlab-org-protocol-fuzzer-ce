using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Test;
using Peach.Pro.Core.MutationStrategies;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.WebApi;
using Assert = NUnit.Framework.Assert;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class WebProxyContentMapTests : BaseRunTester
	{
		[SetUp]
		public override void SetUp()
		{
			// Skip base setup
		}

		[Test]
		public void TestMapIs()
		{
			var xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='TextPlain'>
		<String name='FooBar' value='FooBar' token='true'/>
	</DataModel>
	<Test name='Default' maxOutputSize='65000'>
		<WebProxy>
			<ContentType is='peachy/text' ref='TextPlain'/>
			<Route url='*' baseUrl='{0}' /> 
		</WebProxy>
		<Strategy class='WebProxy' />
		<Publisher class='WebApiProxy'>
			<Param name='Port' value='0' />
		</Publisher>
	</Test>
</Peach>".Fmt(Server.Uri);

			RunEngine(xml);

			var content = new StringContent("FooBar");
			content.Headers.ContentType = new MediaTypeHeaderValue("peachy/text");

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.PutAsync(BaseUrl + "/unknown/api/values/5?filter=foo", content).Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
			Assert.NotNull(op);
			Assert.Null(op.ShadowOperation);
			Assert.LessOrEqual(4, op.Parameters.Count);
			var param = op.Parameters.FirstOrDefault(i => i.In == WebApiParameterIn.Body);
			Assert.NotNull(param);
			Assert.AreEqual("customBody", param.DataElement.Name);
			var e = ((DataElementContainer)param.DataElement)[0];
			Assert.AreEqual("FooBar", e.Name);
			Assert.IsTrue(e.isToken);
			Assert.AreEqual("FooBar", (string)e.DefaultValue);
		}

		[Test]
		public void TestMapStartsWith()
		{
			var xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='TextPlain'>
		<String name='FooBar' value='FooBar' token='true'/>
	</DataModel>
	<Test name='Default' maxOutputSize='65000'>
		<WebProxy>
			<ContentType startsWith='peachy' ref='TextPlain'/>
			<Route url='*' baseUrl='{0}' /> 
		</WebProxy>
		<Strategy class='WebProxy' />
		<Publisher class='WebApiProxy'>
			<Param name='Port' value='0' />
		</Publisher>
	</Test>
</Peach>".Fmt(Server.Uri);

			RunEngine(xml);

			var content = new StringContent("FooBar");
			content.Headers.ContentType = new MediaTypeHeaderValue("peachy/text");

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.PutAsync(BaseUrl + "/unknown/api/values/5?filter=foo", content).Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
			Assert.NotNull(op);
			Assert.Null(op.ShadowOperation);
			Assert.LessOrEqual(4, op.Parameters.Count);
			var param = op.Parameters.FirstOrDefault(i => i.In == WebApiParameterIn.Body);
			Assert.NotNull(param);
			Assert.AreEqual("customBody", param.DataElement.Name);
			var e = ((DataElementContainer)param.DataElement)[0];
			Assert.AreEqual("FooBar", e.Name);
			Assert.IsTrue(e.isToken);
			Assert.AreEqual("FooBar", (string)e.DefaultValue);
		}

	}
}
