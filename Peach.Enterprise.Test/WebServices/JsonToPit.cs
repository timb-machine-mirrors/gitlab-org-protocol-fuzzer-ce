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

			var nodes = nav.Select("//Agent").OfType<System.Xml.XPath.XPathNavigator>().ToList();

			foreach (var n in nodes)
				n.DeleteSelf();

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

			Assert.AreEqual(expected, final);
		}
	}
}
