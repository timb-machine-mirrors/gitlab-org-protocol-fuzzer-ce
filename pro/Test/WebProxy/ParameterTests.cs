using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Pro.Core.Analyzers.WebApi;
using Peach.Pro.Test.WebProxy.TestTarget;
using Peach.Pro.Test.WebProxy.TestTarget.Controllers;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class ParameterTests : BaseTester
	{
		[Test]
		public void TestSwaggerPath()
		{
			_op = null;

			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/api/values/5").Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(_op);
			Assert.NotNull(_op.ShadowOperation);
			Assert.LessOrEqual(1, _op.Parameters.Count);

			var param = _op.Parameters.First(i => i.In == WebApiParameterIn.Path);

			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.AreEqual("5", (string)param.DataElement.DefaultValue);
			Assert.AreEqual(5, ValuesController.Id);
		}

		[Test]
		public void TestSwaggerQuery()
		{
			_op = null;

			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/api/values?filter=foo").Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(_op);
			Assert.NotNull(_op.ShadowOperation);
			Assert.LessOrEqual(1, _op.Parameters.Count);

			var param = _op.Parameters.First(i => i.In == WebApiParameterIn.Query);

			Assert.AreEqual(WebApiParameterIn.Query, param.In);
			Assert.AreEqual("foo", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("foo", ValuesController.Filter);
		}

		[Test]
		public void TestSwaggerHeader()
		{
			_op = null;

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.GetAsync(BaseUrl + "/api/values").Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(_op);
			Assert.NotNull(_op.ShadowOperation);
			Assert.LessOrEqual(1, _op.Parameters.Count);

			var param = _op.Parameters.First(i => i.In == WebApiParameterIn.Header && i.Name == "x-peachy");

			Assert.AreEqual(WebApiParameterIn.Header, param.In);
			Assert.AreEqual("Testing 1..2..3..", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Testing 1..2..3..", ValuesController.X_Peachy);
		}

		[Test]
		public void TestSwaggerFormData()
		{
			_op = null;

			var content = new FormUrlEncodedContent(new[] 
			{
				new KeyValuePair<string, string>("value", "Foo Bar")
			});

			var client = GetHttpClient();
			var response = client.PostAsync(BaseUrl + "/api/values", content).Result;

			Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
			Assert.NotNull(_op);
			Assert.NotNull(_op.ShadowOperation);
			Assert.LessOrEqual(1, _op.Parameters.Count);

			var param = _op.Parameters.First(i => i.In == WebApiParameterIn.FormData);

			Assert.AreEqual(WebApiParameterIn.FormData, param.In);
			Assert.AreEqual("Foo Bar", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Foo Bar", ValuesController.Value);
		}

		[Test]
		public void TestSwaggerParameters()
		{
			_op = null;

			var content = new FormUrlEncodedContent(new[] 
			{
				new KeyValuePair<string, string>("value", "Foo Bar")
			});

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.PutAsync(BaseUrl + "/api/values/5?filter=foo", content).Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(_op);
			Assert.NotNull(_op.ShadowOperation);
			Assert.LessOrEqual(4, _op.Parameters.Count);

			var param = _op.Parameters.First(i => i.In == WebApiParameterIn.Path);
			Assert.NotNull(param);
			Assert.NotNull(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.AreEqual("5", (string)param.DataElement.DefaultValue);
			Assert.AreEqual(5, ValuesController.Id);

			param = _op.Parameters.First(i => i.In == WebApiParameterIn.Query);
			Assert.NotNull(param);
			Assert.NotNull(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Query, param.In);
			Assert.AreEqual("foo", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("foo", ValuesController.Filter);

			param = _op.Parameters.First(i => i.In == WebApiParameterIn.Header);
			Assert.NotNull(param);
			Assert.NotNull(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Header, param.In);
			Assert.AreEqual("Testing 1..2..3..", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Testing 1..2..3..", ValuesController.X_Peachy);

			param = _op.Parameters.First(i => i.In == WebApiParameterIn.FormData);
			Assert.NotNull(param);
			Assert.NotNull(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.FormData, param.In);
			Assert.AreEqual("Foo Bar", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Foo Bar", ValuesController.Value);
		}

		[Test]
		public void TestPath()
		{
			_op = null;

			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(_op);
			Assert.NotNull(_op.ShadowOperation);
			Assert.LessOrEqual(1, _op.Parameters.Count);

			var param = _op.Parameters.First(i => i.In == WebApiParameterIn.Path);

			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.AreEqual("5", (string)param.DataElement.DefaultValue);
			Assert.AreEqual(5, NoSwaggerValuesController.Id);
		}

		[Test]
		public void TestQuery()
		{
			_op = null;

			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/unknown/api/values?filter=foo").Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(_op);
			Assert.NotNull(_op.ShadowOperation);
			Assert.LessOrEqual(1, _op.Parameters.Count);

			var param = _op.Parameters.First(i => i.In == WebApiParameterIn.Query);

			Assert.AreEqual(WebApiParameterIn.Query, param.In);
			Assert.AreEqual("foo", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("foo", NoSwaggerValuesController.Filter);
		}

		[Test]
		public void TestHeader()
		{
			_op = null;

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.GetAsync(BaseUrl + "/unknown/api/values").Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(_op);
			Assert.NotNull(_op.ShadowOperation);
			Assert.LessOrEqual(1, _op.Parameters.Count);

			var param = _op.Parameters.First(i => i.In == WebApiParameterIn.Header && i.Name == "x-peachy");

			Assert.AreEqual(WebApiParameterIn.Header, param.In);
			Assert.AreEqual("Testing 1..2..3..", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Testing 1..2..3..", NoSwaggerValuesController.X_Peachy);
		}

		[Test]
		public void TestFormData()
		{
			_op = null;

			var content = new FormUrlEncodedContent(new[] 
			{
				new KeyValuePair<string, string>("value", "Foo Bar")
			});

			var client = GetHttpClient();
			var response = client.PostAsync(BaseUrl + "/unknown/api/values", content).Result;

			Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
			Assert.NotNull(_op);
			Assert.NotNull(_op.ShadowOperation);
			Assert.LessOrEqual(1, _op.Parameters.Count);

			var param = _op.Parameters.First(i => i.In == WebApiParameterIn.FormData);

			Assert.AreEqual(WebApiParameterIn.FormData, param.In);
			Assert.AreEqual("Foo Bar", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Foo Bar", NoSwaggerValuesController.Value);
		}

		[Test]
		public void TestParameters()
		{
			_op = null;

			var content = new FormUrlEncodedContent(new[] 
			{
				new KeyValuePair<string, string>("value", "Foo Bar")
			});

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.PutAsync(BaseUrl + "/unknown/api/values/5?filter=foo", content).Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(_op);
			Assert.NotNull(_op.ShadowOperation);
			Assert.LessOrEqual(4, _op.Parameters.Count);

			var param = _op.Parameters.First(i => i.In == WebApiParameterIn.Path);
			Assert.NotNull(param);
			Assert.NotNull(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.AreEqual("5", (string)param.DataElement.DefaultValue);
			Assert.AreEqual(5, NoSwaggerValuesController.Id);

			param = _op.Parameters.First(i => i.In == WebApiParameterIn.Query);
			Assert.NotNull(param);
			Assert.NotNull(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Query, param.In);
			Assert.AreEqual("foo", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("foo", NoSwaggerValuesController.Filter);

			param = _op.Parameters.First(i => i.In == WebApiParameterIn.Header);
			Assert.NotNull(param);
			Assert.NotNull(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Header, param.In);
			Assert.AreEqual("Testing 1..2..3..", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Testing 1..2..3..", NoSwaggerValuesController.X_Peachy);

			param = _op.Parameters.First(i => i.In == WebApiParameterIn.FormData);
			Assert.NotNull(param);
			Assert.NotNull(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.FormData, param.In);
			Assert.AreEqual("Foo Bar", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Foo Bar", NoSwaggerValuesController.Value);
		}
	}
}
