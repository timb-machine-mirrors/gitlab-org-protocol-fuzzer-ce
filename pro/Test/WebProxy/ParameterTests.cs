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
using Peach.Core.Dom;
using Peach.Pro.Core.WebApi;
using Peach.Pro.Test.WebProxy.TestTarget;
using Peach.Pro.Test.WebProxy.TestTarget.Controllers;
using Titanium.Web.Proxy.EventArguments;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class ParameterTests : BaseRunTester
	{
		[Test]
		public void TestSwaggerPath()
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

			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/api/values/5").Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

			var op = (WebApiOperation) context.iterationStateStore["op"];

			Assert.NotNull(op);
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

			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/api/values?filter=foo").Result;
			var op = (WebApiOperation)context.iterationStateStore["op"];

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.NotNull(op.Path);
			Assert.NotNull(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.Query);

			Assert.AreEqual(WebApiParameterIn.Query, param.In);
			Assert.AreEqual("foo", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("foo", ValuesController.Filter);
		}

		[Test]
		public void TestSwaggerHeader()
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

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.GetAsync(BaseUrl + "/api/values").Result;
			var op = (WebApiOperation)context.iterationStateStore["op"];

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
			Assert.NotNull(op.Path);
			Assert.NotNull(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.Header && i.Name == "x-peachy");

			Assert.AreEqual(WebApiParameterIn.Header, param.In);
			Assert.AreEqual("Testing 1..2..3..", (string)param.DataElement.DefaultValue);
			Assert.AreEqual("Testing 1..2..3..", ValuesController.X_Peachy);
		}

		[Test]
		public void TestSwaggerFormData()
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

			var client = GetHttpClient();
			var response = client.PostAsync(BaseUrl + "/api/values", content).Result;
			var op = (WebApiOperation)context.iterationStateStore["op"];

			Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
			Assert.NotNull(op);
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

			var complexValue = new ComplexValue
			{
				extraValue = new Value() { value = "Hello extra value" },
				values = new[] { "A", "B", "C", "D" }
			};

			var content = new ObjectContent<ComplexValue>(complexValue, new JsonMediaTypeFormatter());

			var client = GetHttpClient();
			var response = client.PostAsync(BaseUrl + "/api/values/complex", content).Result;
			var op = (WebApiOperation)context.iterationStateStore["op"];

			Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
			Assert.NotNull(op);
			Assert.NotNull(op.Path);
			Assert.NotNull(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.Body);

			Assert.AreEqual(WebApiParameterIn.Body, param.In);
			Assert.AreEqual("{\"values\":[\"A\",\"B\",\"C\",\"D\"],\"extraValue\":{\"value\":\"Hello extra value\"}}",
				StreamVariantToString(param.DataElement.DefaultValue));
			Assert.AreEqual(complexValue.extraValue.value, ValuesController.ComplexValue.extraValue.value);
			Assert.AreEqual(complexValue.values.Length, ValuesController.ComplexValue.values.Length);

			for (var cnt = 0; cnt < complexValue.values.Length; cnt++)
				Assert.AreEqual(complexValue.values[cnt], ValuesController.ComplexValue.values[cnt]);
		}

		[Test]
		public void TestSwaggerXmlBodyData()
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

			var complexValue = new ComplexValue
			{
				extraValue = new Value() { value = "Hello extra value" },
				values = new[] { "A", "B", "C", "D" }
			};

			var content = new ObjectContent<ComplexValue>(complexValue, new XmlMediaTypeFormatter());

			var client = GetHttpClient();
			var response = client.PostAsync(BaseUrl + "/api/values/complex", content).Result;
			var op = (WebApiOperation)context.iterationStateStore["op"];

			Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
			Assert.NotNull(op);
			Assert.NotNull(op.Path);
			Assert.NotNull(op.ShadowOperation);
			Assert.LessOrEqual(1, op.Parameters.Count);

			var param = op.Parameters.First(i => i.In == WebApiParameterIn.Body);

			Assert.AreEqual(WebApiParameterIn.Body, param.In);
			Assert.AreEqual("<ComplexValue xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/Peach.Pro.Test.WebProxy.TestTarget.Controllers\"><extraValue><value>Hello extra value</value></extraValue><values xmlns:d2p1=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><d2p1:string>A</d2p1:string><d2p1:string>B</d2p1:string><d2p1:string>C</d2p1:string><d2p1:string>D</d2p1:string></values></ComplexValue>",
				StreamVariantToString(param.DataElement.DefaultValue));
			Assert.AreEqual(complexValue.extraValue.value, ValuesController.ComplexValue.extraValue.value);
			Assert.AreEqual(complexValue.values.Length, ValuesController.ComplexValue.values.Length);

			for (var cnt = 0; cnt < complexValue.values.Length; cnt++)
				Assert.AreEqual(complexValue.values[cnt], ValuesController.ComplexValue.values[cnt]);
		}

		[Test]
		public void TestSwaggerParameters()
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

			var client = GetHttpClient();
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

			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;
			var op = (WebApiOperation)context.iterationStateStore["op"];

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
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

			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/unknown/api/values?filter=foo").Result;
			var op = (WebApiOperation)context.iterationStateStore["op"];

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
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
		public void TestHeader()
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

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.GetAsync(BaseUrl + "/unknown/api/values").Result;
			var op = (WebApiOperation)context.iterationStateStore["op"];

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
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

			var client = GetHttpClient();
			var response = client.PostAsync(BaseUrl + "/unknown/api/values", content).Result;
			var op = (WebApiOperation)context.iterationStateStore["op"];

			Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
			Assert.NotNull(op);
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
		public void TestParameters()
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

			var client = GetHttpClient();
			var headers = client.DefaultRequestHeaders;
			headers.Add("X-Peachy", "Testing 1..2..3..");

			var response = client.PutAsync(BaseUrl + "/unknown/api/values/5?filter=foo", content).Result;
			var op = (WebApiOperation)context.iterationStateStore["op"];

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(op);
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
