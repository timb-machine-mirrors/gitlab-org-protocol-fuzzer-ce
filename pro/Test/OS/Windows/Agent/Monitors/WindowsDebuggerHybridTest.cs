using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Analyzers;
using Peach.Core.Test;

namespace Peach.Pro.Test.OS.Windows.Agent.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	[Platform("Win")]
	public class WindowsDebuggerHybridTest
	{
		Fault[] faults = null;

		[SetUp]
		public void SetUp()
		{
			faults = null;

			if (!Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
				Assert.Ignore("Cannot run the 32bit version of this test on a 64bit operating system.");

			if (Environment.Is64BitProcess && !Environment.Is64BitOperatingSystem)
				Assert.Ignore("Cannot run the 64bit version of this test on a 32bit operating system.");

			Assert.Fail("FIXME");
		}

		void _Fault(RunContext context, uint currentIteration, Peach.Core.Dom.StateModel stateModel, Fault[] faults)
		{
			Assert.Null(this.faults);
			Assert.True(context.reproducingFault);
			Assert.AreEqual(330, context.reproducingInitialIteration);
			this.faults = faults;
		}

		void _AppendFault(RunContext context, uint currentIteration, Peach.Core.Dom.StateModel stateModel, Fault[] faults)
		{
			List<Fault> tmp = new List<Fault>();
			if (this.faults != null)
				tmp.AddRange(this.faults);

			tmp.AddRange(faults);
			this.faults = tmp.ToArray();
		}

		string xml = @"
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

	<Agent name='LocalAgent'>
		<Monitor class='WindowsDebugger'>
			<Param name='Executable' value='CrashableServer.exe'/>
			<Param name='Arguments' value='127.0.0.1 44444'/>
		</Monitor>
	</Agent>

	<Test name='Default' targetLifetime='iteration'>
		<Agent ref='LocalAgent'/>
		<StateModel ref='TheState'/>
		<Publisher class='Tcp'>
			<Param name='Host' value='127.0.0.1'/>
			<Param name='Port' value='44444'/>
		</Publisher>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='StringLengthEdgeCase' />
		</Mutators>
	</Test>
</Peach>";

		[Test]
		public void TestNoFault()
		{
			VerifyArch();

			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators = new List<string>();
			dom.tests[0].includedMutators.Add("StringCaseMutator");

			RunConfiguration config = new RunConfiguration();

			Engine e = new Engine(null);
			e.Fault += _Fault;
			e.startFuzzing(dom, config);

			Assert.Null(this.faults);
		}

		[Test]
		public void TestFault()
		{
			VerifyArch();

			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators.Add("StringMutator");

			RunConfiguration config = new RunConfiguration();
			config.range = true;
			config.rangeStart = 330;
			config.rangeStop = 330;

			Engine e = new Engine(null);
			e.Fault += _Fault;
			e.startFuzzing(dom, config);

			Assert.NotNull(this.faults);
			Assert.AreEqual(1, this.faults.Length);
			Assert.AreEqual(FaultType.Fault, this.faults[0].type);
			Assert.AreEqual("WindowsDebugEngine", this.faults[0].detectionSource);
		}

		void VerifyArch()
		{
			if (!Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
				Assert.Ignore("Can't run the 32bit version of test on a 64bit operating system.");
			else if (Environment.Is64BitProcess && !Environment.Is64BitOperatingSystem)
				Assert.Ignore("Can't run the 64bit version of test on a 32bit operating system.");
		}

		[Test]
		public void TestEarlyExit()
		{
			string pit = @"
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

	<Agent name='LocalAgent'>
		<Monitor class='WindowsDebugger'>
			<Param name='Executable' value='CrashingFileConsumer.exe'/>
			<Param name='FaultOnEarlyExit' value='true'/>
		</Monitor>
	</Agent>

	<Test name='Default' targetLifetime='iteration'>
		<Agent ref='LocalAgent'/>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(pit)));

			RunConfiguration config = new RunConfiguration();
			config.range = true;
			config.rangeStart = 1;
			config.rangeStop = 1;

			Engine e = new Engine(null);
			e.Fault += _AppendFault;
			e.ReproFault += _AppendFault;

			try
			{
				e.startFuzzing(dom, config);
				Assert.Fail("Should throw!");
			}
			catch (PeachException ex)
			{
				Assert.AreEqual("Fault detected on control record iteration.", ex.Message);
			}

			Assert.NotNull(this.faults);
			Assert.AreEqual(2, this.faults.Length);
			Assert.AreEqual(FaultType.Fault, this.faults[0].type);
			Assert.AreEqual("SystemDebugger", this.faults[0].detectionSource);
			Assert.AreEqual(FaultType.Fault, this.faults[1].type);
			Assert.AreEqual("WindowsDebugEngine", this.faults[1].detectionSource);
		}

		private class WindowsDebuggerHybrid : Monitor2
		{
			public WindowsDebuggerHybrid(string name) : base(name)
			{
			}
		}

		[Test]
		public void TestExitEarlyFault()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", "CrashingFileConsumer.exe" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new WindowsDebuggerHybrid(null);
			w.StartMonitor(args);
			w.IterationStarting(new IterationStartingArgs());

			System.Threading.Thread.Sleep(1000);

			w.IterationFinished();

			Assert.AreEqual(true, w.DetectedFault());
			var f = w.GetMonitorData();
			Assert.NotNull(f);
			Assert.NotNull(f.Fault);
			Assert.AreEqual("Process exited early.", f.Title);

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault1()
		{
			// FaultOnEarlyExit doesn't fault when stop message is sent

			var args = new Dictionary<string, string>
			{
				{ "Executable", "CrashingFileConsumer.exe" },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new WindowsDebuggerHybrid(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(new IterationStartingArgs());

			w.Message("foo");
			w.Message("bar");

			w.IterationFinished();

			Assert.AreEqual(false, w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault2()
		{
			// FaultOnEarlyExit faults when StartOnCall is used and stop message is not sent

			var args = new Dictionary<string, string>
			{
				{ "Executable", "CrashingFileConsumer.exe" },
				{ "StartOnCall", "foo" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new WindowsDebuggerHybrid(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(new IterationStartingArgs());

			w.Message("foo");

			System.Threading.Thread.Sleep(1000);

			w.IterationFinished();

			Assert.AreEqual(true, w.DetectedFault());
			var f = w.GetMonitorData();
			Assert.NotNull(f);
			Assert.NotNull(f.Fault);
			Assert.AreEqual("Process exited early.", f.Title);

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault3()
		{
			// FaultOnEarlyExit doesn't fault when StartOnCall is used

			var args = new Dictionary<string, string>
			{
				{ "Executable", "CrashableServer.exe" },
				{ "Arguments", "127.0.0.1 6789" },
				{ "StartOnCall", "foo" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new WindowsDebuggerHybrid(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(new IterationStartingArgs());

			w.Message("foo");

			w.IterationFinished();

			Assert.AreEqual(false, w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault4()
		{
			// FaultOnEarlyExit doesn't fault when restart every iteration is true

			var args = new Dictionary<string, string>
			{
				{ "Executable", "CrashableServer.exe" },
				{ "Arguments", "127.0.0.1 6789" },
				{ "RestartOnEachTest", "true" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new WindowsDebuggerHybrid(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(new IterationStartingArgs());

			w.IterationFinished();

			Assert.AreEqual(false, w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestRestartAfterFault()
		{
			var startCount = 0;
			var iteration = 0;

			var runner = new MonitorRunner("WindowsDebugger", new Dictionary<string, string>
			{
				{ "Executable", "CrashableServer" },
				{ "Arguments", "127.0.0.1 0" },
				{ "RestartAfterFault", "true" },
			})
			{
				StartMonitor = (m, args) =>
				{
					m.InternalEvent += (s, e) => ++startCount;
					m.StartMonitor(args);
				},
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Should not have detected a fault");

					return ++iteration == 2;
				}
			}
			;

			var f = runner.Run(5);

			Assert.AreEqual(0, f.Length);
			Assert.AreEqual(2, startCount);
		}
	}
}
