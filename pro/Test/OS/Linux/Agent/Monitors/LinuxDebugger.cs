using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.OS.Linux.Agent.Monitors;

namespace Peach.Pro.Test.OS.Linux.Agent.Monitors
{
	[TestFixture] [Category("Peach")]
	public class LinuxDebuggerTests
	{
		[Test]
		public void TestFault()
		{
			var self = Utilities.ExecutionDirectory;

			var args = new Dictionary<string, string>();
			args["Executable"] = "CrashingFileConsumer";
			args["Arguments"] = self;
			args["RestartOnEachTest"] = "true";

			var m = new LinuxDebugger(null);
			m.StartMonitor(args);
			m.SessionStarting();
			m.IterationStarting(null);
			Thread.Sleep(5000);
			m.IterationFinished();
			Assert.AreEqual(true, m.DetectedFault());
			Fault fault = m.GetMonitorData();
			Assert.NotNull(fault);
			Assert.AreEqual(3, fault.collectedData.Count);
			Assert.AreEqual("StackTrace.txt", fault.collectedData[0].Key);
			Assert.AreEqual("stdout.log", fault.collectedData[1].Key);
			Assert.AreEqual("stderr.log", fault.collectedData[2].Key);
			Assert.Greater(fault.collectedData[0].Value.Length, 0);
			Assert.That(fault.description, Is.StringContaining("PossibleStackCorruption"));
			m.SessionFinished();
			m.StopMonitor();
		}

		[Test]
		public void TestNoFault()
		{
			var args = new Dictionary<string, string>();
			args["Executable"] = "CrashingFileConsumer";

			var m = new LinuxDebugger(null);
			m.StartMonitor(args);
			m.SessionStarting();
			m.IterationStarting(null);
			Thread.Sleep(5000);
			m.IterationFinished();
			Assert.AreEqual(false, m.DetectedFault());
			m.SessionFinished();
			m.StopMonitor();
		}

		[Test]
		public void TestMissingProgram()
		{
			var args = new Dictionary<string, string>();
			args["Executable"] = "MissingProgram";

			var m = new LinuxDebugger(null);
			m.StartMonitor(args);
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
			var args = new Dictionary<string, string>();
			args["Executable"] = "MissingProgram";
			args["GdbPath"] = "MissingGdb";

			var m = new LinuxDebugger(null);
			m.StartMonitor(args);

			try
			{
				m.SessionStarting();
				Assert.Fail("should throw");
			}
			catch (PeachException ex)
			{
				const string exp = "Could not start debugger 'MissingGdb'.";
				var act = ex.Message.Substring(0, exp.Length);
				Assert.AreEqual(exp, act);
			}
		}

		[Test]
		public void TestCpuKill()
		{
			var args = new Dictionary<string, string>();
			args["Executable"] = "CrashableServer";
			args["Arguments"] = "127.0.0.1 12346";
			args["StartOnCall"] = "Foo";

			var m = new LinuxDebugger(null);
			m.StartMonitor(args);
			m.SessionStarting();
			m.IterationStarting(null);

			m.Message("Foo");
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
			var args = new Dictionary<string, string>();
			args["Executable"] = "CrashableServer";
			args["Arguments"] = "127.0.0.1 0 5";
			args["StartOnCall"] = "Foo";
			args["NoCpuKill"] = "true";

			var m = new LinuxDebugger(null);
			m.StartMonitor(args);
			m.SessionStarting();
			m.IterationStarting(null);

			m.Message("Foo");
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
			var args = new Dictionary<string, string>();
			args["Executable"] = "CrashableServer";
			args["Arguments"] = "127.0.0.1 0 5";
			args["StartOnCall"] = "Foo";
			args["NoCpuKill"] = "true";
			args["WaitForExitTimeout"] = "1000";

			var m = new LinuxDebugger(null);
			m.StartMonitor(args);
			m.SessionStarting();
			m.IterationStarting(null);

			m.Message("Foo");
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
