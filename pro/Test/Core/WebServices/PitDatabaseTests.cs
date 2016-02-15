using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using Peach.Core;
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
		/*
		 * ArgumentException for posting bad pit.config and pit.agents
		 * UnauthorizedAccessException for posting to locked pit
		 * Verify posting agents with same name/location properly get merged (wizard result)
		 * Verify Param StartMode gets translated properly
		 * POST monitor map name vs key
		 */

		TempDirectory root;
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
	<Include ns='SM' src='file:##PitLibraryPath##/_Common/Models/Image/IMG_Data.xml' />

	<Test name='Default'>
		<Agent ref='TheAgent'/>
		<Strategy class='Random'/>
		<StateModel ref='SM:SM' />
		<Publisher class='Null'/>
	</Test>
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
		<Strategy class='Random'/>
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

		const string pitNoConfig =
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
		<Strategy class='Random'/>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>
";

		[SetUp]
		public void SetUp()
		{
			root = new TempDirectory();

			var cat = Path.Combine(root.Path, "Image");
			Directory.CreateDirectory(cat);

			var mod = Path.Combine(root.Path, "_Common", "Models", "Image");
			Directory.CreateDirectory(mod);

			File.WriteAllText(Path.Combine(mod, "IMG_Data.xml"), modelExample);
			File.WriteAllText(Path.Combine(cat, "IMG.xml"), pitExample);
			File.WriteAllText(Path.Combine(cat, "IMG.xml.config"), configExample);

			db = new PitDatabase();
			db.ValidationEventHandler += OnValidationEvent;
			db.Load(root.Path);
		}

		[TearDown]
		public void TearDown()
		{
			root.Dispose();
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

			Assert.False(libs[1].Locked);
			Assert.AreEqual(1, libs[1].Versions.Count);
			Assert.False(libs[1].Versions[0].Locked);
		}

		[Test]
		public void TestNoConfig()
		{
			File.WriteAllText(Path.Combine(root.Path, "Image", "IMG Copy.xml"), pitExample);

			db.Load(root.Path);

			var ent = db.Entries.ToList();
			Assert.AreEqual(2, ent.Count);

			var img = ent.First(e => e.Name == "IMG");

			var pit1 = db.GetPitByUrl(img.PitUrl);
			Assert.NotNull(pit1);

			var imgCopy = ent.First(e => e.Name == "IMG Copy");

			var pit2 = db.GetPitByUrl(imgCopy.PitUrl);
			var cfg2 = pit2.Config;
			Assert.AreEqual(5, cfg2.Count);

			// Should include system defines

			Assert.AreEqual("Peach.OS", cfg2[0].Key);
			Assert.AreEqual("Peach.Pwd", cfg2[1].Key);
			Assert.AreEqual("Peach.Cwd", cfg2[2].Key);
			Assert.AreEqual("Peach.LogRoot", cfg2[3].Key);
			Assert.AreEqual("PitLibraryPath", cfg2[4].Key);

			// Saving should create the file
			var cfg = pit2.Versions[0].Files[0].Name + ".config";
			Assert.False(File.Exists(cfg), ".config file should not exist");

			db.UpdatePitById(pit2.Id, new Pit { Config = cfg2 });

			// System defines should not be in the file
			Assert.True(File.Exists(cfg), ".config file should exist");

			var defs = PitDefines.ParseFile(cfg);
			Assert.AreEqual(0, defs.Platforms.Count);
			Assert.AreNotEqual(0, defs.SystemDefines.Count);
		}

		[Test]
		public void TestCopyPro()
		{
			var ent = db.Entries.ElementAt(0);
			var lib = db.Libraries.ElementAt(1);
			var pit = db.GetPitById(ent.Id);

			var newName = "IMG Copy";
			var newDesc = "My copy of the img pit";

			var newUrl = db.CopyPit(lib.LibraryUrl, pit.PitUrl, newName, newDesc);

			Assert.NotNull(newUrl);

			Assert.AreEqual(2, db.Entries.Count());
			Assert.AreEqual(1, lib.Versions[0].Pits.Count);

			var newPit = db.GetPitByUrl(newUrl);

			Assert.NotNull(newPit);

			var newXml = File.ReadAllText(newPit.Versions[0].Files[0].Name);
			Assert.NotNull(newXml);

			Assert.AreEqual(newName, newPit.Name);
			Assert.AreEqual(newDesc, newPit.Description);
			Assert.AreEqual(Environment.UserName, newPit.User);

			var expName = Path.Combine(root.Path, "User", "Image", "IMG Copy.xml");
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

			var ex = Assert.Throws<ArgumentException>(() => db.CopyPit(lib.LibraryUrl, pit.PitUrl, newName, newDesc));
			Assert.AreEqual("name", ex.ParamName);
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
			var ent = db.Entries.ElementAt(0);
			var lib = db.Libraries.ElementAt(1);
			var pit = db.GetPitById(ent.Id);

			var newUrl1 = db.CopyPit(lib.LibraryUrl, pit.PitUrl, "IMG Copy 1", "Img desc 1");
			Assert.NotNull(newUrl1);

			var newUrl2 = db.CopyPit(lib.LibraryUrl, newUrl1, "IMG Copy 2", "Img desc 2");
			Assert.NotNull(newUrl2);


			Assert.AreEqual(3, db.Entries.Count());
			Assert.AreEqual(2, lib.Versions[0].Pits.Count);

			var newPit = db.GetPitByUrl(newUrl2);

			Assert.NotNull(newPit);

			var newXml = File.ReadAllText(newPit.Versions[0].Files[0].Name);
			Assert.NotNull(newXml);

			var expName = Path.Combine(root.Path, "User", "Image", "IMG Copy 2.xml");
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
			var ent = db.Entries.First();
			var pit = db.GetPitById(ent.Id);
			var cfg = pit.Config;

			Assert.NotNull(cfg);
			Assert.AreEqual(7, cfg.Count);

			Assert.AreEqual("Strategy", cfg[0].Key);
			Assert.AreEqual("SomeMiscVariable", cfg[1].Key);
			Assert.AreEqual("Peach.OS", cfg[2].Key);
			Assert.AreEqual("Peach.Pwd", cfg[3].Key);
			Assert.AreEqual("Peach.Cwd", cfg[4].Key);
			Assert.AreEqual("Peach.LogRoot", cfg[5].Key);

			// PitLibraryPath is special, and gets turned into a System type
			// regardless of what is in the pit .config
			Assert.AreEqual("PitLibraryPath", cfg[6].Key);
			Assert.AreNotEqual(".", cfg[6].Value);
		}

		[Test]
		public void TestSetPitConfig()
		{
			var ent = db.Entries.First();
			var pit = db.GetPitById(ent.Id);
			var cfg = pit.Config;

			Assert.NotNull(cfg);
			Assert.AreEqual(7, cfg.Count);
			Assert.AreEqual("SomeMiscVariable", cfg[1].Key);
			Assert.AreNotEqual("Foo Bar Baz", cfg[1].Value);
			cfg[1].Value = "Foo Bar Baz";

			db.UpdatePitById(ent.Id, pit);

			var file = pit.Versions[0].Files[0].Name + ".config";
			var defines = PitDefines.ParseFile(file);
			Assert.NotNull(defines);
			Assert.AreEqual(1, defines.Platforms.Count);
			var defs = defines.Platforms[0].Defines;
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
		public void TestOptionalParams()
		{
			var ent = db.Entries.First();
			var pit = db.GetPitById(ent.Id);

			Assert.NotNull(pit.Config);
			Assert.NotNull(pit.Metadata);
			Assert.NotNull(pit.Metadata.Defines);

			Assert.AreEqual(7, pit.Config.Count);
			Assert.AreEqual(2, pit.Metadata.Defines.Count);

			foreach (var item in pit.Metadata.Defines.SelectMany(d => d.Items)) 
				Assert.False(item.Optional, "Define should not be optional");

			var file = pit.Versions[0].Files[0].Name + ".config";
			var defs = PitDefines.ParseFile(file);

			Assert.NotNull(defs);

			// File shouldn't contain optional
			StringAssert.DoesNotContain("optional", File.ReadAllText(file));

			db.UpdatePitById(ent.Id, pit);

			// After saving, file still shouldn't contain optional
			StringAssert.DoesNotContain("optional", File.ReadAllText(file));

			Assert.NotNull(defs.Platforms);
			Assert.AreEqual(1, defs.Platforms.Count);

			defs.Platforms[0].Defines.Add(new PitDefines.StringDefine
			{
				Name = "Optional String",
				Key = "OptStr",
				Value = "",
				Description = "Desc",
				OptionalAttr = true
			});

			XmlTools.Serialize(file, defs);

			pit = db.GetPitById(ent.Id);

			Assert.NotNull(pit.Config);
			Assert.NotNull(pit.Metadata);
			Assert.NotNull(pit.Metadata.Defines);

			Assert.AreEqual(8, pit.Config.Count);
			Assert.AreEqual(2, pit.Metadata.Defines.Count);

			var seq = pit.Metadata.Defines.SelectMany(d => d.Items).ToList();

			Assert.AreEqual(8, seq.Count);

			for (var i = 0; i < seq.Count; ++i)
			{
				if (i == 2)
					Assert.True(seq[i].Optional, "seq[{0}] should be optional".Fmt(i));
				else
					Assert.False(seq[i].Optional, "seq[{0}] should not be optional".Fmt(i));
			}

			var text = File.ReadAllText(file);
			StringAssert.Contains("optional=\"true\"", text);
			StringAssert.DoesNotContain("optional=\"false\"", text);
		}

		[Test]
		public void TestSaveMonitorsOmitDefaults()
		{
			// Ensure default monitor parameters are not written to xml

			var ent = db.Entries.First();
			Assert.NotNull(ent);

			const string json = @"
{
	""agents"" : [
		{
			""name"":""Agent0"",
			""agentUrl"":""local://"",
			""monitors"": [
				{
					""monitorClass"":""Process"",
					""map"": [
						{ ""name"":""Executable"", ""value"":""foo"" },
						{ ""name"":""StartOnCall"", ""value"":"""" },
						{ ""name"":""WaitForExitOnCall"", ""value"":null },
						{ ""name"":""NoCpuKill"", ""value"":""false"" }
					],
				}
			]
		}
	]
}";

			var data = JsonConvert.DeserializeObject<Pit>(json);

			var pit = db.UpdatePitById(ent.Id, data);

			var parser = new PitParser();

			var opts = new Dictionary<string, object>();
			var defs = new Dictionary<string, string>
			{
				{"PitLibraryPath", root.Path},
				{"Strategy", "Random"}
			};
			opts[PitParser.DEFINED_VALUES] = defs;

			var dom = parser.asParser(opts, pit.Versions[0].Files[0].Name);

			Assert.AreEqual(1, dom.tests[0].agents.Count);

			Assert.AreEqual("Agent0", dom.tests[0].agents[0].Name);
			Assert.AreEqual("local://", dom.tests[0].agents[0].location);
			Assert.AreEqual(1, dom.tests[0].agents[0].monitors.Count);

			var mon = dom.tests[0].agents[0].monitors[0];
			Assert.AreEqual("Process", mon.cls);
			Assert.AreEqual(2, mon.parameters.Count);
			Assert.True(mon.parameters.ContainsKey("Executable"), "Should contain key Executable");
			Assert.AreEqual("foo", mon.parameters["Executable"].ToString());
			Assert.True(mon.parameters.ContainsKey("NoCpuKill"), "Should contain key NoCpuKill");
			Assert.AreEqual("false", mon.parameters["NoCpuKill"].ToString());

			Assert.NotNull(pit);
			Assert.NotNull(pit.Agents);

			Assert.AreEqual(1, pit.Agents.Count);
			Assert.AreEqual("Agent0", pit.Agents[0].Name);
			Assert.AreEqual("local://", pit.Agents[0].AgentUrl);
			Assert.AreEqual(1, pit.Agents[0].Monitors.Count);
			Assert.AreEqual(2, pit.Agents[0].Monitors[0].Map.Count);
			Assert.AreEqual("Executable", pit.Agents[0].Monitors[0].Map[0].Key);
			Assert.AreEqual("foo", pit.Agents[0].Monitors[0].Map[0].Value);
			Assert.AreEqual("NoCpuKill", pit.Agents[0].Monitors[0].Map[1].Key);
			Assert.AreEqual("false", pit.Agents[0].Monitors[0].Map[1].Value);

			// Only Key/Value are expected to be set
			// Name/Description come from pit.metadata.monitors
			Assert.AreEqual(null, pit.Agents[0].Monitors[0].Map[0].Name);
			Assert.AreEqual(null, pit.Agents[0].Monitors[0].Map[0].Description);
		}

		[Test]
		public void TestSaveMonitors()
		{
			var ent = db.Entries.First();
			Assert.NotNull(ent);

			var json = @"
[
{
	""name"":""Agent0"",
	""agentUrl"":""local://"",
	""monitors"": [
		{
			""monitorClass"":""PageHeap"",
			""map"": [
				{ ""name"":""Executable"", ""value"":""Foo.exe"" },
				{ ""name"":""WinDbgPath"", ""value"":""C:\\WinDbg""  }
			],
		},
		{
			""monitorClass"":""WindowsDebugger"",
			""map"": [
				{ ""name"":""Executable"", ""value"":""Foo.exe"" },
				{ ""name"":""Arguments"", ""value"":""--arg"" },
				{ ""name"":""IgnoreFirstChanceGuardPage"", ""value"":""true"" }
			],
		},
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
	""name"":""Agent1"",
	""agentUrl"":""tcp://remotehostname"",
	""monitors"": [
		{
			""monitorClass"":""Pcap"",
			""map"": [
				{""name"":""Device"", ""value"":""eth0"" },
				{""name"":""Filter"", ""value"":""tcp port 80"" }
			],
		},
		{
			""monitorClass"":""Pcap"",
			""map"": [
				{""name"":""Device"", ""value"":""eth0"" },
				{""name"":""Filter"", ""value"":""tcp port 8080"" }
			],
		},
	]
},
{
	""name"":""Agent2"",
	""agentUrl"":""tcp://remotehostname2"",
	""monitors"": [
		{
			""monitorClass"":""Pcap"",
			""map"": [
				{""name"":""Device"", ""value"":""eth0"" },
				{""name"":""Filter"", ""value"":""tcp port 80"" }
			],
		}
	]
}
]";

			var agents = JsonConvert.DeserializeObject<List<Pro.Core.WebServices.Models.Agent>>(json);
			Assert.NotNull(agents);

			var pit = db.UpdatePitById(ent.Id, new Pit { Agents = agents });
			Assert.NotNull(pit);

			var parser = new PitParser();

			var defs = new Dictionary<string, string>
			{
				{"PitLibraryPath", root.Path}, 
				{"Strategy", "Random"}
			};

			var opts = new Dictionary<string, object>
			{
				{PitParser.DEFINED_VALUES, defs}
			};

			var dom = parser.asParser(opts, pit.Versions[0].Files[0].Name);

			Assert.AreEqual(3, dom.tests[0].agents.Count);

			Assert.AreEqual("Agent0", dom.tests[0].agents[0].Name);
			Assert.AreEqual("local://", dom.tests[0].agents[0].location);
			Assert.AreEqual(3, dom.tests[0].agents[0].monitors.Count);

			VerifyMonitor(agents[0].Monitors[0], dom.tests[0].agents[0].monitors[0]);
			VerifyMonitor(agents[0].Monitors[1], dom.tests[0].agents[0].monitors[1]);
			VerifyMonitor(agents[0].Monitors[2], dom.tests[0].agents[0].monitors[2]);

			Assert.AreEqual("Agent1", dom.tests[0].agents[1].Name);
			Assert.AreEqual("tcp://remotehostname", dom.tests[0].agents[1].location);
			Assert.AreEqual(2, dom.tests[0].agents[1].monitors.Count);

			VerifyMonitor(agents[1].Monitors[0], dom.tests[0].agents[1].monitors[0]);
			VerifyMonitor(agents[1].Monitors[1], dom.tests[0].agents[1].monitors[1]);

			Assert.AreEqual("Agent2", dom.tests[0].agents[2].Name);
			Assert.AreEqual("tcp://remotehostname2", dom.tests[0].agents[2].location);
			Assert.AreEqual(1, dom.tests[0].agents[2].monitors.Count);

			VerifyMonitor(agents[2].Monitors[0], dom.tests[0].agents[2].monitors[0]);
		}

		private void VerifyMonitor(Monitor jsonMon, Peach.Core.Dom.Monitor domMon)
		{
			Assert.AreEqual(jsonMon.MonitorClass, domMon.cls);
			Assert.AreEqual(jsonMon.Map.Count, domMon.parameters.Count);

			foreach (var item in jsonMon.Map)
			{
				var key = item.Key ?? item.Name;
				Assert.True(domMon.parameters.ContainsKey(key));
				Assert.AreEqual(item.Value, (string)domMon.parameters[key]);
			}
		}

		[Test]
		public void TestSaveProcessMonitors()
		{
			var ent = db.Entries.First();
			Assert.NotNull(ent);

			const string json = @"
[
	{
		""name"":""Agent0"",
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
			}
		]
	}
]";

			var agents = JsonConvert.DeserializeObject<List<Pro.Core.WebServices.Models.Agent>>(json);

			var pit = db.UpdatePitById(ent.Id, new Pit { Agents = agents });

			Assert.NotNull(pit);

			var parser = new PitParser();

			var opts = new Dictionary<string, object>();
			var defs = new Dictionary<string, string>
			{
				{"PitLibraryPath", root.Path},
				{"Strategy", "Random"}
			};
			opts[PitParser.DEFINED_VALUES] = defs;

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
			File.WriteAllText(Path.Combine(root.Path, "Image", "Remote.xml"), remoteInclude);

			db = new PitDatabase(root.Path);
			Assert.NotNull(db);
			Assert.AreEqual(2, db.Entries.Count());

			var file = db.Entries.FirstOrDefault(e => e.Name == "Remote");
			Assert.NotNull(file);
			var pit = db.GetPitById(file.Id);
			Assert.NotNull(pit);
			Assert.AreEqual(2, pit.Versions[0].Files.Count);
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
			string xml =
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
		<Strategy class='Random'/>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>
";
			File.WriteAllText(Path.Combine(root.Path, "_Common", "Models", "Image", "My_Data.xml"), data);
			File.WriteAllText(Path.Combine(root.Path, "Image", "My.xml"), xml);

			db = new PitDatabase(root.Path);
			Assert.NotNull(db);
			Assert.AreEqual(2, db.Entries.Count());

			var file = db.Entries.FirstOrDefault(e => e.Name == "My");
			Assert.NotNull(file);
			var pit = db.GetPitById(file.Id);
			Assert.NotNull(pit);
			Assert.AreEqual(2, pit.Versions[0].Files.Count);
		}

		[Test]
		public void GetConfigNoConfig()
		{
			File.WriteAllText(Path.Combine(root.Path, "Image", "My.xml"), pitNoConfig);

			db = new PitDatabase(root.Path);
			Assert.NotNull(db);
			Assert.AreEqual(2, db.Entries.Count());

			var file = db.Entries.FirstOrDefault(e => e.Name == "My");
			Assert.NotNull(file);

			var pit = db.GetPitByUrl(file.PitUrl);
			Assert.NotNull(pit);
			var cfg = pit.Config;
			Assert.NotNull(cfg);

			Assert.AreEqual(5, cfg.Count);

			Assert.AreEqual("Peach.OS", cfg[0].Key);
			Assert.AreEqual("Peach.Pwd", cfg[1].Key);
			Assert.AreEqual("Peach.Cwd", cfg[2].Key);
			Assert.AreEqual("Peach.LogRoot", cfg[3].Key);
			Assert.AreEqual("PitLibraryPath", cfg[4].Key);
		}

		[Test]
		public void AddRemovePit()
		{
			Assert.AreEqual(1, db.Entries.Count());
			Assert.AreEqual(2, db.Libraries.Count());

			var path = Path.Combine(root.Path, "Image", "My.xml");
			File.WriteAllText(path, pitNoConfig);

			db = new PitDatabase(root.Path);
			Assert.NotNull(db);
			Assert.AreEqual(2, db.Entries.Count());

			var file = db.Entries.FirstOrDefault(e => e.Name == "My");
			Assert.NotNull(file);

			var pit = db.GetPitByUrl(file.PitUrl);
			Assert.NotNull(pit);
			var cfg = pit.Config;
			Assert.NotNull(cfg);

			Assert.AreEqual(5, cfg.Count);

			File.Delete(path);

			db = new PitDatabase(root.Path);
			Assert.NotNull(db);
			Assert.AreEqual(1, db.Entries.Count());

			Assert.Null(db.Entries.FirstOrDefault(e => e.Name == "My"));
		}

		[Test]
		public void TestConfigInjection()
		{
			var cwd = Directory.GetCurrentDirectory();
			try
			{
				Directory.SetCurrentDirectory(root.Path);

				var path = Path.Combine(root.Path, "Image", "inject.xml");
				File.WriteAllText(path, pitExample);

				var asm = Assembly.GetExecutingAssembly();
				var json = Utilities.LoadStringResource(asm, "Peach.Pro.Test.Core.Resources.pit.json");
				var cfg = JsonConvert.DeserializeObject<PitConfig>(json);

				var cfgFile = Path.Combine(root.Path, "Image", "IMG.xml.config");
				var extras = new List<KeyValuePair<string, string>>();
				var defs = PitDefines.ParseFile(cfgFile, extras);

				var evaluated = defs.Evaluate();
				PitInjector.InjectDefines(cfg, defs, evaluated);

				var opts = new Dictionary<string, object>();
				opts[PitParser.DEFINED_VALUES] = evaluated;


				var parser = new PitParser();
				var dom = parser.asParser(opts, path);

				PitInjector.InjectAgents(cfg, evaluated, dom);

				var agent = dom.agents.First();
				var monitor = agent.monitors.First();
				Assert.AreEqual("local://", agent.location);
				Assert.AreEqual(false, monitor.parameters.Any(x => x.Key == "WaitForExitTimeout"), "WaitForExitTimeout should be omitted");
				Assert.AreEqual("http://127.0.0.1:89/", (string)monitor.parameters.Single(x => x.Key == "Arguments").Value);

				var config = new RunConfiguration
				{
					singleIteration = true,
				};

				var e = new Engine(null);
				e.startFuzzing(dom, config);
			}
			finally
			{
				Directory.SetCurrentDirectory(cwd);
			}
		}
	}
}
