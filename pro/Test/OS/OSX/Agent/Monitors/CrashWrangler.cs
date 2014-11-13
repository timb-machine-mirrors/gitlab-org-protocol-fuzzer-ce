using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.OS.OSX.Agent.Monitors;

namespace Peach.Pro.Test.OS.OSX.Agent.Monitors
{
	[TestFixture] [Category("Peach")]
	public class CrashWranglerTest
	{
		[Test]
		public void BadHandler()
		{
			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("foo");
			args["ExecHandler"] = new Variant("foo");
			
			var w = new CrashWrangler(null, "name", args);
			const string expected = "CrashWrangler could not start handler \"foo\" - No such file or directory.";
			Assert.Throws<PeachException>(w.SessionStarting, expected);
		}

		[Test]
		public void BadCommand()
		{
			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("foo");

			var w = new CrashWrangler(null, "name", args);
			const string expected = "CrashWrangler handler could not find command \"foo\".";
			Assert.Throws<PeachException>(w.SessionStarting, expected);
		}
		
		[Test]
		public void TestNoFault()
		{
			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("echo");
			args["Arguments"] = new Variant("hello");

			var w = new CrashWrangler(null, "name", args);
			w.SessionStarting();
			w.IterationStarting(0, false);
			Thread.Sleep(1000);
			w.IterationFinished();
			Assert.AreEqual(false, w.DetectedFault());
			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestStopping()
		{
			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("nc");
			args["Arguments"] = new Variant("-l 12345");

			var w = new CrashWrangler(null, "name", args);
			w.SessionStarting();
			w.IterationStarting(0, false);
			Thread.Sleep(1000);
			w.IterationFinished();
			Assert.AreEqual(false, w.DetectedFault());
			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestStartOnCall()
		{
			var foo = new Variant("foo");

			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("nc");
			args["Arguments"] = new Variant("-l 12345");
			args["StartOnCall"] = foo;
			args["WaitForExitTimeout"] = new Variant("2000");
			args["NoCpuKill"] = new Variant("true");

			var w = new CrashWrangler(null, "name", args);

			w.Message("Action.Call", foo);
			Thread.Sleep(1000);

			var before = DateTime.Now;
			w.IterationFinished();
			var after = DateTime.Now;

			var span = (after - before);

			Assert.AreEqual(false, w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();

			Assert.GreaterOrEqual(span.TotalSeconds, 1.8);
			Assert.LessOrEqual(span.TotalSeconds, 2.2);
		}

		[Test]
		public void TestCpuKill()
		{
			var foo = new Variant("foo");
			
			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("nc");
			args["Arguments"] = new Variant("-l 12345");
			args["StartOnCall"] = foo;

			var w = new CrashWrangler(null, "name", args);

			w.Message("Action.Call", foo);
			Thread.Sleep(1000);

			var before = DateTime.Now;
			w.IterationFinished();
			var after = DateTime.Now;

			var span = (after - before);

			Assert.AreEqual(false, w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();

			Assert.GreaterOrEqual(span.TotalSeconds, 0.0);
			Assert.LessOrEqual(span.TotalSeconds, 0.5);
		}

		[Test]
		public void TestExitOnCallNoFault()
		{
			var foo = new Variant("foo");
			var bar = new Variant("bar");

			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("echo");
			args["Arguments"] = new Variant("hello");
			args["StartOnCall"] = foo;
			args["WaitForExitOnCall"] = bar;
			args["NoCpuKill"] = new Variant("true");

			var w = new CrashWrangler(null, "name", args);

			w.Message("Action.Call", foo);
			w.Message("Action.Call", bar);

			w.IterationFinished();

			Assert.AreEqual(false, w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitOnCallFault()
		{
			var foo = new Variant("foo");
			var bar = new Variant("bar");

			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("nc");
			args["Arguments"] = new Variant("-l 12345");
			args["StartOnCall"] = foo;
			args["WaitForExitOnCall"] = bar;
			args["WaitForExitTimeout"] = new Variant("2000");
			args["NoCpuKill"] = new Variant("true");

			var w = new CrashWrangler(null, "name", args);

			w.Message("Action.Call", foo);
			w.Message("Action.Call", bar);

			w.IterationFinished();

			Assert.AreEqual(true, w.DetectedFault());
			var f = w.GetMonitorData();
			Assert.NotNull(f);
			Assert.AreEqual("ProcessFailedToExit", f.folderName);

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitTime()
		{
			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("nc");
			args["Arguments"] = new Variant("-l 12345");
			args["RestartOnEachTest"] = new Variant("true");

			var w = new CrashWrangler(null, "name", args);
			w.SessionStarting();
			w.IterationStarting(1, false);

			var before = DateTime.Now;
			w.IterationFinished();
			var after = DateTime.Now;

			var span = (after - before);

			Assert.AreEqual(false, w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();

			Assert.GreaterOrEqual(span.TotalSeconds, 0.0);
			Assert.LessOrEqual(span.TotalSeconds, 0.1);
		}

		[Test]
		public void TestExitEarlyFault()
		{
			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("echo");
			args["Arguments"] = new Variant("hello");
			args["FaultOnEarlyExit"] = new Variant("true");

			var w = new CrashWrangler(null, "name", args);
			w.IterationStarting(1, false);

			Thread.Sleep(1000);

			w.IterationFinished();

			Assert.AreEqual(true, w.DetectedFault());
			var f = w.GetMonitorData();
			Assert.NotNull(f);
			Assert.AreEqual("ProcessExitedEarly", f.folderName);

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault1()
		{
			var foo = new Variant("foo");
			var bar = new Variant("bar");

			// FaultOnEarlyExit doesn't fault when stop message is sent

			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("echo");
			args["Arguments"] = new Variant("hello");
			args["StartOnCall"] = foo;
			args["WaitForExitOnCall"] = bar;
			args["FaultOnEarlyExit"] = new Variant("true");

			var w = new CrashWrangler(null, "name", args);
			w.SessionStarting();
			w.IterationStarting(1, false);

			w.Message("Action.Call", foo);
			w.Message("Action.Call", bar);

			w.IterationFinished();

			Assert.AreEqual(false, w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault2()
		{
			var foo = new Variant("foo");

			// FaultOnEarlyExit faults when StartOnCall is used and stop message is not sent

			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("echo");
			args["Arguments"] = new Variant("hello");
			args["StartOnCall"] = foo;
			args["FaultOnEarlyExit"] = new Variant("true");

			var w = new CrashWrangler(null, "name", args);
			w.SessionStarting();
			w.IterationStarting(1, false);

			w.Message("Action.Call", foo);

			Thread.Sleep(1000);

			w.IterationFinished();

			Assert.AreEqual(true, w.DetectedFault());
			var f = w.GetMonitorData();
			Assert.NotNull(f);
			Assert.AreEqual("ProcessExitedEarly", f.folderName);


			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault3()
		{
			var foo = new Variant("foo");

			// FaultOnEarlyExit doesn't fault when StartOnCall is used

			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("nc");
			args["Arguments"] = new Variant("-l 12345");
			args["StartOnCall"] = foo;
			args["FaultOnEarlyExit"] = new Variant("true");

			var w = new CrashWrangler(null, "name", args);
			w.SessionStarting();
			w.IterationStarting(1, false);

			w.Message("Action.Call", foo);

			w.IterationFinished();

			Assert.AreEqual(false, w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault4()
		{
			// FaultOnEarlyExit doesn't fault when restart every iteration is true

			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("nc");
			args["Arguments"] = new Variant("-l 12345");
			args["RestartOnEachTest"] = new Variant("true");
			args["FaultOnEarlyExit"] = new Variant("true");

			var w = new CrashWrangler(null, "name", args);
			w.SessionStarting();
			w.IterationStarting(1, false);

			w.IterationFinished();

			Assert.AreEqual(false, w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestGetData()
		{
			var args = new Dictionary<string, Variant>();
			var path = Utilities.GetAppResourcePath("CrashingProgram");
			args["Executable"] = new Variant(path);

			Environment.SetEnvironmentVariable("PEACH", "qwertyuiopasdfghjklzxcvbnmqwertyuio");

			var w = new CrashWrangler(null, "name", args);
			w.SessionStarting();
			w.IterationStarting(0, false);
			Thread.Sleep(1000);
			w.IterationFinished();
			Assert.AreEqual(true, w.DetectedFault());
			var fault = w.GetMonitorData();
			Assert.NotNull(fault);
			Assert.AreEqual(1, fault.collectedData.Count);
			Assert.AreEqual("StackTrace.txt", fault.collectedData[0].Key);
			Assert.Greater(fault.collectedData[0].Value.Length, 0);
			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestCommandQuoting()
		{
			var args = new Dictionary<string, Variant>();
			args["Executable"] = new Variant("/Applications/QuickTime Player.app/Contents/MacOS/QuickTime Player");
			args["Arguments"] = new Variant("");
			args["RestartOnEachTest"] = new Variant("true");
			args["FaultOnEarlyExit"] = new Variant("true");

			var w = new CrashWrangler(null, "name", args);
			w.SessionStarting();
			Thread.Sleep(1000);

			Assert.AreEqual(false, w.DetectedFault());
			w.SessionFinished();
			w.StopMonitor();
		}
	}
}
