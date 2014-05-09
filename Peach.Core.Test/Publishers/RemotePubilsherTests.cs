using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Peach.Core;
using Peach.Core.Analyzers;
using System.IO;

namespace Peach.Core.Test.Publishers
{
	[TestFixture]
	class RemotePublisherTests
	{
		string template = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">
		<String value=""Hello""/>
	</DataModel>

	<StateModel name=""TheStateModel"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Action1"" type=""output"">
				<DataModel ref=""TheDataModel""/>
			</Action>
		</State>
	</StateModel>

	<Agent name=""LocalAgent"">
	</Agent>

	<Test name=""Default"">
		<Agent ref=""LocalAgent""/>
		<StateModel ref=""TheStateModel""/>
		<Publisher class=""Remote"">
			<Param name=""Agent"" value=""LocalAgent""/>
			<Param name=""Class"" value=""File""/>
			<Param name=""FileName"" value=""{0}""/>
		</Publisher>
	</Test>
</Peach>";

		string raw_eth = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">
		<String value=""Hello!Hello!001122334455""/>
	</DataModel>

	<StateModel name=""TheStateModel"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Action1"" type=""output"">
				<DataModel ref=""TheDataModel""/>
			</Action>
			<Action name=""Action2"" type=""input"">
				<DataModel ref=""TheDataModel""/>
			</Action>
		</State>
	</StateModel>

	<Agent name=""LocalAgent"" location=""tcp://127.0.0.1:9001"">
	</Agent>

	<Test name=""Default"">
		<Agent ref=""LocalAgent""/>
		<StateModel ref=""TheStateModel""/>
		<Publisher class=""Remote"">
			<Param name=""Agent"" value=""LocalAgent""/>
			<Param name=""Class"" value=""RawEther""/>
			<Param name=""Interface"" value=""eth0""/>
		</Publisher>
	</Test>
</Peach>";

		string udp = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">
		<String value=""Hello!Hello!001122334455"" token=""true""/>
	</DataModel>

	<StateModel name=""TheStateModel"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Action1"" type=""output"">
				<DataModel ref=""TheDataModel""/>
			</Action>
			<Action name=""Action2"" type=""input"">
				<DataModel ref=""TheDataModel""/>
			</Action>
		</State>
	</StateModel>

	<Agent name=""LocalAgent"" location=""tcp://127.0.0.1:9001"">
	</Agent>

	<Test name=""Default"">
		<Agent ref=""LocalAgent""/>
		<StateModel ref=""TheStateModel""/>
		<Publisher class=""Remote"">
			<Param name=""Agent"" value=""LocalAgent""/>
			<Param name=""Class"" value=""Udp""/>
			<Param name=""Host"" value=""127.0.0.1""/>
			<Param name=""SrcPort"" value=""{0}""/>
			<Param name=""Port"" value=""{0}""/>
		</Publisher>
	</Test>
</Peach>";

		[Test]
		public void TestCreate()
		{
			string xml = string.Format(template, "tile.txt");

			PitParser parser = new PitParser();
			parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
		}

		[Test]
		public void TestRun()
		{
			string tempFile = Path.GetTempFileName();

			string xml = string.Format(template, tempFile);

			PitParser parser = new PitParser();
			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);

			string[] output = File.ReadAllLines(tempFile);

			Assert.AreEqual(1, output.Length);
			Assert.AreEqual("Hello", output[0]);
		}

		public void RunRemote(string xml)
		{
			var process = Helpers.StartAgent();

			try
			{
				PitParser parser = new PitParser();
				Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

				RunConfiguration config = new RunConfiguration();
				config.singleIteration = true;

				Engine e = new Engine(null);
				e.startFuzzing(dom, config);
			}
			finally
			{
				Helpers.StopAgent(process);
			}
		}

		[Test]
		public void TestRaw()
		{
			if (Peach.Core.Platform.GetOS() != Peach.Core.Platform.OS.Linux)
				Assert.Ignore("Only supported on Linux");

			RunRemote(raw_eth);
		}

		[Test]
		public void TestUdp()
		{
			var port = TestBase.MakePort(12000, 13000);
			RunRemote(udp.Fmt(port));
		}
	}
}
