using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Test.Core.WebServices
{
	[TestFixture]
	class MonitorParamTests
	{
		[Test]
		public void TestMonitorMetadata()
		{
			// Verify no errors are produced against official metadata and all monitors

			var sb = new StringBuilder();
			var m = new MonitorMetadata();

			m.ErrorEventHandler += (s, e) => sb.AppendLine(e.GetException().Message);

			var details = m.Load(new List<string>());

			var json = JsonConvert.SerializeObject(details, Formatting.Indented, new JsonSerializerSettings
			{
				Converters = new List<JsonConverter> { new StringEnumConverter() },
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
			});

			Assert.NotNull(json);

			if (sb.Length > 0)
				Assert.Fail(sb.ToString());
		}


		[Test]
		public void TestOnCall()
		{
			// Verify on calls get properly added to monitor metadata
			var calls1 = new List<string> { "Foo1", "Bar1" };
			var details1 = MonitorMetadata.Generate(calls1);
			var calls2 = new List<string> { "Foo2", "Bar2" };
			var details2 = MonitorMetadata.Generate(calls2);

			Action<List<ParamDetail>, List<string>> verify = null;

			verify = (d, c) =>
			{
				foreach (var p in d)
				{
					if (p.Type == ParameterType.Call)
						CollectionAssert.AreEqual(c, p.Options);
					else if (p.Items != null)
						verify(p.Items, c);
				}
			};

			verify(details1, calls1);
			verify(details2, calls2);
		}

		[Test]
		public void TestNoErrors()
		{
			// When metadata syncs up with types, no errors are produced

			var tester = new MetadataTester
			{
				Type = new[] { typeof(TestMonitor) },
				Metadata = "[{'Name':'TestMonitor','Type':'Monitor','Items':[{'Name':'Test','Type':'Param'}]}]".Replace('\'', '\"')
			};

			var result = tester.Run();

			var exp = "[{'Description':'Desc','Items':[{'DefaultValue':'Foo','Description':'Desc','Key':'Test','Name':'Test','Optional':true,'Type':'String'}],'Key':'TestMonitor','Name':'TestMonitor','OS':'','Type':'Monitor'}]".Replace('\'', '\"');

			Assert.AreEqual(exp, result);

			Assert.AreEqual(0, tester.Errors.Count);
		}

		[Test]
		public void TestCollapsed()
		{
			// Verify we output "Collapsed":true when needed

			var tester = new MetadataTester
			{
				Type = new[] { typeof(TestMonitor) },
				Metadata = "[{'Name':'TestMonitor','Type':'Monitor','Items':[{'Name':'Group1','Type':'Group','Collapsed':true,'Items':[{'Name':'Test','Type':'Param'}]}]}]".Replace('\'', '\"')
			};

			var result = tester.Run();

			var exp = "[{'Description':'Desc','Items':[{'Collapsed':true,'Items':[{'DefaultValue':'Foo','Description':'Desc','Key':'Test','Name':'Test','Optional':true,'Type':'String'}],'Name':'Group1','Type':'Group'}],'Key':'TestMonitor','Name':'TestMonitor','OS':'','Type':'Monitor'}]".Replace('\'', '\"');

			Assert.AreEqual(exp, result);

			Assert.AreEqual(0, tester.Errors.Count);
		}

		[Test]
		public void TestMissingMetadata()
		{
			// If metadata resource can't be found, all monitors end up in "Other" group

			var tester = new MetadataTester
			{
				Type = new[] { typeof(TestMonitor) },
				Metadata = null
			};

			var result = tester.Run();

			var exp = "[{'Items':[{'Description':'Desc','Items':[{'DefaultValue':'Foo','Description':'Desc','Key':'Test','Name':'Test','Optional':true,'Type':'String'}],'Key':'TestMonitor','Name':'TestMonitor','OS':'','Type':'Monitor'}],'Name':'Other','Type':'Group'}]".Replace('\'', '\"');

			Assert.AreEqual(exp, result);

			Assert.AreEqual(1, tester.Errors.Count);
			Assert.AreEqual("Unable to locate monitor metadata resource.", tester.Errors[0]);
		}

		[Test]
		public void TestInvalidMetadata()
		{
			// If metadata resource can't be parsed, all monitors end up in "Other" group

			var tester = new MetadataTester
			{
				Type = new[] { typeof(TestMonitor) },
				Metadata = "{"
			};

			var result = tester.Run();

			var exp = "[{'Items':[{'Description':'Desc','Items':[{'DefaultValue':'Foo','Description':'Desc','Key':'Test','Name':'Test','Optional':true,'Type':'String'}],'Key':'TestMonitor','Name':'TestMonitor','OS':'','Type':'Monitor'}],'Name':'Other','Type':'Group'}]".Replace('\'', '\"');

			Assert.AreEqual(exp, result);

			Assert.AreEqual(1, tester.Errors.Count);
			Assert.AreEqual("Unable to parse monitor metadata resource.", tester.Errors[0]);
		}

		[Test]
		public void TestMissingMonitor()
		{
			// All monitors not referenced in metadata end up in "Other" group

			var tester = new MetadataTester
			{
				Type = new[] { typeof(TestMonitor), typeof(TestTwoMonitor) },
				Metadata = "[]"
			};

			var result = tester.Run();

			var exp = "[{'Items':[{'Description':'Desc','Items':[{'DefaultValue':'Foo','Description':'Desc','Key':'Test','Name':'Test','Optional':true,'Type':'String'}],'Key':'TestMonitor','Name':'TestMonitor','OS':'','Type':'Monitor'},{'Description':'Desc','Items':[{'DefaultValue':'Foo','Description':'Desc','Key':'TestTwo','Name':'TestTwo','Optional':true,'Type':'String'}],'Key':'TestTwoMonitor','Name':'TestTwoMonitor','OS':'','Type':'Monitor'}],'Name':'Other','Type':'Group'}]".Replace('\'', '\"');

			Assert.AreEqual(exp, result);

			Assert.AreEqual(1, tester.Errors.Count);
			Assert.AreEqual("Missing metadata entries for the following monitors: 'TestMonitor', 'TestTwoMonitor'.", tester.Errors[0]);
		}

		[Test]
		public void TestExtraMonitor()
		{
			// Ignore monitors that are referenced in metadata but don't exist

			var tester = new MetadataTester
			{
				Type = new[] { typeof(TestMonitor) },
				Metadata = "[{'Name':'MissingMonitor','Type':'Monitor','Items':[]},{'Name':'TestMonitor','Type':'Monitor','Items':[{'Name':'Test','Type':'Param'}]}]".Replace('\'', '\"')
			};

			var result = tester.Run();

			var exp = "[{'Description':'Desc','Items':[{'DefaultValue':'Foo','Description':'Desc','Key':'Test','Name':'Test','Optional':true,'Type':'String'}],'Key':'TestMonitor','Name':'TestMonitor','OS':'','Type':'Monitor'}]".Replace('\'', '\"');

			Assert.AreEqual(exp, result);

			Assert.AreEqual(1, tester.Errors.Count);
			Assert.AreEqual("Ignoring metadata entry for monitor 'MissingMonitor', no plugin exists with that name.", tester.Errors[0]);
		}

		[Test]
		public void TestOmittedParameter()
		{
			// Raise warning if metadata has an omitted paraameter for a monitor

			var tester = new MetadataTester
			{
				Type = new[] { typeof(TestThreeMonitor) },
				Metadata = "[{'Name':'TestThreeMonitor','Type':'Monitor','Items':[]}]".Replace('\'', '\"')
			};

			var result = tester.Run();

			var exp = "[{'Description':'Desc','Items':[],'Key':'TestThreeMonitor','Name':'TestThreeMonitor','OS':'','Type':'Monitor'}]".Replace('\'', '\"');

			Assert.AreEqual(exp, result);

			Assert.AreEqual(1, tester.Errors.Count);
			Assert.AreEqual("Monitor TestThreeMonitor had the following parameters omitted from the metadata: 'Param1', 'Param2'.", tester.Errors[0]);
		}

		[Test]
		public void TestDuplicatedParameter()
		{
			// Raise warning if metadata has duplicated paraameter for a monitor

			var tester = new MetadataTester
			{
				Type = new[] { typeof(TestTwoMonitor) },
				Metadata = "[{'Name':'TestTwoMonitor','Type':'Monitor','Items':[{'Name':'TestTwo','Type':'Param'},{'Name':'TestTwo','Type':'Param'},{'Name':'TestTwo','Type':'Param'}]}]".Replace('\'', '\"')
			};

			var result = tester.Run();

			var exp = "[{'Description':'Desc','Items':[{'DefaultValue':'Foo','Description':'Desc','Key':'TestTwo','Name':'TestTwo','Optional':true,'Type':'String'},{'DefaultValue':'Foo','Description':'Desc','Key':'TestTwo','Name':'TestTwo','Optional':true,'Type':'String'},{'DefaultValue':'Foo','Description':'Desc','Key':'TestTwo','Name':'TestTwo','Optional':true,'Type':'String'}],'Key':'TestTwoMonitor','Name':'TestTwoMonitor','OS':'','Type':'Monitor'}]".Replace('\'', '\"');

			Assert.AreEqual(exp, result);

			Assert.AreEqual(1, tester.Errors.Count);
			Assert.AreEqual("Monitor TestTwoMonitor had the following parameters duplicated in the metadata: 'TestTwo'.", tester.Errors[0]);
		}

		[Test]
		public void TestMissingParameter()
		{
			// Raise warning if metadata has paraameter for a monitor that doesn't exist

			var tester = new MetadataTester
			{
				Type = new[] { typeof(NoParamMonitor) },
				Metadata = "[{'Name':'NoParamMonitor','Type':'Monitor','Items':[{'Name':'TestTwo','Type':'Param'}]}]".Replace('\'', '\"')
			};

			var result = tester.Run();

			var exp = "[{'Description':'Desc','Items':[],'Key':'NoParamMonitor','Name':'NoParamMonitor','OS':'','Type':'Monitor'}]".Replace('\'', '\"');

			Assert.AreEqual(exp, result);

			Assert.AreEqual(1, tester.Errors.Count);
			Assert.AreEqual("Ignoring metadata entry for parameter 'TestTwo' on monitor 'NoParamMonitor', no parameter exists with that name.", tester.Errors[0]);
		}

		[Test]
		public void TestInvalidParameterType()
		{
			// Unsupported ParameterType triggers a warning and defaults to a string type.

			var tester = new MetadataTester
			{
				Type = new[] { typeof(InvalidParamMonitor) },
				Metadata = "[{'Name':'InvalidParamMonitor','Type':'Monitor','Items':[{'Name':'Test','Type':'Param'}]}]".Replace('\'', '\"')
			};

			var result = tester.Run();

			var exp = "[{'Description':'Desc','Items':[{'DefaultValue':'','Description':'Desc','Key':'Test','Name':'Test','Optional':true,'Type':'String'}],'Key':'InvalidParamMonitor','Name':'InvalidParamMonitor','OS':'','Type':'Monitor'}]".Replace('\'', '\"');

			Assert.AreEqual(exp, result);

			Assert.AreEqual(1, tester.Errors.Count);
			Assert.AreEqual("Monitor InvalidParamMonitor has invalid parameter type 'Peach.Pro.Test.Core.WebServices.MonitorParamTests+MetadataTester'.", tester.Errors[0]);
		}

		[Test]
		public void TestInvalidOS()
		{
			// Invalid OS on monitor triggers warning but emits empty string

			var tester = new MetadataTester
			{
				OS = Platform.OS.Unix,
				Type = new[] { typeof(NoParamMonitor) },
				Metadata = "[{'Name':'NoParamMonitor','Type':'Monitor'}]".Replace('\'', '\"')
			};

			var result = tester.Run();

			var exp = "[{'Description':'Desc','Key':'NoParamMonitor','Name':'NoParamMonitor','OS':'','Type':'Monitor'}]".Replace('\'', '\"');

			Assert.AreEqual(exp, result);

			Assert.AreEqual(1, tester.Errors.Count);
			Assert.AreEqual("Monitor NoParamMonitor specifies unsupported OS 'Unix'.", tester.Errors[0]);
		}

		[Test]
		public void TestNoDescription()
		{
			// No description causes json to output empty string

			var tester = new MetadataTester
			{
				Type = new[] { typeof(NoDescriptionMonitor) },
				Metadata = "[{'Name':'NoDescriptionMonitor','Type':'Monitor'}]".Replace('\'', '\"')
			};

			var result = tester.Run();

			var exp = "[{'Description':'','Key':'NoDescriptionMonitor','Name':'NoDescriptionMonitor','OS':'','Type':'Monitor'}]".Replace('\'', '\"');

			Assert.AreEqual(exp, result);

			Assert.AreEqual(1, tester.Errors.Count);
			Assert.AreEqual("Monitor NoDescriptionMonitor does not have a description.", tester.Errors[0]);
		}

		class NoDescriptionMonitor
		{
		}

		[System.ComponentModel.DescriptionAttribute("Desc")]
		class NoParamMonitor
		{
		}

		[System.ComponentModel.DescriptionAttribute("Desc")]
		[Parameter("Test", typeof(MetadataTester), "Desc", "")]
		class InvalidParamMonitor
		{
		}

		[System.ComponentModel.DescriptionAttribute("Desc")]
		[Parameter("Test", typeof(string), "Desc", "Foo")]
		class TestMonitor
		{
		}

		[System.ComponentModel.DescriptionAttribute("Desc")]
		[Parameter("TestTwo", typeof(string), "Desc", "Foo")]
		class TestTwoMonitor
		{
		}

		[System.ComponentModel.DescriptionAttribute("Desc")]
		[Parameter("Param1", typeof(string), "Desc", "Foo")]
		[Parameter("Param2", typeof(string), "Desc", "Bar")]
		class TestThreeMonitor
		{
		}

		private class MetadataTester : MonitorMetadata
		{
			private class OrderedContractResolver : DefaultContractResolver
			{
				protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
				{
					return base.CreateProperties(type, memberSerialization).OrderBy(p => p.PropertyName).ToList();
				}
			}

			public List<string> Errors { get; private set; }

			public string Metadata { private get; set; }
			public Type[] Type { private get; set; }
			public Platform.OS OS { private get; set; }

			public MetadataTester()
			{
				OS = Platform.OS.All;
				Errors = new List<string>();
				ErrorEventHandler += (o, e) => Errors.Add(e.GetException().Message);
			}

			public string Run()
			{
				Assert.NotNull(Type, "Type must be non-null");

				var details = Load(new List<string>());

				var json = JsonConvert.SerializeObject(details, Formatting.None, new JsonSerializerSettings
				{
					Converters = new List<JsonConverter> { new StringEnumConverter() },
					NullValueHandling = NullValueHandling.Ignore,
					DefaultValueHandling = DefaultValueHandling.Ignore,
					ContractResolver = new OrderedContractResolver()
				});

				return json;
			}

			protected override IEnumerable<KeyValuePair<MonitorAttribute, Type>> GetAllMonitors()
			{
				return Type.Select(t =>
					new KeyValuePair<MonitorAttribute, Type>(
						new MonitorAttribute(t.Name) { OS = OS },
						t));
			}

			protected override TextReader OpenMetadataStream()
			{
				if (Metadata == null)
					return null;

				return new StringReader(Metadata);
			}
		}
	}
}
