using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;
using Peach.Core.Dom;
using Peach.Pro.Core.WebApi;
using Peach.Pro.Test.WebProxy.TestTarget.Controllers;
using Peach.Core.Test;
using Peach.Pro.Core.Dom;
using Peach.Pro.Test.Core;
using Encoding = Peach.Core.Encoding;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class ParameterTestsRecord : BaseRunTesterRecord
	{
		[Test]
		public void TestSwaggerPath()
		{
			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/api/values/5").Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

			var op = GetOp();

			Assert.NotNull(op);
			Assert.AreEqual("GET", op.Method);
			Assert.NotNull(op.Path);
			Assert.NotNull(op.ShadowOperation);
			Assert.AreEqual("/api/values/{id}", op.Path.Path);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.Path);

			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.AreEqual("5", (string)param.DataElement.DefaultValue);
			Assert.AreEqual(5, ValuesController.Id);

			var dom = OperationsToDom();
			Assert.NotNull(dom);
			var pit = SerializeOperations(dom);
			Assert.NotNull(pit);
			var rtt = TestBase.ParsePit(pit);
			Assert.NotNull(rtt);
		}

		[Test]
		public void TestSwaggerQuery()
		{
			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/api/values?filter=foo").Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("GET", op.Method);
			Assert.NotNull(op.Path);
			Assert.NotNull(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.Query);

			Assert.AreEqual(WebApiParameterIn.Query, param.In);
			Assert.AreEqual("foo", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("foo", ValuesController.Filter);

			var dom = OperationsToDom();
			Assert.NotNull(dom);
			var pit = SerializeOperations(dom);
			Assert.NotNull(pit);
			var rtt = TestBase.ParsePit(pit);
			Assert.NotNull(rtt);
		}

		[Test]
		public void TestSwaggerHeader()
		{
			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.GetAsync(BaseUrl + "/api/values").Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("GET", op.Method);
			Assert.NotNull(op.Path);
			Assert.NotNull(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.Header && i.Name == "x-peachy");

			Assert.AreEqual(WebApiParameterIn.Header, param.In);
			Assert.AreEqual("Testing 1..2..3..", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Testing 1..2..3..", ValuesController.X_Peachy);

			var dom = OperationsToDom();
			Assert.NotNull(dom);
			var pit = SerializeOperations(dom);
			Assert.NotNull(pit);
			var rtt = TestBase.ParsePit(pit);
			Assert.NotNull(rtt);
		}

		[Test]
		public void TestSwaggerFormData()
		{
			var content = new FormUrlEncodedContent(new[] 
			{
				new KeyValuePair<string, string>("value", "Foo Bar")
			});

			var client = GetHttpClient();
			var response = client.PostAsync(BaseUrl + "/api/values", content).Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("POST", op.Method);
			Assert.NotNull(op.Path);
			Assert.NotNull(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.FormData);

			Assert.AreEqual(WebApiParameterIn.FormData, param.In);
			Assert.AreEqual("Foo Bar", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Foo Bar", ValuesController.Value);

			var dom = OperationsToDom();
			Assert.NotNull(dom);
			var pit = SerializeOperations(dom);
			Assert.NotNull(pit);
			var rtt = TestBase.ParsePit(pit);
			Assert.NotNull(rtt);
		}

		[Test]
		public void TestSwaggerJsonBodyData()
		{
			var complexValue = new ComplexValue
			{
				extraValue = new Value() { value = "Hello extra value" },
				values = new[] { "A", "B", "C", "D" }
			};

			var content = new ObjectContent<ComplexValue>(complexValue, new JsonMediaTypeFormatter());

			var client = GetHttpClient();
			var response = client.PostAsync(BaseUrl + "/api/values/complex", content).Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("POST", op.Method);
			Assert.NotNull(op.Path);
			Assert.NotNull(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.Body);

			Assert.AreEqual(WebApiParameterIn.Body, param.In);
			Assert.IsTrue(param.DataElement is JsonObject);
			Assert.AreEqual("{\"values\":[\"A\",\"B\",\"C\",\"D\"],\"extraValue\":{\"value\":\"Hello extra value\",\"extra\":null,\"a\":null}}",
				(string)param.DataElement.DefaultValue);
			Assert.AreEqual((string)param.DataElement.DefaultValue, (string)param.DataElement.InternalValue);
			Assert.AreEqual((string)param.DataElement.DefaultValue, param.DataElement.Value.BitsToString());
			Assert.AreEqual(complexValue.extraValue.value, ValuesController.ComplexValue.extraValue.value);
			Assert.AreEqual(complexValue.values.Length, ValuesController.ComplexValue.values.Length);

			for (var cnt = 0; cnt < complexValue.values.Length; cnt++)
				Assert.AreEqual(complexValue.values[cnt], ValuesController.ComplexValue.values[cnt]);

			var dom = OperationsToDom();
			Assert.NotNull(dom);
			var pit = SerializeOperations(dom);
			Assert.NotNull(pit);
			var rtt = TestBase.ParsePit(pit);
			Assert.NotNull(rtt);
		}

		[Test]
		public void TestSwaggerXmlBodyData()
		{
			var complexValue = new ComplexValue
			{
				extraValue = new Value() { value = "Hello extra value" },
				values = new[] { "A", "B", "C", "D" }
			};

			var content = new ObjectContent<ComplexValue>(complexValue, new XmlMediaTypeFormatter());

			var client = GetHttpClient();
			var response = client.PostAsync(BaseUrl + "/api/values/complex", content).Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("POST", op.Method);
			Assert.NotNull(op.Path);
			Assert.NotNull(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.Body);

			Assert.AreEqual(WebApiParameterIn.Body, param.In);
			Assert.IsTrue(param.DataElement is XmlElement);
			Assert.AreEqual("<ComplexValue xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/Peach.Pro.Test.WebProxy.TestTarget.Controllers\"><extraValue><a xmlns:d3p1=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" i:nil=\"true\" /><extra i:nil=\"true\" /><value>Hello extra value</value></extraValue><values xmlns:d2p1=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><d2p1:string>A</d2p1:string><d2p1:string>B</d2p1:string><d2p1:string>C</d2p1:string><d2p1:string>D</d2p1:string></values></ComplexValue>",
				(string)param.DataElement.DefaultValue);
			Assert.AreEqual((string)param.DataElement.DefaultValue, param.DataElement.InternalValue.BitsToString());
			Assert.AreEqual((string)param.DataElement.DefaultValue, param.DataElement.Value.BitsToString());
			Assert.AreEqual(complexValue.extraValue.value, ValuesController.ComplexValue.extraValue.value);
			Assert.AreEqual(complexValue.values.Length, ValuesController.ComplexValue.values.Length);

			for (var cnt = 0; cnt < complexValue.values.Length; cnt++)
				Assert.AreEqual(complexValue.values[cnt], ValuesController.ComplexValue.values[cnt]);

			var dom = OperationsToDom();
			Assert.NotNull(dom);
			var pit = SerializeOperations(dom);
			Assert.NotNull(pit);
			var rtt = TestBase.ParsePit(pit);
			Assert.NotNull(rtt);
		}

		[Test]
		public void TestSwaggerParameters()
		{
			var content = new FormUrlEncodedContent(new[] 
			{
				new KeyValuePair<string, string>("value", "Foo Bar")
			});

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.PutAsync(BaseUrl + "/api/values/5?filter=foo", content).Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("PUT", op.Method);
			Assert.NotNull(op.ShadowOperation);
			Assert.LessOrEqual(4, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.Path);
			Assert.NotNull(param);
			Assert.NotNull(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.AreEqual("5", (string)param.DataElement.DefaultValue);
			Assert.AreEqual(5, ValuesController.Id);

			param = op.Parameters.First(i => i.In == WebApiParameterIn.Query);
			Assert.NotNull(param);
			Assert.NotNull(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Query, param.In);
			Assert.AreEqual("foo", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("foo", ValuesController.Filter);

			param = op.Parameters.First(i => i.In == WebApiParameterIn.Header);
			Assert.NotNull(param);
			Assert.NotNull(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Header, param.In);
			Assert.AreEqual("Testing 1..2..3..", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Testing 1..2..3..", ValuesController.X_Peachy);

			param = op.Parameters.First(i => i.In == WebApiParameterIn.FormData);
			Assert.NotNull(param);
			Assert.NotNull(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.FormData, param.In);
			Assert.AreEqual("Foo Bar", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Foo Bar", ValuesController.Value);

			var dom = OperationsToDom();
			Assert.NotNull(dom);
			var pit = SerializeOperations(dom);
			Assert.NotNull(pit);
			var rtt = TestBase.ParsePit(pit);
			Assert.NotNull(rtt);
		}


		[Test]
		public void TestPath()
		{
			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("GET", op.Method);
			Assert.NotNull(op.Path);
			Assert.Null(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.Path && i.Name == "5");

			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual("5", (string)param.DataElement.DefaultValue);
			Assert.AreEqual(5, NoSwaggerValuesController.Id);

			var dom = OperationsToDom();
			Assert.NotNull(dom);
			var pit = SerializeOperations(dom);
			Assert.NotNull(pit);
			var rtt = TestBase.ParsePit(pit);
			Assert.NotNull(rtt);
		}

		[Test]
		public void TestQuery()
		{
			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/unknown/api/values?filter=foo").Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("GET", op.Method);
			Assert.NotNull(op.Path);
			Assert.Null(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.Query);

			Assert.AreEqual(WebApiParameterIn.Query, param.In);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual("foo", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("foo", NoSwaggerValuesController.Filter);

			var dom = OperationsToDom();
			Assert.NotNull(dom);
			var pit = SerializeOperations(dom);
			Assert.NotNull(pit);
			var rtt = TestBase.ParsePit(pit);
			Assert.NotNull(rtt);
		}

		[Test]
		public void TestDuplicateQuery()
		{
			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/unknown/api/values?filter=foo&filter=bar").Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("GET", op.Method);
			Assert.NotNull(op.Path);
			Assert.Null(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var queryParams = op.Parameters.Where(i => i.In == WebApiParameterIn.Query).ToArray();

			Assert.AreEqual(2, queryParams.Length);

			var param = queryParams[0];

			Assert.AreEqual(WebApiParameterIn.Query, param.In);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual("filter", param.Key);
			Assert.AreEqual("filter", param.Name);
			Assert.AreEqual("foo", (string)param.DataElement.DefaultValue);

			param = queryParams[1];

			Assert.AreEqual(WebApiParameterIn.Query, param.In);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual("filter", param.Key);
			Assert.AreEqual("filter_1", param.Name);
			Assert.AreEqual("bar", (string)param.DataElement.DefaultValue);

			var dom = OperationsToDom();
			Assert.NotNull(dom);
			var pit = SerializeOperations(dom);
			Assert.NotNull(pit);
			var rtt = TestBase.ParsePit(pit);
			Assert.NotNull(rtt);
		}

		[Test]
		public void TestDuplicatePath()
		{
			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/unknown/api/values/values").Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("GET", op.Method);
			Assert.NotNull(op.Path);
			Assert.Null(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var pathParams = op.Parameters.Where(i => i.In == WebApiParameterIn.Path).ToArray();

			Assert.AreEqual(4, pathParams.Length);

			var param = pathParams[0];

			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual("unknown", param.Key);
			Assert.AreEqual("unknown", param.Name);
			Assert.AreEqual("unknown", (string)param.DataElement.DefaultValue);

			param = pathParams[1];

			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.Null(param.ShadowParameter);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual("api", param.Key);
			Assert.AreEqual("api", param.Name);
			Assert.AreEqual("api", (string)param.DataElement.DefaultValue);

			param = pathParams[2];

			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.Null(param.ShadowParameter);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual("values", param.Key);
			Assert.AreEqual("values", param.Name);
			Assert.AreEqual("values", (string)param.DataElement.DefaultValue);


			param = pathParams[3];

			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.Null(param.ShadowParameter);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual("values", param.Key);
			Assert.AreEqual("values_1", param.Name);
			Assert.AreEqual("values", (string)param.DataElement.DefaultValue);

			var dom = OperationsToDom();
			Assert.NotNull(dom);
			var pit = SerializeOperations(dom);
			Assert.NotNull(pit);
			var rtt = TestBase.ParsePit(pit);
			Assert.NotNull(rtt);
		}

		[Test]
		public void TestDuplicateHeaders()
		{
			var c = new TcpClient();

			c.Connect(IPAddress.Loopback, Port);

			var msg = new[]
			{
				"GET /unknown/api/values/values",
				"Host: sometesthost",
				"X-Peach: Testing 1..2..3..",
				"X-Peach: Testing 4..5..6..",
				"",
				"",
			};

			var buf = Encoding.ASCII.GetBytes(string.Join("\r\n", msg));

			c.Client.Send(buf);

			string val;

			using (var rdr = new StreamReader(c.GetStream()))
			{
				val = rdr.ReadLine();
			}

			Assert.NotNull(val);

			var op = GetOp();

			Assert.AreEqual("HTTP/1.1 200 OK", val);
			Assert.NotNull(op);
			Assert.AreEqual("GET", op.Method);
			Assert.NotNull(op.Path);
			Assert.Null(op.ShadowOperation);
			Assert.AreEqual(7, op.Parameters.Count);

			var param = op.Parameters[0];
			Assert.NotNull(param);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.AreEqual("unknown", (string)param.DataElement.DefaultValue);

			param = op.Parameters[1];
			Assert.NotNull(param);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.AreEqual("api", (string)param.DataElement.DefaultValue);

			param = op.Parameters[2];
			Assert.NotNull(param);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.AreEqual("values", (string)param.DataElement.DefaultValue);

			param = op.Parameters[3];
			Assert.NotNull(param);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.AreEqual("values", (string)param.DataElement.DefaultValue);

			param = op.Parameters[4];
			Assert.NotNull(param);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Header, param.In);
			Assert.AreEqual("Host", param.Key);
			Assert.AreEqual("sometesthost", (string)param.DataElement.DefaultValue);

			param = op.Parameters[5];
			Assert.NotNull(param);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Header, param.In);
			Assert.AreEqual("X-Peach", param.Key);
			Assert.AreEqual("x-peach", param.Name);
			Assert.AreEqual("Testing 1..2..3..", (string)param.DataElement.DefaultValue);

			param = op.Parameters[6];
			Assert.NotNull(param);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Header, param.In);
			Assert.AreEqual("X-Peach", param.Key);
			Assert.AreEqual("x-peach_1", param.Name);
			Assert.AreEqual("Testing 4..5..6..", (string)param.DataElement.DefaultValue);

			var dom = OperationsToDom();
			Assert.NotNull(dom);
			var pit = SerializeOperations(dom);
			Assert.NotNull(pit);
			var rtt = TestBase.ParsePit(pit);
			Assert.NotNull(rtt);
		}

		[Test]
		public void TestQueryArray()
		{
			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/unknown/api/values?a=b,b&a=c&a=d").Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("GET", op.Method);
			Assert.NotNull(op.Path);
			Assert.Null(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var p = op.Parameters.Where(i => i.In == WebApiParameterIn.Query).ToArray();

			Assert.AreEqual(3, p.Length);

			var param = p[0];

			Assert.AreEqual(WebApiParameterIn.Query, param.In);
			Assert.Null(param.ShadowParameter);

			Assert.AreEqual("b,b", (string)p[0].DataElement.DefaultValue);
			Assert.AreEqual("c", (string)p[1].DataElement.DefaultValue);
			Assert.AreEqual("d", (string)p[2].DataElement.DefaultValue);

			Assert.AreEqual("b,b", NoSwaggerValuesController.ArrayValue[0]);
			Assert.AreEqual("c", NoSwaggerValuesController.ArrayValue[1]);
			Assert.AreEqual("d", NoSwaggerValuesController.ArrayValue[2]);
		}

		[Test]
		public void TestHeader()
		{
			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.GetAsync(BaseUrl + "/unknown/api/values").Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("GET", op.Method);
			Assert.NotNull(op.Path);
			Assert.Null(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.Header && i.Name == "x-peachy");

			Assert.AreEqual(WebApiParameterIn.Header, param.In);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual("Testing 1..2..3..", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Testing 1..2..3..", NoSwaggerValuesController.X_Peachy);

			var dom = OperationsToDom();
			Assert.NotNull(dom);
			var pit = SerializeOperations(dom);
			Assert.NotNull(pit);
			var rtt = TestBase.ParsePit(pit);
			Assert.NotNull(rtt);
		}

		[Test]
		public void TestFormData()
		{
			var content = new FormUrlEncodedContent(new[] 
			{
				new KeyValuePair<string, string>("value", "Foo Bar")
			});

			var client = GetHttpClient();
			var response = client.PostAsync(BaseUrl + "/unknown/api/values", content).Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("POST", op.Method);
			Assert.NotNull(op.Path);
			Assert.Null(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.FormData);

			Assert.AreEqual(WebApiParameterIn.FormData, param.In);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual("Foo Bar", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Foo Bar", NoSwaggerValuesController.Value);

			var dom = OperationsToDom();
			Assert.NotNull(dom);
			var pit = SerializeOperations(dom);
			Assert.NotNull(pit);
			var rtt = TestBase.ParsePit(pit);
			Assert.NotNull(rtt);
		}

		[Test]
		public void TestFormDataArray()
		{
			var content = new FormUrlEncodedContent(new[] 
			{
				new KeyValuePair<string, string>("a", "b,b"),
				new KeyValuePair<string, string>("a", "c"),
				new KeyValuePair<string, string>("a", "d")
			});

			var client = GetHttpClient();
			var response = client.PostAsync(BaseUrl + "/unknown/api/values", content).Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("POST", op.Method);
			Assert.NotNull(op.Path);
			Assert.Null(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var p = op.Parameters.Where(i => i.In == WebApiParameterIn.FormData).ToArray();

			Assert.AreEqual(3, p.Length);

			var param = p[0];

			Assert.AreEqual(WebApiParameterIn.FormData, param.In);
			Assert.Null(param.ShadowParameter);

			Assert.AreEqual("b,b", (string)p[0].DataElement.DefaultValue);
			Assert.AreEqual("c", (string)p[1].DataElement.DefaultValue);
			Assert.AreEqual("d", (string)p[2].DataElement.DefaultValue);

			Assert.AreEqual("b,b", NoSwaggerValuesController.ArrayValue[0]);
			Assert.AreEqual("c", NoSwaggerValuesController.ArrayValue[1]);
			Assert.AreEqual("d", NoSwaggerValuesController.ArrayValue[2]);
		}

		[Test]
		public void TestParameters()
		{
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
			Assert.AreEqual("PUT", op.Method);
			Assert.NotNull(op.Path);
			Assert.Null(op.ShadowOperation);
			Assert.LessOrEqual(4, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.Path && i.Name == "5");
			Assert.NotNull(param);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.AreEqual("5", (string)param.DataElement.DefaultValue);
			Assert.AreEqual(5, NoSwaggerValuesController.Id);

			param = op.Parameters.First(i => i.In == WebApiParameterIn.Query);
			Assert.NotNull(param);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Query, param.In);
			Assert.AreEqual("foo", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("foo", NoSwaggerValuesController.Filter);

			param = op.Parameters.First(i => i.In == WebApiParameterIn.Header);
			Assert.NotNull(param);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Header, param.In);
			Assert.AreEqual("Testing 1..2..3..", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Testing 1..2..3..", NoSwaggerValuesController.X_Peachy);

			param = op.Parameters.First(i => i.In == WebApiParameterIn.FormData);
			Assert.NotNull(param);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.FormData, param.In);
			Assert.AreEqual("Foo Bar", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Foo Bar", NoSwaggerValuesController.Value);

			var dom = OperationsToDom();
			Assert.NotNull(dom);
			var pit = SerializeOperations(dom);
			Assert.NotNull(pit);
			var rtt = TestBase.ParsePit(pit);
			Assert.NotNull(rtt);
		}

		/*
		[Test]
		public void TestChangingParameters()
		{
						var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<Test name=""Default"" maxOutputSize=""65000"">
		<WebProxy>
			<Route url=""*"" swagger=""" + SwaggerFile + @""" onRequest=""context.iterationStateStore.Add('op', op)"" /> 
		</WebProxy>
		<Strategy class=""WebProxy"" />
		<Publisher class=""WebApiProxy"" />
	</Test>
</Peach>";

			Task.Run(() =>
			{
				try
				{
					RunEngine(xml);
				}
				catch (Exception)
				{
					System.Diagnostics.Debugger.Break();
				}
			});

			Thread.Sleep(2000);

			var context = dom.context;

			var content = new FormUrlEncodedContent(new[] 
			{
				new KeyValuePair<string, string>("value", "Foo Bar")
			});

			var client = GetHttpClient((s, e, o) =>
			{
				var p = o.Parameters.First(i => i.In == WebApiParameterIn.Path);
				Assert.AreEqual("5", (string)p.DataElement.DefaultValue);
				p.DataElement.MutatedValue = new Variant("100");

				p = o.Parameters.First(i => i.In == WebApiParameterIn.Query);
				Assert.AreEqual("foo", (string)p.DataElement.DefaultValue);
				p.DataElement.MutatedValue = new Variant("Query_Modified");

				p = o.Parameters.First(i => i.In == WebApiParameterIn.Header);
				Assert.AreEqual("Testing 1..2..3..", (string)p.DataElement.DefaultValue);
				p.DataElement.MutatedValue = new Variant("Header_Modified");

				p = o.Parameters.First(i => i.In == WebApiParameterIn.FormData);
				Assert.AreEqual("Foo Bar", (string)p.DataElement.DefaultValue);
				p.DataElement.MutatedValue = new Variant("Form_Modified");
			});

			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.PutAsync(BaseUrl + "/api/values/5?filter=foo", content).Result;
			var op = (WebApiOperation)context.iterationStateStore["op"];

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.NotNull(op.ShadowOperation);
			Assert.LessOrEqual(4, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.Path);
			Assert.NotNull(param);
			Assert.NotNull(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Path, param.In);
			Assert.AreEqual("100", (string)param.DataElement.MutatedValue);
			Assert.AreEqual(100, ValuesController.Id);

			param = op.Parameters.First(i => i.In == WebApiParameterIn.Query);
			Assert.NotNull(param);
			Assert.NotNull(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Query, param.In);
			Assert.AreEqual("Query_Modified", (string)param.DataElement.MutatedValue);
			Assert.AreEqual("Query_Modified", ValuesController.Filter);

			param = op.Parameters.First(i => i.In == WebApiParameterIn.Header);
			Assert.NotNull(param);
			Assert.NotNull(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.Header, param.In);
			Assert.AreEqual("Header_Modified", (string)param.DataElement.MutatedValue);
			Assert.AreEqual("Header_Modified", ValuesController.X_Peachy);

			param = op.Parameters.First(i => i.In == WebApiParameterIn.FormData);
			Assert.NotNull(param);
			Assert.NotNull(param.ShadowParameter);
			Assert.AreEqual(WebApiParameterIn.FormData, param.In);
			Assert.AreEqual("Form_Modified", (string)param.DataElement.MutatedValue);
			Assert.AreEqual("Form_Modified", ValuesController.Value);
		}
		 */
	}
}
