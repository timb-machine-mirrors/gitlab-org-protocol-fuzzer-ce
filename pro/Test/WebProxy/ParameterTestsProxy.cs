using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Net.Sockets;
using NUnit.Framework;
using Peach.Core.Dom;
using Peach.Pro.Core.WebApi;
using Peach.Pro.Test.WebProxy.TestTarget.Controllers;
using Peach.Core.Test;
using Peach.Pro.Core.Dom;
using Titanium.Web.Proxy;
using Encoding = Peach.Core.Encoding;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class ParameterTestsProxy : BaseRunTester
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
		}

		[Test]
		public void TestSwaggerNullQuery()
		{
			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/api/values?%2ffoo&%2fbar").Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("GET", op.Method);
			Assert.NotNull(op.Path);
			Assert.NotNull(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var param = op.Parameters.Where(i => i.In == WebApiParameterIn.Query).ToList();

			Assert.AreEqual(2, param.Count);
			Assert.AreEqual(null, param[0].Key);
			Assert.AreEqual("/foo", (string)param[0].DataElement.DefaultValue);
			Assert.AreEqual(null, param[1].Key);
			Assert.AreEqual("/bar", (string)param[1].DataElement.DefaultValue);

			StringAssert.Contains("GET /api/values?%2ffoo&%2fbar", Requests[0]);
		}

		[Test]
		public void TestSwaggerOptionalQuery()
		{
			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/api/values").Result;
			var op = GetOp();
			var req = Requests[0];

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("GET", op.Method);
			Assert.NotNull(op.Path);
			Assert.NotNull(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			StringAssert.StartsWith("GET /api/values HTTP/1.1", req);
		}

		[Test]
		public void TestSwaggerOptionalHeaders()
		{
			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/api/values").Result;
			var op = GetOp();
			var req = Requests[0];

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("GET", op.Method);
			Assert.NotNull(op.Path);
			Assert.NotNull(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			StringAssert.DoesNotContain("x-peachy:", req);
		}

		[Test]
		public void TestSwaggerOptionalFormData()
		{
			var content = new FormUrlEncodedContent(new[] 
			{
				new KeyValuePair<string, string>("value", "foo")
			});

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.PostAsync(BaseUrl + "/api/values", content).Result;
			var op = GetOp();
			var req = Requests[0];

			Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("POST", op.Method);
			Assert.NotNull(op.Path);
			Assert.NotNull(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			StringAssert.StartsWith("POST /api/values HTTP/1.1", req);
			StringAssert.DoesNotContain("extra=", req);
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

			StringAssert.StartsWith("GET /api/values HTTP/1.1", Requests[0]);
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
		}

		[Test]
		public void TestDeleteQuery()
		{
			var client = GetHttpClient();
			var response = client.DeleteAsync(BaseUrl + "/unknown/api/values?filter=foo").Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("DELETE", op.Method);
			Assert.NotNull(op.Path);
			Assert.Null(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.Query);

			Assert.IsTrue(NoSwaggerValuesController.MethodDelete);
			Assert.AreEqual(WebApiParameterIn.Query, param.In);
			Assert.Null(param.ShadowParameter);
			Assert.AreEqual("foo", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("foo", NoSwaggerValuesController.Filter);
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
		}

		[Test]
		public void TestChunkedEncodingFormData()
		{
			var uri = new Uri(BaseUrl);
			var json = @"value=Foo+Bar";

			var msg = string.Format(@"POST {2}/unknown/api/values HTTP/1.1
Host: {0}:{1}
Transfer-Encoding: chunked
Content-Type: application/x-www-form-urlencoded

{3:X}
{4}
0


", uri.Host, uri.Port, BaseUrl, json.Length, json).Replace("\r", "").Replace("\n", "\r\n");

			var msgBuff = Encoding.ASCII.GetBytes(msg);
			var responseLine = string.Empty;

			using (var client = new TcpClient())
			{
				client.Connect("127.0.0.1", Port);
				client.GetStream().Write(msgBuff, 0, msgBuff.Length);
				client.GetStream().Flush();

				using (var reader = new StreamReader(client.GetStream()))
					responseLine = reader.ReadLine();
			}

			var op = GetOp();

			StringAssert.StartsWith("HTTP/1.1 201", responseLine);
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
		public void TestMultiPartFormData()
		{
			var content = new MultipartFormDataContent();
			var metadataContent = new ByteArrayContent(Encoding.ASCII.GetBytes("{\"foo\":\"bar\"}"));
			metadataContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
			content.Add(metadataContent);
			var imageContent = new ByteArrayContent(Encoding.ASCII.GetBytes("Hello World"));
			imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
			content.Add(imageContent, "image", "中時東自想図.jpg");
			var client = GetHttpClient();
			var response = client.PostAsync(BaseUrl + "/unknown/api/values", content).Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("POST", op.Method);
			Assert.NotNull(op.Path);
			Assert.Null(op.ShadowOperation);

			var elems = op.Action.outputData.SelectMany(d => d.dataModel.PreOrderTraverse().Select(e => e.FullFieldId)).ToList();

			var expected = new[]
			{
				"Path", "Path.unknown",
				"Path", "Path.api", 
				"Path", "Path.values",
				"Headers", "Headers.content-type",
				"Headers", "Headers.host",
				"Headers", "Headers.content-length",
				"Headers", "Headers.expect",
				"Headers", "Headers.connection",
				"Part.Headers", "Part.Headers.content-type",
				"Part.Headers", "Part.Headers.content-disposition",
				"Part", "Part.jsonBody", "Part.jsonBody.foo",
				"image.Headers", "image.Headers.content-type",
				"image.Headers", "image.Headers.content-disposition",
				"image", "image.unknownBody",
			};

			CollectionAssert.AreEqual(expected, elems);

			foreach (var item in op.Action.outputData)
			{
				switch (item.dataModel.FieldId)
				{
					case "Path":
						Assert.AreEqual(2083, item.MaxOutputSize);
						break;
					case "Headers":
					case "Part.Headers":
					case "image.Headers":
						Assert.AreEqual(16 * 1024, item.MaxOutputSize);
						break;
					case "Part":
					case "image":
						Assert.AreEqual(0, item.MaxOutputSize);
						break;
					default:
						Assert.Fail("Uknown dataModel name");
						break;
				}
			}
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
		}

		[Test]
		public void TestDotsInNames()
		{
			var content = new FormUrlEncodedContent(new[] 
			{
				new KeyValuePair<string, string>("val.lue", "Foo Bar")
			});

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X.Peachy", "Testing 1..2..3..");

			var response = client.PutAsync(BaseUrl + "/unknown/api.foo/values/5?filter=foo&foo.a=b", content).Result;
			var op = GetOp();

			Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
			Assert.NotNull(op);
			Assert.AreEqual("PUT", op.Method);
			Assert.NotNull(op.Path);
			Assert.Null(op.ShadowOperation);


			Assert.LessOrEqual(6, op.Parameters.Count);

			Assert.AreEqual("unknown", op.Parameters[0].Key);
			Assert.AreEqual("unknown", op.Parameters[0].DataElement.Name);
			Assert.AreEqual("api.foo", op.Parameters[1].Key);
			Assert.AreEqual("api_foo", op.Parameters[1].DataElement.Name);
			Assert.AreEqual("values", op.Parameters[2].Key);
			Assert.AreEqual("values", op.Parameters[2].DataElement.Name);
			Assert.AreEqual("5", op.Parameters[3].Key);
			Assert.AreEqual("n5", op.Parameters[3].DataElement.Name);
			Assert.AreEqual("filter", op.Parameters[4].Key);
			Assert.AreEqual("filter", op.Parameters[4].DataElement.Name);
			Assert.AreEqual("foo.a", op.Parameters[5].Key);
			Assert.AreEqual("foo_a", op.Parameters[5].DataElement.Name);
			Assert.AreEqual("X.Peachy", op.Parameters[6].Key);
			Assert.AreEqual("x_peachy", op.Parameters[6].DataElement.Name);
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
