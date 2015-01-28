using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Pro.Core.Agent.Monitors;

namespace Peach.Pro.Test.Core.Monitors
{
	[TestFixture] [Category("Peach")]
	class ProcessMonitorTests
	{
		string MakeXml(string folder)
		{
			string template = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello' mutable='false'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='LocalAgent'>
		<Monitor class='Process'>
			<Param name='Executable' value='{0}'/>
		</Monitor>
	</Agent>

	<Test name='Default' replayEnabled='false'>
		<Agent ref='LocalAgent'/>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='RandomDeterministic'/>
	</Test>
</Peach>";

			var ret = string.Format(template, folder);
			return ret;
		}

		void Run(string proccessNames, Engine.IterationStartingEventHandler OnIterStart = null)
		{
			string xml = MakeXml(proccessNames);

			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators = new List<string>();
			dom.tests[0].includedMutators.Add("StringCaseMutator");

			RunConfiguration config = new RunConfiguration();

			Engine e = new Engine(null);
			if (OnIterStart != null)
				e.IterationStarting += OnIterStart;
			e.startFuzzing(dom, config);
		}

		[Test, ExpectedException(typeof(PeachException))]
		public void TestBadProcss()
		{
			// Specify a process name that is not running
			Run("some_invalid_process");
		}

		[Test]
		public void TestStartOnCall()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", "CrashableServer" },
				{ "Arguments", "127.0.0.1 {0}".Fmt(TestBase.MakePort(60000, 61000)) },
				{ "StartOnCall", "foo" },
				{"WaitForExitTimeout", "2000" },
				{ "NoCpuKill", "true" },
			};

			var p = new ProcessMonitor(null);
			p.StartMonitor(args);

			p.Message("Action.Call", new Variant("foo"));
			System.Threading.Thread.Sleep(1000);

			var before = DateTime.Now;
			p.IterationFinished();
			var after = DateTime.Now;

			var span = (after - before);

			Assert.AreEqual(false, p.DetectedFault());

			p.SessionFinished();
			p.StopMonitor();

			Assert.GreaterOrEqual(span.TotalSeconds, 1.9);
			Assert.LessOrEqual(span.TotalSeconds, 2.1);
		}

		[Test]
		public void TestCpuKill()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", "CrashableServer" },
				{ "Arguments", "127.0.0.1 {0}".Fmt(TestBase.MakePort(61000, 62000)) },
				{ "StartOnCall", "foo" },
			};

			var p = new ProcessMonitor(null);
			p.StartMonitor(args);

			p.Message("Action.Call", new Variant("foo"));
			System.Threading.Thread.Sleep(1000);

			var before = DateTime.Now;
			p.IterationFinished();
			var after = DateTime.Now;

			var span = (after - before);

			Assert.AreEqual(false, p.DetectedFault());

			p.SessionFinished();
			p.StopMonitor();

			Assert.GreaterOrEqual(span.TotalSeconds, 0.0);
			Assert.LessOrEqual(span.TotalSeconds, 0.5);
		}

		[Test]
		public void TestExitOnCallNoFault()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", "CrashingFileConsumer" },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "NoCpuKill", "true" },
			};

			var p = new ProcessMonitor(null);
			p.StartMonitor(args);

			p.Message("Action.Call", new Variant("foo"));
			p.Message("Action.Call", new Variant("bar"));

			p.IterationFinished();

			Assert.AreEqual(false, p.DetectedFault());

			p.SessionFinished();
			p.StopMonitor();
		}

		[Test]
		public void TestExitOnCallFault()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", "CrashableServer" },
				{ "Arguments", "127.0.0.1 {0}".Fmt(TestBase.MakePort(62000, 63000)) },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "WaitForExitTimeout", "2000" },
				{ "NoCpuKill", "true" },
			};

			var p = new ProcessMonitor(null);
			p.StartMonitor(args);

			p.Message("Action.Call", new Variant("foo"));
			p.Message("Action.Call", new Variant("bar"));

			p.IterationFinished();

			Assert.AreEqual(true, p.DetectedFault());
			Fault f = p.GetMonitorData();
			Assert.NotNull(f);
			Assert.AreEqual("ProcessFailedToExit", f.folderName);

			p.SessionFinished();
			p.StopMonitor();
		}

		[Test]
		public void TestExitTime()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", "CrashableServer" },
				{ "Arguments", "127.0.0.1 {0}".Fmt(TestBase.MakePort(63000, 64000)) },
				{ "RestartOnEachTest", "true" },
			};

			var p = new ProcessMonitor(null);
			p.StartMonitor(args);

			p.SessionStarting();
			p.IterationStarting(1, false);

			var before = DateTime.Now;
			p.IterationFinished();
			var after = DateTime.Now;

			var span = (after - before);

			Assert.AreEqual(false, p.DetectedFault());

			p.SessionFinished();
			p.StopMonitor();

			Assert.GreaterOrEqual(span.TotalSeconds, 0.0);
			Assert.LessOrEqual(span.TotalSeconds, 0.1);
		}

		[Test]
		public void TestExitEarlyFault()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", "CrashingFileConsumer" },
				{ "FaultOnEarlyExit", "true" },
			};

			var p = new ProcessMonitor(null);
			p.StartMonitor(args);

			p.SessionStarting();
			p.IterationStarting(1, false);

			System.Threading.Thread.Sleep(1000);

			p.IterationFinished();

			Assert.AreEqual(true, p.DetectedFault());
			Fault f = p.GetMonitorData();
			Assert.NotNull(f);
			Assert.AreEqual("ProcessExitedEarly", f.folderName);

			p.SessionFinished();
			p.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault1()
		{
			// FaultOnEarlyExit doesn't fault when stop message is sent

			var args = new Dictionary<string, string>
			{
				{ "Executable", "CrashingFileConsumer" },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "FaultOnEarlyExit", "true" },
			};

			var p = new ProcessMonitor(null);
			p.StartMonitor(args);

			p.SessionStarting();
			p.IterationStarting(1, false);

			p.Message("Action.Call", new Variant("foo"));
			p.Message("Action.Call", new Variant("bar"));

			p.IterationFinished();

			Assert.AreEqual(false, p.DetectedFault());

			p.SessionFinished();
			p.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault2()
		{
			// FaultOnEarlyExit faults when StartOnCall is used and stop message is not sent

			var args = new Dictionary<string, string>
			{
				{ "Executable", "CrashingFileConsumer" },
				{ "StartOnCall", "foo" },
				{ "FaultOnEarlyExit", "true" },
			};

			var p = new ProcessMonitor(null);
			p.StartMonitor(args);

			p.SessionStarting();
			p.IterationStarting(1, false);

			p.Message("Action.Call", new Variant("foo"));

			System.Threading.Thread.Sleep(1000);

			p.IterationFinished();

			Assert.AreEqual(true, p.DetectedFault());
			Fault f = p.GetMonitorData();
			Assert.NotNull(f);
			Assert.AreEqual("ProcessExitedEarly", f.folderName);


			p.SessionFinished();
			p.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault3()
		{
			// FaultOnEarlyExit doesn't fault when StartOnCall is used

			var args = new Dictionary<string, string>
			{
				{ "Executable", "CrashableServer" },
				{ "Arguments", "127.0.0.1 {0}".Fmt(TestBase.MakePort(63000, 64000)) },
				{ "StartOnCall", "foo" },
				{ "FaultOnEarlyExit", "true" },
			};

			var p = new ProcessMonitor(null);
			p.StartMonitor(args);

			p.SessionStarting();
			p.IterationStarting(1, false);

			p.Message("Action.Call", new Variant("foo"));

			p.IterationFinished();

			Assert.AreEqual(false, p.DetectedFault());
			
			p.SessionFinished();
			p.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault4()
		{
			// FaultOnEarlyExit doesn't fault when restart every iteration is true

			var args = new Dictionary<string, string>
			{
				{ "Executable", "CrashableServer" },
				{ "Arguments", "127.0.0.1 {0}".Fmt(TestBase.MakePort(63000, 64000)) },
				{ "RestartOnEachTest", "true" },
				{ "FaultOnEarlyExit", "true" },
			};

			var p = new ProcessMonitor(null);
			p.StartMonitor(args);

			p.SessionStarting();
			p.IterationStarting(1, false);

			p.IterationFinished();

			Assert.AreEqual(false, p.DetectedFault());

			p.SessionFinished();
			p.StopMonitor();
		}
	}
}