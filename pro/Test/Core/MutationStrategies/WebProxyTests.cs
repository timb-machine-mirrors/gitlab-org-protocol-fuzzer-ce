using System.Collections.Generic;
using System.IO;
using System.Xml;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.MutationStrategies;
using Peach.Pro.Core.WebApi;

namespace Peach.Pro.Test.Core.MutationStrategies
{
	class WebApiData : ActionData
	{
		
	}

	[TestFixture]
	[Peach]
	[Quick]
	internal class WebProxyTests
	{
		[Test]
		public void TestCreate()
		{
			var ctx = new RunContext
			{
				config = new RunConfiguration(),
				test = new Peach.Core.Dom.Test()
			};

			var strategy = new WebProxyStrategy(null);

			strategy.Initialize(ctx, null);

			strategy.Finalize(ctx, null);
		}

		[Test]
		public void TestDeserialize()
		{
			const string xml = @"
<WebProxy>
	<Route url='*' />
	<Route url='/foo/bar' mutate='false' swagger='/tmp/swagger/json' baseUrl='google.com' faultOnStatusCodes='500,501' />
</WebProxy>
";

			var obj = XmlTools.Deserialize<WebProxyOptions>(new StringReader(xml));

			Assert.NotNull(obj);
			Assert.NotNull(obj.Routes);
			Assert.AreEqual(2, obj.Routes.Count);
			Assert.NotNull(obj.Routes[0]);
			Assert.AreEqual("*", obj.Routes[0].Url);
			Assert.AreEqual(true, obj.Routes[0].Mutate);
			Assert.NotNull(obj.Routes[1]);
			Assert.AreEqual("/foo/bar", obj.Routes[1].Url);
			Assert.AreEqual(false, obj.Routes[1].Mutate);
			Assert.AreEqual("/tmp/swagger/json", obj.Routes[1].Swagger);
			Assert.AreEqual("google.com", obj.Routes[1].BaseUrl);
			Assert.AreEqual(2, obj.Routes[1].FaultOnStatusCodes.Count);
			Assert.AreEqual(500, obj.Routes[1].FaultOnStatusCodes[0]);
			Assert.AreEqual(501, obj.Routes[1].FaultOnStatusCodes[1]);
		}

		[Test]
		public void TestSerialize()
		{
			const string xml = @"
<Peach>
	<Test name='Default'>
		<WebProxy>
			<Route url='*' />
			<Route url='/foo/bar' mutate='false' baseUrl='google.com' faultOnStatusCodes='500,501' />
		</WebProxy>

		<Publisher class='Null' />
	</Test>
</Peach>";


			var dom = ParsePit(xml);

			Assert.NotNull(dom);

			var settings = new XmlWriterSettings
			{
				Encoding = System.Text.Encoding.UTF8,
				Indent = true
			};

			using (var sout = new MemoryStream())
			{
				using (var xmlWriter = XmlWriter.Create(sout, settings))
				{
					xmlWriter.WriteStartDocument();

					dom.tests[0].WritePit(xmlWriter);

					xmlWriter.WriteEndDocument();
				}

				var pitOut = Encoding.UTF8.GetString(sout.ToArray(), 3, (int)sout.Length-3);
				//File.WriteAllText(@"c:\temp\pitout.xml", pitOut);

				Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Test name=\"Default\" maxOutputSize=\"1073741824\">\r\n  <WebProxy>\r\n    <Route url=\"*\" swagger=\"\" onRequest=\"\" mutate=\"True\" baseUrl=\"\" faultOnStateCodes=\"\" />\r\n    <Route url=\"/foo/bar\" swagger=\"\" onRequest=\"\" mutate=\"False\" baseUrl=\"google.com\" faultOnStateCodes=\"500,501\" />\r\n    <Route url=\"*\" swagger=\"\" onRequest=\"\" mutate=\"False\" baseUrl=\"\" faultOnStateCodes=\"500,501\" />\r\n  </WebProxy>\r\n</Test>", pitOut);
			}
		}

		[Test]
		public void TestPit()
		{
			var fileName = Path.GetTempFileName();
			try
			{

				File.WriteAllText(fileName, "{\"a\":\"b\"}");

				var xml = @"
<Peach>
	<Test name='Default'>
		<WebProxy>
			<Route url='/p/jobs' swagger='" + fileName + @"' />
		</WebProxy>
		<Strategy class='WebProxy' />
		<Publisher class='Null' />
	</Test>
</Peach>";

				var dom = ParsePit(xml);

				Assert.NotNull(dom);

				var sm = (WebProxyStateModel) dom.tests[0].stateModel;
				var routes = sm.Options.Routes;

				Assert.AreEqual(2, routes.Count);
				Assert.AreEqual("/p/jobs", routes[0].Url);
				Assert.AreEqual("*", routes[1].Url);
			}
			finally
			{
				File.Delete(fileName);
			}
		}

		public static Peach.Core.Dom.Dom ParsePit(string xml, Dictionary<string, object> args = null)
		{
			return new ProPitParser().asParser(args, new MemoryStream(Encoding.UTF8.GetBytes(xml)));
		}

	}
}
