using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using NLog;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Test;
using Peach.Pro.Core.Publishers;
using Encoding = Peach.Core.Encoding;
using Logger = NLog.Logger;
using Monitor = Peach.Core.Agent.Monitor;

namespace Peach.Pro.Test.Core.Agent
{
	[TestFixture]
	[Category("Peach")]
	public class AgentTests
	{
		SingleInstance si;

		[SetUp]
		public void SetUp()
		{
			si = SingleInstance.CreateInstance("Peach.Core.Test.Agent.AgentTests");
			si.Lock();
		}

		[TearDown]
		public void TearDown()
		{
			si.Dispose();
			si = null;
		}

		public Process process;

		[Monitor("TestLogFunctions", true, IsTest = true)]
		[Parameter("FileName", typeof(string), "File to log to")]
		public class TestLogMonitor : Monitor
		{
			public string FileName { get; set; }

			void Log(string msg, params object[] args)
			{
				using (var writer = new StreamWriter(FileName, true))
				{
					writer.WriteLine(msg, args);
				}
			}

			public TestLogMonitor(string name)
				: base(name)
			{
			}

			public override void StartMonitor(Dictionary<string, string> args)
			{
				base.StartMonitor(args);
				Log("StartMonitor");
			}

			public override void StopMonitor()
			{
				Log("StopMonitor");
			}

			public override void SessionStarting()
			{
				Log("SessionStarting");
			}

			public override void SessionFinished()
			{
				Log("SessionFinished");
			}

			public override void IterationStarting(uint iterationCount, bool isReproduction)
			{
				Log("IterationStarting {0} {1}", iterationCount, isReproduction.ToString().ToLower());
			}

			public override void IterationFinished()
			{
				Log("IterationFinished");
			}

			public override bool DetectedFault()
			{
				Log("DetectedFault");
				return false;
			}

			public override Fault GetMonitorData()
			{
				Log("GetMonitorData");
				return null;
			}

			public override bool MustStop()
			{
				Log("MustStop");
				return false;
			}

			public override void Message(string msg)
			{
				Log("Message {0}", msg);
			}
		}

		[Publisher("AgentKiller", true, IsTest = true)]
		public class AgentKillerPublisher : Publisher
		{
			public AgentTests owner;

			static readonly Logger logger = LogManager.GetCurrentClassLogger();

			public AgentKillerPublisher(Dictionary<string, Variant> args)
				: base(args)
			{
			}

			protected override Logger Logger
			{
				get { return logger; }
			}

			protected RunContext Context
			{
				get
				{
					var dom = Test.parent;
					return dom.context;
				}
			}

			protected override void OnOpen()
			{
				base.OnOpen();

				if (!IsControlIteration && (Iteration % 2) == 1)
				{
					// Lame hack to make sure CrashableServer gets stopped
					Context.agentManager.IterationFinished();

					owner.StopAgent();
					owner.StartAgent();
				}
			}
		}

		public void StartAgent()
		{
			process = Helpers.StartAgent();
		}

		public void StopAgent()
		{
			Helpers.StopAgent(process);
			process = null;
		}

		static string CrashableServer
		{
			get
			{
				var ext = "";
				if (Platform.GetOS() == Platform.OS.Windows)
				{
					ext = ".exe";
				}
				return Utilities.GetAppResourcePath("CrashableServer") + ext;
			}
		}

		static string PlatformMonitor
		{
			get
			{
				if (Platform.GetOS() != Platform.OS.Windows) return "Process";
				if (!Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
					Assert.Ignore("Cannot run the 32bit version of this test on a 64bit operating system.");

				if (Environment.Is64BitProcess && !Environment.Is64BitOperatingSystem)
					Assert.Ignore("Cannot run the 64bit version of this test on a 32bit operating system.");
				return "WindowsDebugger";
			}
		}

		[Test]
		public void TestReconnect()
		{
			var port = TestBase.MakePort(20000, 21000);
			var tmp = Path.GetTempFileName();

			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output' publisher='Remote'>
				<DataModel ref='TheDataModel'/>
			</Action>

			<Action type='open' publisher='Killer'/>
		</State>
	</StateModel>

	<Agent name='RemoteAgent' location='tcp://127.0.0.1:9001'>
		<Monitor class='{0}'>
			<Param name='Executable' value='{1}'/>
			<Param name='Arguments' value='127.0.0.1 {2}'/>
			<Param name='RestartOnEachTest' value='true'/>
			<Param name='FaultOnEarlyExit' value='true'/>
		</Monitor>
		<Monitor class='TestLogFunctions'>
			<Param name='FileName' value='{3}'/>
		</Monitor>
	</Agent>

	<Test name='Default' targetLifetime='iteration'>
		<Agent ref='RemoteAgent'/>
		<StateModel ref='TheState'/>
		<Publisher name='Remote' class='Remote'>
			<Param name='Agent' value='RemoteAgent' />
			<Param name='Class' value='Tcp'/>
			<Param name='Host' value='127.0.0.1' />
			<Param name='Port' value='{2}' />
		</Publisher>
		<Publisher name='Killer' class='AgentKiller'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='StringStatic' />
		</Mutators>
	</Test>
</Peach>".Fmt(PlatformMonitor, CrashableServer, port, tmp);

			try
			{
				StartAgent();

				var parser = new PitParser();
				var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

				var pub = (AgentKillerPublisher)dom.tests[0].publishers[1];
				pub.owner = this;

				var config = new RunConfiguration { range = true, rangeStart = 83, rangeStop = 86 };

				var e = new Engine(null);
				e.Fault += e_Fault;
				e.startFuzzing(dom, config);

				Assert.Greater(faults.Count, 0);

				var contents = File.ReadAllLines(tmp);
				var expected = new[] {
// Iteration 83 (Control & Record)
"StartMonitor", "SessionStarting", "IterationStarting 83 false", "IterationFinished", "DetectedFault", "MustStop", 
// Iteration 83 - Agent is killed (IterationFinished is a hack to kill CrashableServer)
"IterationStarting 83 false", "IterationFinished", 
// Agent is restarted & fault is not detected
"StartMonitor", "SessionStarting", "IterationStarting 84 false", "IterationFinished", "DetectedFault", "MustStop", 
// Agent is killed
"IterationStarting 85 false", "IterationFinished", 
// Agent is restarted & fault is detected
"StartMonitor", "SessionStarting", "IterationStarting 86 false", "IterationFinished", "DetectedFault", "GetMonitorData", "MustStop",
// Reproduction occurs & fault is detected
"IterationStarting 86 true", "IterationFinished", "DetectedFault", "GetMonitorData", "MustStop",
// Fussing stops
"SessionFinished", "StopMonitor"
				};

				Assert.That(contents, Is.EqualTo(expected));
			}
			finally
			{
				if (process != null)
					StopAgent();

				File.Delete(tmp);
			}
		}

		readonly Dictionary<uint, Fault[]> faults = new Dictionary<uint, Fault[]>();

		void e_Fault(RunContext context, uint currentIteration, Peach.Core.Dom.StateModel stateModel, Fault[] faultData)
		{
			faults[currentIteration] = faultData;
		}

		[Test]
		public void TestSoftException()
		{
			var port = TestBase.MakePort(20000, 21000);

			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output' publisher='Remote'>
				<DataModel ref='TheDataModel'/>
			</Action>
			<Action type='output' publisher='Remote'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='RemoteAgent' location='tcp://127.0.0.1:9001'>
		<Monitor class='{0}'>
			<Param name='Executable' value='{1}'/>
			<Param name='Arguments' value='127.0.0.1 {2}'/>
			<Param name='FaultOnEarlyExit' value='true'/>
		</Monitor>
	</Agent>

	<Test name='Default' targetLifetime='iteration'>
		<Agent ref='RemoteAgent'/>
		<StateModel ref='TheState'/>
		<Publisher name='Remote' class='Remote'>
			<Param name='Agent' value='RemoteAgent' />
			<Param name='Class' value='Tcp'/>
			<Param name='Host' value='127.0.0.1' />
			<Param name='Port' value='{2}' />
		</Publisher>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='StringStatic' />
		</Mutators>
	</Test>
</Peach>".Fmt(PlatformMonitor, CrashableServer, port);

			try
			{
				StartAgent();

				var parser = new PitParser();
				var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
				var config = new RunConfiguration { range = true, rangeStart = 83, rangeStop = 86 };

				var e = new Engine(null);
				e.Fault += e_Fault;
				e.startFuzzing(dom, config);

				Assert.Greater(faults.Count, 0);
			}
			finally
			{
				if (process != null)
					StopAgent();
			}
		}

		[Test]
		public void TestBadProcess()
		{
			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='RemoteAgent' location='tcp://127.0.0.1:9001'>
		<Monitor class='{0}'>
			<Param name='Executable' value='MissingProgram'/>
		</Monitor>
	</Agent>

	<Test name='Default'>
		<Agent ref='RemoteAgent'/>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='RandomDeterministic'/>
	</Test>
</Peach>".Fmt(PlatformMonitor);

			try
			{
				StartAgent();

				var parser = new PitParser();
				var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

				var config = new RunConfiguration();

				var e = new Engine(null);

				var ex = Assert.Throws<PeachException>(() => e.startFuzzing(dom, config));

				var msg = Platform.GetOS() != Platform.OS.Windows
					? "Could not start process 'MissingProgram'."
					: "System debugger could not start process 'MissingProgram'.";

				Assert.That(ex.Message, Is.StringStarting(msg));
			}
			finally
			{
				if (process != null)
					StopAgent();
			}
		}

		[Test]
		public void TestNoTcpPort()
		{
			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='RemoteAgent' location='tcp://127.0.0.1'>
		<Monitor class='{0}'>
			<Param name='Executable' value='{1}'/>
			<Param name='Arguments' value='127.0.0.1 0'/>
			<Param name='RestartOnEachTest' value='true'/>
			<Param name='FaultOnEarlyExit' value='true'/>
		</Monitor>
	</Agent>

	<Test name='Default'>
		<Agent ref='RemoteAgent'/>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='RandomDeterministic'/>
	</Test>
</Peach>".Fmt(PlatformMonitor, CrashableServer);

			try
			{
				StartAgent();

				var parser = new PitParser();
				var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

				var config = new RunConfiguration { singleIteration = true };

				var e = new Engine(null);

				e.startFuzzing(dom, config);
			}
			finally
			{
				if (process != null)
					StopAgent();
			}
		}

		[Monitor("LoggingMonitor", true, IsTest = true)]
		public class LoggingMonitor : Monitor
		{
			public LoggingMonitor(string name)
				: base(name)
			{
				history.Add(Name + ".LoggingMonitor");
			}

			public override void StartMonitor(Dictionary<string, string> args)
			{
				history.Add(Name + ".StartMonitor");
			}

			public override void StopMonitor()
			{
				history.Add(Name + ".StopMonitor");
			}

			public override void SessionStarting()
			{
				history.Add(Name + ".SessionStarting");
			}

			public override void SessionFinished()
			{
				history.Add(Name + ".SessionFinished");
			}

			public override void IterationStarting(uint iterationCount, bool isReproduction)
			{
				history.Add(Name + ".IterationStarting");
			}

			public override void IterationFinished()
			{
				history.Add(Name + ".IterationFinished");
			}

			public override bool DetectedFault()
			{
				history.Add(Name + ".DetectedFault");
				return false;
			}

			public override Fault GetMonitorData()
			{
				history.Add(Name + ".GetMonitorData");
				return null;
			}

			public override bool MustStop()
			{
				history.Add(Name + ".MustStop");
				return false;
			}

			public override void Message(string msg)
			{
				history.Add(Name + ".Message." + msg);
			}
		}

		static readonly List<string> history = new List<string>();

		[Test]
		public void TestAgentOrder()
		{
			const string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
			<Action type='call' method='Foo' publisher='Peach.Agent'/>
		</State>
	</StateModel>

	<Agent name='Local1'>
		<Monitor name='Local1.mon1' class='LoggingMonitor'/>
		<Monitor name='Local1.mon2' class='LoggingMonitor'/>
	</Agent>

	<Agent name='Local2'>
		<Monitor name='Local2.mon1' class='LoggingMonitor'/>
		<Monitor name='Local2.mon2' class='LoggingMonitor'/>
	</Agent>

	<Test name='Default'>
		<Agent ref='Local1'/>
		<Agent ref='Local2'/>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='Random'/>
	</Test>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration {singleIteration = true};

			var e = new Engine(null);
			e.startFuzzing(dom, config);

			string[] expected =
			{
				"Local1.mon1.LoggingMonitor",
				"Local1.mon1.StartMonitor",
				"Local1.mon2.LoggingMonitor",
				"Local1.mon2.StartMonitor",
				"Local1.mon1.SessionStarting",
				"Local1.mon2.SessionStarting",
				"Local2.mon1.LoggingMonitor",
				"Local2.mon1.StartMonitor",
				"Local2.mon2.LoggingMonitor",
				"Local2.mon2.StartMonitor",
				"Local2.mon1.SessionStarting",
				"Local2.mon2.SessionStarting",
				"Local1.mon1.IterationStarting",
				"Local1.mon2.IterationStarting",
				"Local2.mon1.IterationStarting",
				"Local2.mon2.IterationStarting",
				"Local1.mon1.Message.Foo",
				"Local1.mon2.Message.Foo",
				"Local2.mon1.Message.Foo",
				"Local2.mon2.Message.Foo",
				"Local2.mon2.IterationFinished",
				"Local2.mon1.IterationFinished",
				"Local1.mon2.IterationFinished",
				"Local1.mon1.IterationFinished",
				"Local1.mon1.DetectedFault",
				"Local1.mon2.DetectedFault",
				"Local2.mon1.DetectedFault",
				"Local2.mon2.DetectedFault",
				"Local1.mon1.MustStop",
				"Local1.mon2.MustStop",
				"Local2.mon1.MustStop",
				"Local2.mon2.MustStop",
				"Local2.mon2.SessionFinished",
				"Local2.mon1.SessionFinished",
				"Local1.mon2.SessionFinished",
				"Local1.mon1.SessionFinished",
				"Local2.mon2.StopMonitor",
				"Local2.mon1.StopMonitor",
				"Local1.mon2.StopMonitor",
				"Local1.mon1.StopMonitor",
			};

			Assert.That(history, Is.EqualTo(expected));

		}

		[Publisher("TestRemoteFile", true, IsTest = true)]
		[Parameter("FileName", typeof(string), "Name of file to open for reading/writing")]
		public class TestRemoteFilePublisher : StreamPublisher
		{
			private static readonly Logger logger = LogManager.GetCurrentClassLogger();
			protected override Logger Logger { get { return logger; } }

			public string FileName { get; set; }

			public TestRemoteFilePublisher(Dictionary<string, Variant> args)
				: base(args)
			{
				stream = new MemoryStream();
			}

			void Log(string msg, params object[] args)
			{
				using (var writer = new StreamWriter(FileName, true))
				{
					writer.WriteLine(msg, args);
				}
			}

			protected override void OnStart()
			{
				Log("OnStart");
			}

			protected override void OnStop()
			{
				Log("OnStop");
			}

			protected override void OnOpen()
			{
				Log("OnOpen");
			}

			protected override void OnClose()
			{
				Log("OnClose");
			}

			protected override void OnAccept()
			{
				Log("OnAccept");
			}

			protected override void OnInput()
			{
				Log("OnInput");

				// Write some bytes!
				stream = new MemoryStream();
				var data = Encoding.ASCII.GetBytes("Returning Data");
				stream.Write(data, 0, data.Length);
				stream.Seek(0, SeekOrigin.Begin);
			}

			public override void WantBytes(long count)
			{
				Log("WantBytes {0}", count);
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				var ret = base.Read(buffer, offset, count);
				Log("Read, Want: {0}, Got: {1}", count - offset, ret);
				return ret;
			}

			protected override void OnOutput(BitwiseStream data)
			{
				// Do a copy to ensure it is remoted properly
				var strm = new BitStream();
				data.CopyTo(strm);

				Log("OnOutput {0}/{1}", strm.Length, strm.LengthBits);
			}

			protected override Variant OnGetProperty(string property)
			{
				Log("GetProperty: {0}", property);

				switch (property)
				{
					case "int":
						return new Variant((int)100);
					case "string":
						return new Variant("This is a string");
					case "bytes":
						return new Variant(new byte[] { 0xff, 0x00, 0xff });
					case "bits":
						var bs = new BitStream();
						bs.Write(new byte[] { 0xfe, 0x00, 0xfe }, 0, 3);
						bs.Seek(0, SeekOrigin.Begin);
						return new Variant(bs);
					default:
						return new Variant();
				}
			}

			protected override void OnSetProperty(string property, Variant value)
			{
				Log("SetProperty {0} {1} {2}", property, value.GetVariantType(), value.ToString());
			}

			protected override Variant OnCall(string method, List<ActionParameter> args)
			{
				var sb = new StringBuilder();

				for (var i = 0; i < args.Count; ++i)
				{
					sb.AppendFormat("Param{0}: {1}", i + 1, Encoding.ASCII.GetString(args[i].dataModel.Value.ToArray()));
					if (i < (args.Count - 1))
						sb.AppendLine();
				}

				Log(sb.ToString());

				return new Variant(Bits.Fmt("{0:L8}{1}", 7, "Success"));
			}
		}

		[Test]
		public void TestRemotePublisher()
		{
			var tmp = Path.GetTempFileName();

			var xml = @"
<Peach>
	<DataModel name='Param1'>
		<Number size='8' value='0x7c'/>
	</DataModel>

	<DataModel name='Param2'>
		<String value='Hello'/>
	</DataModel>

	<DataModel name='Param3'>
		<Blob value='World'/>
	</DataModel>

	<DataModel name='Result'>
		<Number size='8'>
			<Relation type='size' of='str'/>
		</Number>
		<String name='str'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='Param2'/>
			</Action>

			<Action type='accept'/>

			<Action name='input' type='input'>
				<DataModel ref='Param2'/>
			</Action>

			<Action type='setProperty' property='int'>
				<DataModel ref='Param1'/>
			</Action>

			<Action type='getProperty' property='int'>
				<DataModel ref='Param1'/>
			</Action>

			<Action type='getProperty' property='string'>
				<DataModel ref='Param2'/>
			</Action>

			<Action type='getProperty' property='bytes'>
				<DataModel ref='Param2'/>
			</Action>

			<Action type='getProperty' property='bits'>
				<DataModel ref='Param2'/>
			</Action>

			<!--Action name='call' type='call' method='foo'>
				<Param>
					<DataModel ref='Param1'/>
				</Param>
				<Param>
					<DataModel ref='Param2'/>
				</Param>
				<Param>
					<DataModel ref='Param3'/>
				</Param>
				<Result>
					<DataModel ref='Result'/>
				</Result>
			</Action-->
		</State>
	</StateModel>

	<Agent name='RemoteAgent' location='tcp://127.0.0.1:9001'/>

	<Test name='Default'>
		<Agent ref='RemoteAgent'/>
		<StateModel ref='TheState'/>
		<Publisher class='Remote'>
			<Param name='Class' value='TestRemoteFile'/>
			<Param name='Agent' value='RemoteAgent'/>
			<Param name='FileName' value='{0}'/>
		</Publisher>
		<Strategy class='RandomDeterministic'/>
	</Test>
</Peach>".Fmt(tmp);

			try
			{
				StartAgent();

				var parser = new PitParser();
				var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

				var config = new RunConfiguration { singleIteration = true };

				var e = new Engine(null);
				e.startFuzzing(dom, config);

				var contents = File.ReadAllLines(tmp);
				var expected = new[] {
					"OnStart",
					"OnOpen",
					"OnOutput 5/40",
					"OnAccept",
					"OnInput",
					"Read, Want: 14, Got: 14",
					"Read, Want: 0, Got: 0",
					"SetProperty int BitStream 7c",
					"GetProperty: int",
					"GetProperty: string",
					"GetProperty: bytes",
					"GetProperty: bits",
					"OnClose",
					"OnStop",
				};

				Assert.That(contents, Is.EqualTo(expected));

				var st = dom.tests[0].stateModel.states[0];
				//var act = st.actions["call"] as Dom.Actions.Call;
				//Assert.NotNull(act);
				//Assert.NotNull(act.result);
				//Assert.NotNull(act.result.dataModel);
				//Assert.AreEqual(2, act.result.dataModel.Count);
				//Assert.AreEqual(7, (int)act.result.dataModel[0].DefaultValue);
				//Assert.AreEqual("Success", (string)act.result.dataModel[1].DefaultValue);

				var inp = st.actions["input"];
				Assert.AreEqual("Returning Data", inp.dataModel.InternalValue.BitsToString());
			}
			finally
			{
				if (process != null)
					StopAgent();
			}
		}
	}
}
