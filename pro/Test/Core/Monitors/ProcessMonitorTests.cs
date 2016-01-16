using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	class ProcessMonitorTests
	{
		[Test]
		public void TestBadProcss()
		{
			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", "some_invalid_process" }
			});

			var ex = Assert.Throws<PeachException>(() => runner.Run());
			StringAssert.StartsWith("Could not start process 'some_invalid_process'.", ex.Message);
		}

		[Test]
		public void TestStartOnCall()
		{
			var sw = new Stopwatch();

			var runner = new MonitorRunner("Process", new Dictionary<string, string> {
				{ "Executable", Utilities.GetAppResourcePath("CrashableServer") },
				{ "Arguments", "127.0.0.1" },
				{ "StartOnCall", "foo" },
				{ "WaitForExitTimeout", "2000" },
				{ "NoCpuKill", "true" },
			}) {
				Message = m =>
				{
					m.Message("foo");
					Thread.Sleep(500);
				},
				IterationFinished = m =>
				{
					sw.Start();
					m.IterationFinished();
					sw.Stop();
				}
			};

			var faults = runner.Run();
			Assert.AreEqual(0, faults.Length);

			Assert.GreaterOrEqual(sw.Elapsed.TotalSeconds, 1.9);
			Assert.LessOrEqual(sw.Elapsed.TotalSeconds, 2.1);
		}

		[Test]
		public void TestCpuKill()
		{
			var sw = new Stopwatch();

			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("CrashableServer") },
				{ "Arguments", "127.0.0.1 0" },
				{ "StartOnCall", "foo" },
			})
			{
				Message = m =>
				{
					m.Message("foo");
					Thread.Sleep(500);
				},
				IterationFinished = m =>
				{
					sw.Start();
					m.IterationFinished();
					sw.Stop();
				}
			};

			var faults = runner.Run();
			Assert.AreEqual(0, faults.Length);

			Assert.GreaterOrEqual(sw.Elapsed.TotalSeconds, 0.0);
			Assert.LessOrEqual(sw.Elapsed.TotalSeconds, 0.5);
		}

		[Test]
		public void TestExitOnCallNoFault()
		{
			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("CrashingFileConsumer") },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "NoCpuKill", "true" },
			})
			{
				Message = m =>
				{
					m.Message("foo");
					m.Message("bar");
				},
			};

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestExitOnCallFault()
		{
			var exe = Utilities.GetAppResourcePath("CrashableServer");
			
			var runner = new MonitorRunner("Process", new Dictionary<string, string> {
				{ "Executable", exe },
				{ "Arguments", "127.0.0.1 0" },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "WaitForExitTimeout", "2000" },
				{ "NoCpuKill", "true" },
			}) {
				Message = m =>
				{
					m.Message("foo");
					m.Message("bar");
				},
			};

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Process '{0}' did not exit in 2000ms.".Fmt(exe), faults[0].Title);
			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual(Monitor2.Hash("Process{0}".Fmt(exe)), faults[0].Fault.MajorHash);
			Assert.AreEqual(Monitor2.Hash("FailedToExit"), faults[0].Fault.MinorHash);
		}

		[Test]
		public void TestExitTime()
		{
			var sw = new Stopwatch();

			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("CrashableServer") },
				{ "Arguments", "127.0.0.1 0" },
				{ "RestartOnEachTest", "true" },
			})
			{
				IterationFinished = m =>
				{
					sw.Start();
					m.IterationFinished();
					sw.Stop();
				}
			};

			var faults = runner.Run();
			Assert.AreEqual(0, faults.Length);

			Assert.GreaterOrEqual(sw.Elapsed.TotalSeconds, 0.0);
			Assert.LessOrEqual(sw.Elapsed.TotalSeconds, 0.1);
		}

		[Test]
		public void TestExitEarlyFault()
		{
			var exe = Utilities.GetAppResourcePath("CrashingFileConsumer");
			var runner = new MonitorRunner("Process", new Dictionary<string, string> {
				{ "Executable",  exe },
				{ "FaultOnEarlyExit", "true" },
			}) {
				Message = m => Thread.Sleep(1000),
			};

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Process '{0}' exited early.".Fmt(exe), faults[0].Title);
			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual(Monitor2.Hash("Process{0}".Fmt(exe)), faults[0].Fault.MajorHash);
			Assert.AreEqual(Monitor2.Hash("ExitedEarly"), faults[0].Fault.MinorHash);
		}

		[Test]
		public void TestExitEarlyFault1()
		{
			// FaultOnEarlyExit doesn't fault when stop message is sent

			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("CrashingFileConsumer") },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "FaultOnEarlyExit", "true" },
			})
			{
				Message = m =>
				{
					m.Message("foo");
					m.Message("bar");
				},
			};

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestExitEarlyFault2()
		{
			// FaultOnEarlyExit faults when WaitForExitOnCall is used and stop message is not sent

			var exe = Utilities.GetAppResourcePath("CrashingFileConsumer");

			var runner = new MonitorRunner("Process", new Dictionary<string, string> {
				{ "Executable", exe },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "FaultOnEarlyExit", "true" },
			}) {
				Message = m =>
				{
					m.Message("foo");
					Thread.Sleep(1000);
				},
			};

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Process '{0}' exited early.".Fmt(exe), faults[0].Title);
			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual(Monitor2.Hash("Process{0}".Fmt(exe)), faults[0].Fault.MajorHash);
			Assert.AreEqual(Monitor2.Hash("ExitedEarly"), faults[0].Fault.MinorHash);
		}

		[Test]
		public void TestExitEarlyFault3()
		{
			// FaultOnEarlyExit doesn't fault when StartOnCall is used

			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("CrashableServer") },
				{ "Arguments", "127.0.0.1 0" },
				{ "StartOnCall", "foo" },
				{ "FaultOnEarlyExit", "true" },
			})
			{
				Message = m => m.Message("foo"),
			};

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestExitEarlyFault4()
		{
			// FaultOnEarlyExit doesn't fault when restart every iteration is true

			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("CrashableServer") },
				{ "Arguments", "127.0.0.1 0" },
				{ "RestartOnEachTest", "true" },
				{ "FaultOnEarlyExit", "true" },
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestRestartAfterFault()
		{
			var startCount = 0;
			var iteration = 0;

			var runner = new MonitorRunner("Process", new Dictionary<string, string>
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

		[Test]
		[Repeat(30)]
		public void TestAddressSanitizer()
		{
			if (Platform.GetOS() == Platform.OS.Windows)
				Assert.Ignore("ASAN is not supported on Windows");

			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("UseAfterFree") },
				{ "AddressSanitizer", "true" },
			})
			{
				Message = m =>
				{
					Thread.Sleep(10);
				}
			};

			var faults = runner.Run(10);

			Assert.NotNull(faults);

			Assert.Greater(faults.Length, 0);

			foreach (var data in faults)
			{
				Assert.AreEqual("Process", data.DetectionSource);
				Assert.AreEqual("heap-use-after-free", data.Fault.Risk);
				Assert.IsFalse(data.Fault.MustStop);
				StringAssert.Contains("Shadow bytes", data.Fault.Description);

				if (Platform.GetOS() == Platform.OS.OSX)
				{
					const string pattern = "heap-use-after-free on address 0x61400000fe44 at pc 0x000100001b8f";
					StringAssert.StartsWith(pattern, data.Title);
					StringAssert.Contains(pattern, data.Fault.Description);
					Assert.AreEqual("02133A7E", data.Fault.MajorHash);
					Assert.AreEqual("9DD19897", data.Fault.MinorHash);
				}
				else if (Platform.GetOS() == Platform.OS.Linux)
				{
					const string pattern = "heap-use-after-free on address ";
					if (Platform.GetArch() == Platform.Architecture.x64)
					{
						StringAssert.StartsWith(pattern, data.Title);
						StringAssert.Contains(pattern, data.Fault.Description);
						CollectionAssert.Contains(new[] { "C755DA91", "3BFFE0CC" }, data.Fault.MajorHash);
						Assert.AreEqual("9DD19897", data.Fault.MinorHash);
					}
					else
					{
						StringAssert.StartsWith(pattern, data.Title);
						StringAssert.Contains(pattern, data.Fault.Description);
						CollectionAssert.Contains(new[] { "DF8C57E3", "7938DA7F" }, data.Fault.MajorHash);
						CollectionAssert.Contains(new[] { "6B08385F", "552648B1" }, data.Fault.MinorHash);
					}
				}
			}
		}
	}
}