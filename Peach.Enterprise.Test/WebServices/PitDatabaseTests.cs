using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;
using Peach.Enterprise.WebServices;
using Peach.Core;


namespace Peach.Enterprise.Test.WebServices
{
	[TestFixture]
	public class PitDatabaseTests
	{
		string root;
		PitDatabase db;

		static string pitExample =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach xmlns='http://peachfuzzer.com/2012/Peach'
       xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
       xsi:schemaLocation='http://peachfuzzer.com/2012/Peach peach.xsd'
       author='Deja Vu Security, LLC'
       description='IMG PIT'
       version='0.0.1'>

	<DataModel name='DM'>
		<Blob/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel name='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>
";

		static string configExample =
@"<?xml version='1.0' encoding='utf-8'?>
<PitDefines xmlns:xsd='http://www.w3.org/2001/XMLSchema'
            xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
            xmlns='http://peachfuzzer.com/2012/PitDefines'>
  <All>
    <Strategy key='Strategy' value='Random' name='Mutation Strategy' description='The mutation strategy to use when fuzzing.' />
    <String key='PitLibraryPath' value='.' name='Pit Library Path' description='The path to the root of the pit library.' />
  </All>
</PitDefines>
";

		[SetUp]
		public void SetUp()
		{
			var tmp = Path.GetTempFileName();
			File.Delete(tmp);
			Directory.CreateDirectory(tmp);
			root = tmp;

			var cat = Path.Combine(tmp, "Image");
			Directory.CreateDirectory(cat);

			File.WriteAllText(Path.Combine(cat, "IMG.xml"), pitExample);
			File.WriteAllText(Path.Combine(cat, "IMG.xml.config"), configExample);

			db = new PitDatabase();
			db.ValidationEventHandler += (o, e) => { throw new PeachException("DB failed to load", e.Exception);  };
			db.Load(root);
		}

		[TearDown]
		public void TearDown()
		{
			if (root != null)
				Directory.Delete(root, true);

			root = null;
			db = null;
		}

		[Test]
		public void TestParse()
		{
			Assert.NotNull(root);
			Assert.NotNull(db);

			Assert.AreEqual(1, db.Entries.Count());
			Assert.AreEqual(2, db.Libraries.Count());

			var libs = db.Libraries.ToList();

			Assert.True(libs[0].Locked);
			Assert.AreEqual(1, libs[0].Versions.Count);
			Assert.True(libs[0].Versions[0].Locked);
			Assert.AreEqual(1, libs[0].Versions[0].Pits.Count);

			Assert.False(libs[1].Locked);
			Assert.AreEqual(1, libs[1].Versions.Count);
			Assert.False(libs[1].Versions[0].Locked);
		}

		[Test]
		public void TestNoConfig()
		{
			File.WriteAllText(Path.Combine(root, "Image", "IMG Copy.xml"), pitExample);

			db.Load(root);

			var ent = db.Entries.ToList();
			Assert.AreEqual(2, ent.Count);

			var img = ent.Where(e => e.Name == "IMG").First();

			var cfg1 = db.GetConfigByUrl(img.PitUrl);
			Assert.NotNull(cfg1);
			Assert.AreEqual(2, cfg1.Config.Count);

			var imgCopy = ent.Where(e => e.Name == "IMG Copy").First();

			var cfg2 = db.GetConfigByUrl(imgCopy.PitUrl);
			Assert.NotNull(cfg2);
			Assert.AreEqual(0, cfg2.Config.Count);
		}

		[Test]
		public void TestCopyPro()
		{
			var pit = db.Entries.ElementAt(0);
			var lib = db.Libraries.ElementAt(1);

			var newName = "IMG Copy";
			var newDesc = "My copy of the img pit";

			var newUrl = db.CopyPit(lib.LibraryUrl, pit.PitUrl, newName, newDesc);

			Assert.NotNull(newUrl);

			Assert.AreEqual(2, db.Entries.Count());
			Assert.AreEqual(1, lib.Versions[0].Pits.Count());

			var newPit = db.GetPitByUrl(newUrl);

			Assert.NotNull(newPit);

			var newXml = File.ReadAllText(newPit.Versions[0].Files[0].Name);
			Assert.NotNull(newXml);

			Assert.AreEqual(newName, newPit.Name);
			Assert.AreEqual(newDesc, newPit.Description);
			Assert.AreEqual(Environment.UserName, newPit.User);

			var expName = Path.Combine(root, "User", "Image", "IMG Copy.xml");
			Assert.AreEqual(1, newPit.Versions[0].Files.Count);
			Assert.AreEqual(expName, newPit.Versions[0].Files[0].Name);

			Assert.True(File.Exists(expName));
			Assert.True(File.Exists(expName + ".config"));

			var srcCfg = File.ReadAllText(pit.Versions[0].Files[0].Name + ".config");
			var newCfg = File.ReadAllText(expName + ".config");

			Assert.AreEqual(srcCfg, newCfg);
		}

		[Test]
		public void TestCopyUser()
		{
			var pit = db.Entries.ElementAt(0);
			var lib = db.Libraries.ElementAt(1);

			var newUrl1 = db.CopyPit(lib.LibraryUrl, pit.PitUrl, "IMG Copy 1", "Img desc 1");
			Assert.NotNull(newUrl1);

			var newUrl2 = db.CopyPit(lib.LibraryUrl, newUrl1, "IMG Copy 2", "Img desc 2");
			Assert.NotNull(newUrl2);


			Assert.AreEqual(3, db.Entries.Count());
			Assert.AreEqual(2, lib.Versions[0].Pits.Count());

			var newPit = db.GetPitByUrl(newUrl2);

			Assert.NotNull(newPit);

			var newXml = File.ReadAllText(newPit.Versions[0].Files[0].Name);
			Assert.NotNull(newXml);

			var expName = Path.Combine(root, "User", "Image", "IMG Copy 2.xml");
			Assert.AreEqual(1, newPit.Versions[0].Files.Count);
			Assert.AreEqual(expName, newPit.Versions[0].Files[0].Name);

			Assert.True(File.Exists(expName));
			Assert.True(File.Exists(expName + ".config"));

			var srcCfg = File.ReadAllText(pit.Versions[0].Files[0].Name + ".config");
			var newCfg = File.ReadAllText(expName + ".config");

			Assert.AreEqual(srcCfg, newCfg);
		}

		[Test]
		public void TestSaveMonitors()
		{
			var pit = db.Entries.First();

			var json = @"
[
{
	""agentUrl"":""local://"",
	""monitors"": [
		{
			""monitorClass"":""PageHeap"",
			""path"": [1110],
			""map"": [
				{ ""key"":""WinDbgExecutable"", ""param"":""Executable"", ""value"":""Foo.exe"" },
				{ ""key"":""WinDbgPath"", ""param"":""WinDbgPath"", ""value"":""C:\\WinDbg""  }
			],
			""description"": ""Page Heap: {WinDbgExecutable} {WinDbgPath}""
		},
		{
			""monitorClass"":""WindowsDebugger"",
			""path"": [1100],
			""map"": [
				{ ""key"":""WinDbgExecutable"",	""param"":""Executable"", ""value"":""Foo.exe"" },
				{ ""key"":""WinDbgArguments"", ""param"":""Arguments"", ""value"":""--arg"" },
				{ ""key"":""WinDbgIgnoreFirstChanceGuardPage"",	""param"":""IgnoreFirstChanceGuardPage"", ""value"":""false"" }
			],
			""description"": ""Windows Debugger: {WinDbgExecutable} {WinDbgPath} {WinDbgProcessName} {WinDbgService} {WinDbgStart} {WinDbgIgnoreFirstChanceGuardPage}""
		},
	],
},
{
	""agentUrl"":""tcp://remotehostname"",
	""monitors"": [
		{
			""monitorClass"":""Pcap"",
			""path"": [ 4100 ],
			""map"":[
				{""key"":""PcapDevice"", ""param"":""Device"", ""value"":""eth0"" },
				{""key"":""PcapFilter"", ""param"":""Filter"", ""value"":""tcp port 80"" }
				],
			""description"":""Network capture on {AgentUrl}, interface {PcapDevice} using {PcapFilter}.""
		},
	],
},
{
	""agentUrl"":""local://"",
	""monitors"": [
		{
			""monitorClass"":""CanaKit"",
			""path"": [ 4100 ],
			""map"": [
				{""key"":""CanaKitRelaySerialPort"",	""param"":""SerialPort"", ""value"":""COM1"" },
				{""key"":""CanaKitRelayRelayNumber"",	""param"":""RelayNumber"", ""value"":""1"" },
			]
		},
	],
},
{
	""agentUrl"":""tcp://remotehostname2"",
	""monitors"": [
		{
			""monitorClass"":""Pcap"",
			""path"": [ 4100 ],
			""map"":[
				{""key"":""PcapDevice"", ""param"":""Device"", ""value"":""eth0"" },
				{""key"":""PcapFilter"", ""param"":""Filter"", ""value"":""tcp port 80"" }
				],
			""description"":""Network capture on {AgentUrl}, interface {PcapDevice} using {PcapFilter}.""
		},
	],
},
{
	""agentUrl"":""tcp://remotehostname"",
	""monitors"": [
		{
			""monitorClass"":""Pcap"",
			""path"": [ 4100 ],
			""map"":[
				{""key"":""PcapDevice"", ""param"":""Device"", ""value"":""eth0"" },
				{""key"":""PcapFilter"", ""param"":""Filter"", ""value"":""tcp port 8080"" }
				],
			""description"":""Network capture on {AgentUrl}, interface {PcapDevice} using {PcapFilter}.""
		},
	],
},
]";
			var monitors = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Peach.Enterprise.WebServices.Models.Agent>>(json);

			PitDatabase.SaveMonitors(pit, monitors);

			var parser = new Peach.Core.Analyzers.PitParser();
			var dom = parser.asParser(null, pit.Versions[0].Files[0].Name);

			Assert.AreEqual(3, dom.tests[0].agents.Count);

			Assert.AreEqual("Agent0", dom.tests[0].agents[0].name);
			Assert.AreEqual("local://", dom.tests[0].agents[0].location);
			Assert.AreEqual(3, dom.tests[0].agents[0].monitors.Count);

			VerifyMonitor(monitors[0].Monitors[0], dom.tests[0].agents[0].monitors[0]);
			VerifyMonitor(monitors[0].Monitors[1], dom.tests[0].agents[0].monitors[1]);
			VerifyMonitor(monitors[2].Monitors[0], dom.tests[0].agents[0].monitors[2]);

			Assert.AreEqual("Agent1", dom.tests[0].agents[1].name);
			Assert.AreEqual("tcp://remotehostname", dom.tests[0].agents[1].location);
			Assert.AreEqual(2, dom.tests[0].agents[1].monitors.Count);

			VerifyMonitor(monitors[1].Monitors[0], dom.tests[0].agents[1].monitors[0]);
			VerifyMonitor(monitors[4].Monitors[0], dom.tests[0].agents[1].monitors[1]);

			Assert.AreEqual("Agent2", dom.tests[0].agents[2].name);
			Assert.AreEqual("tcp://remotehostname2", dom.tests[0].agents[2].location);
			Assert.AreEqual(1, dom.tests[0].agents[2].monitors.Count);

			VerifyMonitor(monitors[3].Monitors[0], dom.tests[0].agents[2].monitors[0]);
		}

		private void VerifyMonitor(Enterprise.WebServices.Models.Monitor jsonMon, Core.Dom.Monitor domMon)
		{
			Assert.AreEqual(jsonMon.MonitorClass, domMon.cls);
			Assert.AreEqual(jsonMon.Map.Count, domMon.parameters.Count);

			foreach (var item in jsonMon.Map)
			{
				Assert.True(domMon.parameters.ContainsKey(item.Key));
				Assert.AreEqual(item.Value, (string)domMon.parameters[item.Key]);
			}
		}

		void VerifyMonitor2() { }
	}
}
