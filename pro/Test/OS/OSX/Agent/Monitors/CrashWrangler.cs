using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Test;
using Peach.Pro.OS.OSX.Agent.Monitors;

namespace Peach.Pro.Test.OS.OSX.Agent.Monitors
{
	[TestFixture] [Category("Peach")]
	public class CrashWranglerTest
	{
		[Test]
		public void BadHandler()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", "foo" },
				{ "ExecHandler", "foo" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			const string expected = "CrashWrangler could not start handler \"foo\" - No such file or directory.";
			Assert.Throws<PeachException>(w.SessionStarting, expected);
		}

		[Test]
		public void BadCommand()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", "foo" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			const string expected = "CrashWrangler handler could not find command \"foo\".";
			Assert.Throws<PeachException>(w.SessionStarting, expected);
		}
		
		[Test]
		public void TestNoFault()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", "echo" },
				{ "Arguments", "hello" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(null);
			Thread.Sleep(1000);
			w.IterationFinished();
			Assert.AreEqual(false, w.DetectedFault());
			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestStopping()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", "nc" },
				{ "Arguments", "-l 12345" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(null);
			Thread.Sleep(1000);
			w.IterationFinished();
			Assert.AreEqual(false, w.DetectedFault());
			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestStartOnCall()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", "nc" },
				{ "Arguments", "-l 12345" },
				{ "StartOnCall", "foo" },
				{ "WaitForExitTimeout", "2000" },
				{ "NoCpuKill", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);

			w.Message("foo");
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
			var args = new Dictionary<string, string>
			{
				{ "Executable", "nc" },
				{ "Arguments", "-l 12345" },
				{ "StartOnCall", "foo" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);

			w.Message("foo");
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
			var args = new Dictionary<string, string>
			{
				{ "Executable", "echo" },
				{ "Arguments", "hello" },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "NoCpuKill", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);

			w.Message("foo");
			w.Message("bar");

			w.IterationFinished();

			Assert.AreEqual(false, w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitOnCallFault()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", "nc" },
				{ "Arguments", "-l 12345" },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "WaitForExitTimeout", "2000" },
				{ "NoCpuKill", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);

			w.Message("foo");
			w.Message("bar");

			w.IterationFinished();

			Assert.AreEqual(true, w.DetectedFault());
			var f = w.GetMonitorData();
			Assert.NotNull(f);
			Assert.NotNull(f.Fault);
			Assert.AreEqual(Monitor2.Hash("CrashWranglernc"), f.Fault.MajorHash);
			Assert.AreEqual(Monitor2.Hash("FailedToExit"), f.Fault.MinorHash);

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitTime()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", "nc" },
				{ "Arguments", "-l 12345" },
				{ "RestartOnEachTest", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(null);

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
			var args = new Dictionary<string, string>
			{
				{ "Executable", "echo" },
				{ "Arguments", "hello" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.IterationStarting(null);

			Thread.Sleep(1000);

			w.IterationFinished();

			Assert.AreEqual(true, w.DetectedFault());
			var f = w.GetMonitorData();
			Assert.NotNull(f);
			Assert.NotNull(f.Fault);
			Assert.AreEqual(Monitor2.Hash("CrashWranglerecho"), f.Fault.MajorHash);
			Assert.AreEqual(Monitor2.Hash("ExitedEarly"), f.Fault.MinorHash);

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault1()
		{
			// FaultOnEarlyExit doesn't fault when stop message is sent

			var args = new Dictionary<string, string>
			{
				{ "Executable", "echo" },
				{ "Arguments", "hello" },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(null);

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
				{ "Executable", "echo" },
				{ "Arguments", "hello" },
				{ "StartOnCall", "foo" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(null);

			w.Message("foo");

			Thread.Sleep(1000);

			w.IterationFinished();

			Assert.AreEqual(true, w.DetectedFault());
			var f = w.GetMonitorData();
			Assert.NotNull(f);
			Assert.NotNull(f.Fault);
			Assert.AreEqual(Monitor2.Hash("CrashWranglerecho"), f.Fault.MajorHash);
			Assert.AreEqual(Monitor2.Hash("ExitedEarly"), f.Fault.MinorHash);

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault3()
		{
			// FaultOnEarlyExit doesn't fault when StartOnCall is used

			var args = new Dictionary<string, string>
			{
				{ "Executable", "nc" },
				{ "Arguments", "-l 12345" },
				{ "StartOnCall", "foo" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(null);

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
				{ "Executable", "nc" },
				{ "Arguments", "-l 12345" },
				{ "RestartOnEachTest", "true" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(null);

			w.IterationFinished();

			Assert.AreEqual(false, w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestGetData()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("CrashingProgram") },
			};

			Environment.SetEnvironmentVariable("PEACH", "qwertyuiopasdfghjklzxcvbnmqwertyuio");

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(null);
			Thread.Sleep(1000);
			w.IterationFinished();
			Assert.AreEqual(true, w.DetectedFault());
			var fault = w.GetMonitorData();
			Assert.NotNull(fault);
			Assert.NotNull(fault.Fault);
			Assert.False(string.IsNullOrEmpty(fault.Fault.Description));
			Assert.AreEqual(0, fault.Data.Count);
			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestCommandQuoting()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", "/Applications/QuickTime Player.app/Contents/MacOS/QuickTime Player" },
				{ "Arguments", "" },
				{ "RestartOnEachTest", "true" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			Thread.Sleep(1000);

			Assert.AreEqual(false, w.DetectedFault());
			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestRestartAfterFault()
		{
			var startCount = 0;
			var iteration = 0;

			var runner = new MonitorRunner("CrashWrangler", new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("CrashableServer") },
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

			var faults = runner.Run(5);

			Assert.AreEqual(0, faults.Length);
			Assert.AreEqual(2, startCount);
		}
	}
}
