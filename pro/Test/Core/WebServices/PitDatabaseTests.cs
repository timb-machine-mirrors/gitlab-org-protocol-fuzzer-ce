using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Analyzers;
using Peach.Pro.Core;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using File = System.IO.File;
using Monitor = Peach.Pro.Core.WebServices.Models.Monitor;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.WebServices
{
	[TestFixture]
	[Quick]
	[Peach]
	public class PitDatabaseTests
	{
		string root;
		PitDatabase db;

		static string remoteInclude =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach xmlns='http://peachfuzzer.com/2012/Peach'
       xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
       xsi:schemaLocation='http://peachfuzzer.com/2012/Peach peach.xsd'
       author='Pit Author Name'
       description='IMG PIT'
       version='0.0.1'>

	<Include ns='SM' src='http://foo.com/_Common/Models/Image/IMG_Data.xml' />

	<Test name='Default'>
		<Agent ref='TheAgent'/>
		<Strategy class='##Strategy##'/>
		<StateModel ref='SM:SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>
</Peach>
";

		static string modelExample =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach xmlns='http://peachfuzzer.com/2012/Peach'
       xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
       xsi:schemaLocation='http://peachfuzzer.com/2012/Peach peach.xsd'
       author='Pit Author Name'
       description='IMG PIT'
       version='0.0.1'>

	<DataModel name='DM'>
		<String value='Hello World' />
	</DataModel>
</Peach>
";

		static string pitExample =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach xmlns='http://peachfuzzer.com/2012/Peach'
       xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
       xsi:schemaLocation='http://peachfuzzer.com/2012/Peach peach.xsd'
       author='Pit Author Name'
       version='0.0.1'>

	<Agent name='TheAgent'>
		<Monitor class='RunCommand'>
			<Param name='Command' value='Foo'/>
			<Param name='StartOnCall' value='Foo'/>
		</Monitor>
	</Agent>

	<Include ns='DM' src='file:##PitLibraryPath##/_Common/Models/Image/IMG_Data.xml' />

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel name='DM:DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<Agent ref='TheAgent'/>
		<Strategy class='##Strategy##'/>
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
    <String key='SomeMiscVariable' value='Foo' name='Misc Variable' description='Description goes here' />
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

			var mod = Path.Combine(tmp, "_Common", "Models", "Image");
			Directory.CreateDirectory(mod);

			File.WriteAllText(Path.Combine(mod, "IMG_Data.xml"), modelExample);
			File.WriteAllText(Path.Combine(cat, "IMG.xml"), pitExample);
			File.WriteAllText(Path.Combine(cat, "IMG.xml.config"), configExample);

			db = new PitDatabase();
			db.ValidationEventHandler += OnValidationEvent;
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

		private void OnValidationEvent(object sender, ValidationEventArgs args)
		{
			throw new PeachException("DB failed to load", args.Exception);
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
			Assert.AreEqual("IMG", libs[0].Versions[0].Pits[0].Name);

			var p = db.GetPitByUrl(libs[0].Versions[0].Pits[0].PitUrl);
			Assert.NotNull(p);

			Assert.AreEqual(true, p.Versions[0].Configured);

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

			var img = ent.First(e => e.Name == "IMG");

			var cfg1 = db.GetConfigByUrl(img.PitUrl);
			Assert.NotNull(cfg1);

			var imgCopy = ent.First(e => e.Name == "IMG Copy");

			var cfg2 = db.GetConfigByUrl(imgCopy.PitUrl);
			Assert.NotNull(cfg2);
			Assert.AreEqual(4, cfg2.Count);

			// Should include system defines

			Assert.AreEqual("Peach.Pwd", cfg2[0].Key);
			Assert.AreEqual("Peach.Cwd", cfg2[1].Key);
			Assert.AreEqual("Peach.LogRoot", cfg2[2].Key);
			Assert.AreEqual("PitLibraryPath", cfg2[3].Key);

			// Saving should create the file
			var cfg = imgCopy.Versions[0].Files[0].Name + ".config";
			Assert.False(File.Exists(cfg), ".config file should not exist");

			PitDatabase.SaveConfig(imgCopy, cfg2);

			// System defines should not be in the file
			Assert.True(File.Exists(cfg), ".config file should exist");

			var defs = PitDefines.Parse(cfg);
			Assert.AreEqual(0, defs.Count);
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
			Assert.AreEqual(2, newPit.Versions[0].Files.Count);
			Assert.AreEqual(expName, newPit.Versions[0].Files[0].Name);

			Assert.True(File.Exists(expName));
			Assert.True(File.Exists(expName + ".config"));

			var srcCfg = File.ReadAllText(pit.Versions[0].Files[0].Name + ".config");
			var newCfg = File.ReadAllText(expName + ".config");

			Assert.AreEqual(srcCfg, newCfg);
		}

		[Test]
		public void TestCopyDotInName()
		{
			var pit = db.Entries.ElementAt(0);
			var lib = db.Libraries.ElementAt(1);

			var newName = "Foo.Mine";
			var newDesc = "My copy of the img pit";

			var newUrl = db.CopyPit(lib.LibraryUrl, pit.PitUrl, newName, newDesc);
			var newPit = db.GetPitByUrl(newUrl);

			Assert.AreEqual(newName, newPit.Name);
		}

		[Test]
		public void TestCopyPathInFilename()
		{
			var pit = db.Entries.ElementAt(0);
			var lib = db.Libraries.ElementAt(1);

			var newName = "../../../Foo";
			var newDesc = "My copy of the img pit";

			try
			{
				db.CopyPit(lib.LibraryUrl, pit.PitUrl, newName, newDesc);
				Assert.Fail("Should throw");
			}
			catch (ArgumentException)
			{
			}
		}

		[Test]
		public void TestCopyBadFilename()
		{
			var pit = db.Entries.ElementAt(0);
			var lib = db.Libraries.ElementAt(1);

			var newName = "****";
			var newDesc = "My copy of the img pit";

			try
			{
				// Linux lets everything but '/' be in a file name
				db.CopyPit(lib.LibraryUrl, pit.PitUrl, newName, newDesc);

				if (Platform.GetOS() == Platform.OS.Windows)
					Assert.Fail("Should throw");
				else
					Assert.Pass();
			}
			catch (ArgumentException)
			{
				if (Platform.GetOS() == Platform.OS.Windows)
					Assert.Pass();
				else
					Assert.Fail("Should not throw");
			}
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
			Assert.AreEqual(2, newPit.Versions[0].Files.Count);
			Assert.AreEqual(expName, newPit.Versions[0].Files[0].Name);

			Assert.True(File.Exists(expName));
			Assert.True(File.Exists(expName + ".config"));

			var srcCfg = File.ReadAllText(pit.Versions[0].Files[0].Name + ".config");
			var newCfg = File.ReadAllText(expName + ".config");

			Assert.AreEqual(srcCfg, newCfg);
		}

		[Test]
		public void TestGetPitConfig()
		{
			var pit = db.Entries.First();

			var cfg = db.GetConfigByUrl(pit.PitUrl);

			Assert.NotNull(cfg);
			Assert.AreEqual(6, cfg.Count);

			// Always are 3 system types at the beginning!
			Assert.AreEqual("Peach.Pwd", cfg[0].Key);
			Assert.AreEqual(ParameterType.System, cfg[0].Type);
			Assert.AreEqual("Peach.Cwd", cfg[1].Key);
			Assert.AreEqual(ParameterType.System, cfg[1].Type);
			Assert.AreEqual("Peach.LogRoot", cfg[2].Key);
			Assert.AreEqual(ParameterType.System, cfg[2].Type);

			// PitLibraryPath is special, and gets turned into a System type
			// regardless of what is in the pit .config
			Assert.AreEqual("PitLibraryPath", cfg[3].Key);
			Assert.AreNotEqual(".", cfg[3].Value);
			Assert.AreEqual(ParameterType.System, cfg[3].Type);

			Assert.AreEqual("Strategy", cfg[4].Key);
			Assert.AreEqual(ParameterType.Enum, cfg[4].Type);

			Assert.AreEqual("SomeMiscVariable", cfg[5].Key);
			Assert.AreEqual(ParameterType.String, cfg[5].Type);
		}

		[Test]
		public void TestSetPitConfig()
		{
			var pit = db.Entries.First();

			var cfg = db.GetConfigByUrl(pit.PitUrl);


			Assert.NotNull(cfg);
			Assert.AreEqual(6, cfg.Count);
			Assert.AreEqual("SomeMiscVariable", cfg[5].Key);
			Assert.AreEqual(ParameterType.String, cfg[5].Type);
			Assert.AreNotEqual("Foo Bar Baz", cfg[5].Value);
			cfg[5].Value = "Foo Bar Baz";

			PitDatabase.SaveConfig(pit, cfg);

			var file = pit.Versions[0].Files[0].Name + ".config";
			var defs = PitDefines.Parse(file);
			Assert.NotNull(defs);

			// Peach.Pwd and Peach.Cwd do not get saved
			Assert.AreEqual(3, defs.Count);

			Assert.AreEqual("Strategy", defs[0].Key);
			Assert.AreEqual("Random", defs[0].Value);

			Assert.AreEqual("SomeMiscVariable", defs[2].Key);
			Assert.AreEqual("Foo Bar Baz", defs[2].Value);

			// PitLibraryPath should NOT be updated, it is set automagically by the runtime
			Assert.AreEqual("PitLibraryPath", defs[1].Key);
			Assert.AreEqual(".", defs[1].Value);
		}


		[Test]
		public void HasAgents()
		{
			string noAgents =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach xmlns='http://peachfuzzer.com/2012/Peach'
       xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
       xsi:schemaLocation='http://peachfuzzer.com/2012/Peach peach.xsd'
       author='Pit Author Name'
       description='File'
       version='0.0.1'>

	<Test name='Default'>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			File.WriteAllText(Path.Combine(root, "Image", "File.xml"), noAgents);

			db = new PitDatabase(root);
			Assert.NotNull(db);
			Assert.AreEqual(2, db.Entries.Count());

			var file = db.Entries.FirstOrDefault(e => e.Name == "File");
			Assert.NotNull(file);
			Assert.False(file.Versions[0].Configured);

			var img = db.Entries.FirstOrDefault(e => e.Name == "IMG");
			Assert.NotNull(img);
			Assert.True(img.Versions[0].Configured);
		}

		[Test]
		public void TestAllMonitors()
		{
			var errors = new List<string>();

			// remove test SetUp handler for this test
			db.ValidationEventHandler -= OnValidationEvent;

			db.ValidationEventHandler += (s, e) => errors.Add(e.Exception.Message);

			db.GetAllMonitors();

			CollectionAssert.IsEmpty(errors);
		}

		[Test]
		public void TestInvalidMonitor()
		{
			// remove test SetUp handler for this test
			db.ValidationEventHandler -= OnValidationEvent;

			var error = false;
			db.ValidationEventHandler += (s, e) =>
			{
				error = true;
			};

			var attr = new MonitorAttribute("FakeMonitor")
			{
				OS = Platform.OS.Unix
			};

			var monitor = db.MakeMonitor(attr, typeof(string), null);
			Assert.IsTrue(error);
			Assert.AreEqual("", monitor.OS);
		}

		[Test]
		public void TestSaveMonitors()
		{
			var pit = db.Entries.First();
			Assert.NotNull(pit);

			var json = @"
[
{
	""agentUrl"":""local://"",
	""monitors"": [
		{
			""monitorClass"":""PageHeap"",
			""map"": [
				{ ""name"":""Executable"", ""value"":""Foo.exe"" },
				{ ""name"":""WinDbgPath"", ""value"":""C:\\WinDbg""  }
			],
			""description"": ""Page Heap: {WinDbgExecutable} {WinDbgPath}""
		},
		{
			""monitorClass"":""WindowsDebugger"",
			""map"": [
				{ ""name"":""Executable"", ""value"":""Foo.exe"" },
				{ ""name"":""Arguments"", ""value"":""--arg"" },
				{ ""name"":""IgnoreFirstChanceGuardPage"", ""value"":""false"" }
			],
			""description"": ""Windows Debugger: {WinDbgExecutable} {WinDbgPath} {WinDbgProcessName} {WinDbgService} {WinDbgStart} {WinDbgIgnoreFirstChanceGuardPage}""
		}
	]
},
{
	""agentUrl"":""tcp://remotehostname"",
	""monitors"": [
		{
			""monitorClass"":""Pcap"",
			""map"": [
				{""name"":""Device"", ""value"":""eth0"" },
				{""name"":""Filter"", ""value"":""tcp port 80"" }
			],
			""description"":""Network capture on {AgentUrl}, interface {PcapDevice} using {PcapFilter}.""
		}
	]
},
{
	""agentUrl"":""local://"",
	""monitors"": [
		{
			""monitorClass"":""CanaKit"",
			""map"": [
				{""name"":""SerialPort"", ""value"":""COM1"" },
				{""name"":""RelayNumber"", ""value"":""1"" }
			]
		}
	]
},
{
	""agentUrl"":""tcp://remotehostname2"",
	""monitors"": [
		{
			""monitorClass"":""Pcap"",
			""map"": [
				{""name"":""Device"", ""value"":""eth0"" },
				{""name"":""Filter"", ""value"":""tcp port 80"" }
			],
			""description"":""Network capture on {AgentUrl}, interface {PcapDevice} using {PcapFilter}.""
		}
	]
},
{
	""agentUrl"":""tcp://remotehostname"",
	""monitors"": [
		{
			""monitorClass"":""Pcap"",
			""map"": [
				{""name"":""Device"", ""value"":""eth0"" },
				{""name"":""Filter"", ""value"":""tcp port 8080"" }
			],
			""description"":""Network capture on {AgentUrl}, interface {PcapDevice} using {PcapFilter}.""
		},
	]
}
]";

			var monitors = JsonConvert.DeserializeObject<List<Pro.Core.WebServices.Models.Agent>>(json);
			Assert.NotNull(monitors);

			PitDatabase.SaveAgents(pit, monitors);

			var parser = new PitParser();

			var defs = new Dictionary<string, string>
			{
				{"PitLibraryPath", root}, 
				{"Strategy", "Random"}
			};

			var opts = new Dictionary<string, object>
			{
				{PitParser.DefinedValues, defs}
			};

			var dom = parser.asParser(opts, pit.Versions[0].Files[0].Name);

			Assert.AreEqual(3, dom.tests[0].agents.Count);

			Assert.AreEqual("Agent0", dom.tests[0].agents[0].Name);
			Assert.AreEqual("local://", dom.tests[0].agents[0].location);
			Assert.AreEqual(3, dom.tests[0].agents[0].monitors.Count);

			VerifyMonitor(monitors[0].Monitors[0], dom.tests[0].agents[0].monitors[0]);
			VerifyMonitor(monitors[0].Monitors[1], dom.tests[0].agents[0].monitors[1]);
			VerifyMonitor(monitors[2].Monitors[0], dom.tests[0].agents[0].monitors[2]);

			Assert.AreEqual("Agent1", dom.tests[0].agents[1].Name);
			Assert.AreEqual("tcp://remotehostname", dom.tests[0].agents[1].location);
			Assert.AreEqual(2, dom.tests[0].agents[1].monitors.Count);

			VerifyMonitor(monitors[1].Monitors[0], dom.tests[0].agents[1].monitors[0]);
			VerifyMonitor(monitors[4].Monitors[0], dom.tests[0].agents[1].monitors[1]);

			Assert.AreEqual("Agent2", dom.tests[0].agents[2].Name);
			Assert.AreEqual("tcp://remotehostname2", dom.tests[0].agents[2].location);
			Assert.AreEqual(1, dom.tests[0].agents[2].monitors.Count);

			VerifyMonitor(monitors[3].Monitors[0], dom.tests[0].agents[2].monitors[0]);
		}

		private void VerifyMonitor(Monitor jsonMon, Peach.Core.Dom.Monitor domMon)
		{
			Assert.AreEqual(jsonMon.MonitorClass, domMon.cls);
			Assert.AreEqual(jsonMon.Map.Count, domMon.parameters.Count);

			foreach (var item in jsonMon.Map)
			{
				Assert.True(domMon.parameters.ContainsKey(item.Name));
				Assert.AreEqual(item.Value, (string)domMon.parameters[item.Name]);
			}
		}

		[Test]
		public void TestSaveProcessMonitors()
		{
			var pit = db.Entries.First();

			var json = @"
[
{
	""agentUrl"":""local://"",
	""monitors"": [
		{
			""monitorClass"":""WindowsDebugger"",
			""map"": [
				{ ""name"":""StartMode"", ""value"":""StartOnCall"" },
			],
		},
		{
			""monitorClass"":""WindowsDebugger"",
			""map"": [
				{ ""name"":""StartMode"", ""value"":""RestartOnEachTest"" },
			],
		},
		{
			""monitorClass"":""WindowsDebugger"",
			""map"": [
				{ ""name"":""StartMode"", ""value"":""StartOnEachIteration"" },
			],
		},
	],
},
]";
			var agents = JsonConvert.DeserializeObject<List<Pro.Core.WebServices.Models.Agent>>(json);

			PitDatabase.SaveAgents(pit, agents);

			var parser = new PitParser();

			var opts = new Dictionary<string, object>();
			var defs = new Dictionary<string, string>
			{
				{"PitLibraryPath", root},
				{"Strategy", "Random"}
			};
			opts[PitParser.DefinedValues] = defs;

			var dom = parser.asParser(opts, pit.Versions[0].Files[0].Name);

			Assert.AreEqual(1, dom.tests[0].agents.Count);

			Assert.AreEqual("Agent0", dom.tests[0].agents[0].Name);
			Assert.AreEqual("local://", dom.tests[0].agents[0].location);
			Assert.AreEqual(3, dom.tests[0].agents[0].monitors.Count);

			var param1 = dom.tests[0].agents[0].monitors[0].parameters;
			Assert.AreEqual(1, param1.Count);
			Assert.True(param1.ContainsKey("StartOnCall"));
			Assert.AreEqual("ExitIterationEvent", (string)param1["StartOnCall"]);


			var param2 = dom.tests[0].agents[0].monitors[1].parameters;
			Assert.AreEqual(2, param2.Count);
			Assert.True(param2.ContainsKey("StartOnCall"));
			Assert.AreEqual("StartIterationEvent", (string)param2["StartOnCall"]);
			Assert.True(param2.ContainsKey("WaitForExitOnCall"));
			Assert.AreEqual("ExitIterationEvent", (string)param2["WaitForExitOnCall"]);

			var param3 = dom.tests[0].agents[0].monitors[2].parameters;
			Assert.AreEqual(1, param3.Count);
			Assert.True(param3.ContainsKey("RestartOnEachTest"));
			Assert.AreEqual("false", (string)param3["RestartOnEachTest"]);
		}

		[Test]
		public void RemoteInclude()
		{
			File.WriteAllText(Path.Combine(root, "Image", "Remote.xml"), remoteInclude);

			db = new PitDatabase(root);
			Assert.NotNull(db);
			Assert.AreEqual(2, db.Entries.Count());

			var file = db.Entries.FirstOrDefault(e => e.Name == "Remote");
			Assert.NotNull(file);
			Assert.AreEqual(1, file.Versions[0].Files.Count);
		}

		[Test]
		public void IncludeWithoutXmlns()
		{
			string data =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='DM'>
		<String/>
	</DataModel>
</Peach>
";
			string pit =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<Include ns='DM' src='file:##PitLibraryPath##/_Common/Models/Image/My_Data.xml' />

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel name='DM:DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<Strategy class='##Strategy##'/>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>
";
			File.WriteAllText(Path.Combine(root, "_Common", "Models", "Image", "My_Data.xml"), data);
			File.WriteAllText(Path.Combine(root, "Image", "My.xml"), pit);

			db = new PitDatabase(root);
			Assert.NotNull(db);
			Assert.AreEqual(2, db.Entries.Count());

			var file = db.Entries.FirstOrDefault(e => e.Name == "My");
			Assert.NotNull(file);
			Assert.AreEqual(2, file.Versions[0].Files.Count);
		}

		[Test]
		public void GetConfigNoConfig()
		{
			string pit =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='DM'>
		<String/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel name='DM:DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<Strategy class='##Strategy##'/>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>
";
			File.WriteAllText(Path.Combine(root, "Image", "My.xml"), pit);

			db = new PitDatabase(root);
			Assert.NotNull(db);
			Assert.AreEqual(2, db.Entries.Count());

			var file = db.Entries.FirstOrDefault(e => e.Name == "My");
			Assert.NotNull(file);

			var cfg = db.GetConfigByUrl(file.PitUrl);
			Assert.NotNull(cfg);

			Assert.AreEqual(4, cfg.Count);

			Assert.AreEqual("Peach.Pwd", cfg[0].Key);
			Assert.AreEqual("Peach.Cwd", cfg[1].Key);
			Assert.AreEqual("Peach.LogRoot", cfg[2].Key);
			Assert.AreEqual("PitLibraryPath", cfg[3].Key);
		}

		[Test]
		public void AddRemovePit()
		{
			Assert.AreEqual(1, db.Entries.Count());
			Assert.AreEqual(2, db.Libraries.Count());

			const string pit = 
@"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='DM'>
		<String/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel name='DM:DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<Strategy class='##Strategy##'/>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>
";
			var path = Path.Combine(root, "Image", "My.xml");
			File.WriteAllText(path, pit);

			db = new PitDatabase(root);
			Assert.NotNull(db);
			Assert.AreEqual(2, db.Entries.Count());

			var file = db.Entries.FirstOrDefault(e => e.Name == "My");
			Assert.NotNull(file);

			var cfg = db.GetConfigByUrl(file.PitUrl);
			Assert.NotNull(cfg);

			Assert.AreEqual(4, cfg.Count);

			File.Delete(path);

			db = new PitDatabase(root);
			Assert.NotNull(db);
			Assert.AreEqual(1, db.Entries.Count());

			Assert.Null(db.Entries.FirstOrDefault(e => e.Name == "My"));
		}
	}
}
