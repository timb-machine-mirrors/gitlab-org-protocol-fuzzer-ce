using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;

using Peach.Core;
using System.Xml;

namespace Peach.Enterprise.Test.WebServices
{
	[TestFixture]
	public class JsonToPit
	{
		[Test]
		public void Test()
		{
			string json = @"
[
    {
        ""agentUrl"": ""local://"",
        ""monitors"": [
            {
                ""monitorClass"":""Pcap"",
                ""map"":[
                    {""key"":""PcapDevice"", ""param"":""Device"", value:""MyInterface""},
                    {""key"":""PcapFilter"", ""param"":""Filter"", value:""MyFilter""}
                    ],
                ""description"":""Network capture on interface {PcapDevice} using {PcapFilter}, collect from {AgentUrl}""
            }
        ]
    }
]";

			string pit = @"
<Peach>
	<Include ns='foo' src='other'/>

	<Agent>
		<Monitor class='Foo' />
	</Agent>

	<Agent location='tcp://1.1.1.1'/>

	<Test name='Default'>
		<Publisher name='Pub'/>
		<Strategy class='Random'/>
		<Agent ref='Foo'/>
		<Agent ref='Bar'/>
		<Logger class='Simple'/>
	</Test>
</Peach>
";

			var agents = JsonConvert.DeserializeObject<List<Peach.Enterprise.WebServices.Models.Agent>>(json);

			Assert.NotNull(agents);
			Assert.AreEqual(1, agents.Count);

			var doc = new XmlDocument();
			doc.LoadXml(pit);

			var nav = doc.CreateNavigator();

			while (true)
			{
				var oldAgent = nav.SelectSingleNode("//Agent");

				if (oldAgent != null)
					oldAgent.DeleteSelf();
				else
					break;
			}

			var sb = new StringBuilder();
			using (var wtr = XmlWriter.Create(new StringWriter(sb), new XmlWriterSettings() { Indent = true, OmitXmlDeclaration = true}))
			{
				doc.WriteTo(wtr);
			}

			var final = sb.ToString();
			Assert.NotNull(final);

			string expected =
@"<Peach>
  <Include ns=""foo"" src=""other"" />
  <Test name=""Default"">
    <Publisher name=""Pub"" />
    <Strategy class=""Random"" />
    <Logger class=""Simple"" />
  </Test>
</Peach>";

			expected = expected.Replace("\r\n", "\n");
			final = final.Replace("\r\n", "\n");

			Assert.AreEqual(expected, final);
		}

		[Test]
		public void NamespacedXml()
		{
			string xml =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach xmlns='http://peachfuzzer.com/2012/Peach'
       xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
       xsi:schemaLocation='http://peachfuzzer.com/2012/Peach peach.xsd'
       author='Deja Vu Security, LLC'
       description='IMG PIT'
       version='0.0.1'>
	<Test name='Default' />
</Peach>
";

			string tmp = Path.GetTempFileName();

			try
			{
				File.WriteAllText(tmp, xml);

				var doc = new XmlDocument();

				using (var rdr = XmlReader.Create(tmp))
				{
					doc.Load(rdr);
				}

				var nav = doc.CreateNavigator();

				var nsMgr = new XmlNamespaceManager(nav.NameTable);
				nsMgr.AddNamespace("p", "http://peachfuzzer.com/2012/Peach");

				var test = nav.Select("/p:Peach/p:Test", nsMgr);

				Assert.True(test.MoveNext());
				Assert.AreEqual("Default", test.Current.GetAttribute("name", ""));

			}
			finally
			{
				File.Delete(tmp);
			}
		}


		[Test]
		public void WriteXmlWithAttrs()
		{
			string xml =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach xmlns=""http://peachfuzzer.com/2012/Peach"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://peachfuzzer.com/2012/Peach peach.xsd"" author=""Deja Vu Security, LLC"">
  <Test name=""Default"" />
</Peach>";

			string src = Path.GetTempFileName();
			string dst = Path.GetTempFileName();

			try
			{
				File.WriteAllText(src, xml);

				var doc = new XmlDocument();

				using (var rdr = XmlReader.Create(src))
				{
					doc.Load(rdr);
				}

				var settings = new XmlWriterSettings()
				{
					Indent = true,
					Encoding = System.Text.Encoding.UTF8,
					IndentChars = "  ",
				};

				using (var writer = XmlWriter.Create(dst, settings))
				{
					doc.WriteTo(writer);
				}

				string srcStr = File.ReadAllText(src).Replace("\r\n", "\n");
				string dstStr = File.ReadAllText(dst).Replace("\r\n", "\n");

				Assert.AreEqual(srcStr, dstStr);
			}
			finally
			{
				File.Delete(src);
				File.Delete(dst);
			}
		}

		[Test]
		public void AddXmlElement()
		{
			string xml =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach xmlns=""http://peachfuzzer.com/2012/Peach"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://peachfuzzer.com/2012/Peach peach.xsd"" author=""Deja Vu Security, LLC"">
  <Test name=""Default"" />
</Peach>";

			string expected =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach xmlns=""http://peachfuzzer.com/2012/Peach"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://peachfuzzer.com/2012/Peach peach.xsd"" author=""Deja Vu Security, LLC"">
  <Agent name=""TheAgent"" />
  <Test name=""Default"" />
</Peach>";

			string src = Path.GetTempFileName();
			string dst = Path.GetTempFileName();

			try
			{
				File.WriteAllText(src, xml);

				var doc = new XmlDocument();

				using (var rdr = XmlReader.Create(src))
				{
					doc.Load(rdr);
				}

				var nav = doc.CreateNavigator();

				var nsMgr = new XmlNamespaceManager(nav.NameTable);
				nsMgr.AddNamespace("p", "http://peachfuzzer.com/2012/Peach");

				var test = nav.SelectSingleNode("/p:Peach/p:Test", nsMgr);

				Assert.NotNull(test);

				using (var w = test.InsertBefore())
				{
					w.WriteStartElement("Agent", "http://peachfuzzer.com/2012/Peach");
					w.WriteAttributeString("name", "TheAgent");
					w.WriteEndElement();
				}

				var settings = new XmlWriterSettings()
				{
					Indent = true,
					Encoding = System.Text.Encoding.UTF8,
					IndentChars = "  ",
				};

				using (var writer = XmlWriter.Create(dst, settings))
				{
					doc.WriteTo(writer);
				}

				string srcStr = expected.Replace("\r\n", "\n");
				string dstStr = File.ReadAllText(dst).Replace("\r\n", "\n");

				Assert.AreEqual(srcStr, dstStr);
			}
			finally
			{
				File.Delete(src);
				File.Delete(dst);
			}
		}
	}
}
