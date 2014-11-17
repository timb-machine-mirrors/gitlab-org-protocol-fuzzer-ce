using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.OS.Linux.Agent.Monitors;

namespace Peach.Pro.Test.OS.Linux.Agent.Monitors
{
	[TestFixture] [Category("Peach")]
	public class LinuxDebuggerTests
	{
		[Test]
		public void TestFault()
		{
			var self = Assembly.GetExecutingAssembly().Location;

			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("CrashingFileConsumer");
			args["Arguments"] = new Variant(self);
			args["RestartOnEachTest"] = new Variant("true");

			var m = new LinuxDebugger(null, null, args);
			m.SessionStarting();
			m.IterationStarting(1, false);
			Thread.Sleep(5000);
			m.IterationFinished();
			Assert.AreEqual(true, m.DetectedFault());
			Fault fault = m.GetMonitorData();
			Assert.NotNull(fault);
			Assert.AreEqual(1, fault.collectedData.Count);
			Assert.AreEqual("StackTrace.txt", fault.collectedData[0].Key);
			Assert.Greater(fault.collectedData[0].Value.Length, 0);
			Assert.That(fault.description, Is.StringContaining("PossibleStackCorruption"));
			m.SessionFinished();
			m.StopMonitor();
		}

		[Test]
		public void TestNoFault()
		{
			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("CrashingFileConsumer");

			var m = new LinuxDebugger(null, null, args);
			m.SessionStarting();
			m.IterationStarting(1, false);
			Thread.Sleep(5000);
			m.IterationFinished();
			Assert.AreEqual(false, m.DetectedFault());
			m.SessionFinished();
			m.StopMonitor();
		}

		[Test]
		public void TestMissingProgram()
		{
			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("MissingProgram");

			var m = new LinuxDebugger(null, null, args);
			try
			{
				m.SessionStarting();
				Assert.Fail("should throw");
			}
			catch (PeachException ex)
			{
				Assert.AreEqual("GDB was unable to start 'MissingProgram'.", ex.Message);
			}
		}

		[Test]
		public void TestMissingGdb()
		{
			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("MissingProgram");
			args["GdbPath"] = new Variant("MissingGdb");

			var m = new LinuxDebugger(null, null, args);

			try
			{
				m.SessionStarting();
				Assert.Fail("should throw");
			}
			catch (PeachException ex)
			{
				var exp = "Could not start debugger 'MissingGdb'.";
				var act = ex.Message.Substring(0, exp.Length);
				Assert.AreEqual(exp, act);
			}
		}

		[Test]
		public void TestCpuKill()
		{
			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("CrashableServer");
			args["Arguments"] = new Variant("127.0.0.1 12346");
			args["StartOnCall"] = new Variant("Foo");

			var m = new LinuxDebugger(null, null, args);
			m.SessionStarting();
			m.IterationStarting(1, false);

			m.Message("Action.Call", new Variant("Foo"));
			Thread.Sleep(1000);

			var before = DateTime.Now;
			m.IterationFinished();
			var after = DateTime.Now;

			var span = (after - before);

			Thread.Sleep(1000);
			Assert.AreEqual(false, m.DetectedFault());
			m.SessionFinished();
			m.StopMonitor();

			Assert.GreaterOrEqual(span.TotalSeconds, 0.0);
			Assert.LessOrEqual(span.TotalSeconds, 0.5);
		}

		[Test]
		public void TestNoCpuKill()
		{
			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("CrashableServer");
			args["Arguments"] = new Variant("127.0.0.1 0 5");
			args["StartOnCall"] = new Variant("Foo");
			args["NoCpuKill"] = new Variant("true");

			var m = new LinuxDebugger(null, null, args);
			m.SessionStarting();
			m.IterationStarting(1, false);

			m.Message("Action.Call", new Variant("Foo"));
			Thread.Sleep(1000);

			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			m.IterationFinished();
			sw.Stop();

			var span = sw.Elapsed;

			Assert.AreEqual(false, m.DetectedFault());
			m.SessionFinished();
			m.StopMonitor();

			Assert.GreaterOrEqual(span.TotalSeconds, 4);
			Assert.LessOrEqual(span.TotalSeconds, 6);
		}

		[Test]
		public void TestNoCpuKillWaitFail()
		{
			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("CrashableServer");
			args["Arguments"] = new Variant("127.0.0.1 0 5");
			args["StartOnCall"] = new Variant("Foo");
			args["NoCpuKill"] = new Variant("true");
			args["WaitForExitTimeout"] = new Variant("1000");

			var m = new LinuxDebugger(null, null, args);
			m.SessionStarting();
			m.IterationStarting(1, false);

			m.Message("Action.Call", new Variant("Foo"));
			Thread.Sleep(1000);

			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			m.IterationFinished();
			sw.Stop();

			var span = sw.Elapsed;

			Assert.AreEqual(false, m.DetectedFault());
			m.SessionFinished();
			m.StopMonitor();

			Assert.GreaterOrEqual(span.TotalSeconds, 0.5);
			Assert.LessOrEqual(span.TotalSeconds, 1.5);
		}
	}
}
