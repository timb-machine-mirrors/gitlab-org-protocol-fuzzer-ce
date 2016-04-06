using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Test;
using Peach.Pro.Core.Agent.Monitors;

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

		[Test]
		public void TestAsanRegex()
		{
			const string example = @"==13983==ERROR: AddressSanitizer: SEGV on unknown address 0x00002f10b7d6 (pc 0x0000004ee255 bp 0x7ffc0abb72d0 sp 0x7ffc0abb72d0 T0)
    #0 0x4ee254 in decode_tag_number /home/peach/bacnet-stack-0.8.3/lib/../src/bacdcode.c:313:9
    #1 0x4ee4e2 in decode_tag_number_and_value /home/peach/bacnet-stack-0.8.3/lib/../src/bacdcode.c:379:11
    #2 0x519191 in awf_decode_service_request /home/peach/bacnet-stack-0.8.3/lib/../src/awf.c:130:17
    #3 0x50fd31 in handler_atomic_write_file /home/peach/bacnet-stack-0.8.3/lib/../demo/handler/h_awf.c:114:11
    #4 0x4ed71f in apdu_handler /home/peach/bacnet-stack-0.8.3/lib/../src/apdu.c:477:21
    #5 0x50a2e3 in npdu_handler /home/peach/bacnet-stack-0.8.3/lib/../demo/handler/h_npdu.c:88:17
    #6 0x4c4662 in main /home/peach/bacnet-stack-0.8.3/demo/server/main.c:188:13
    #7 0x7f1926df3ec4 in __libc_start_main /build/eglibc-3GlaMS/eglibc-2.19/csu/libc-start.c:287
    #8 0x4c442c in _start (/home/peach/bacnet-stack-0.8.3/bin/bacserv+0x4c442c)

AddressSanitizer can not provide additional info.
SUMMARY: AddressSanitizer: SEGV /home/peach/bacnet-stack-0.8.3/lib/../src/bacdcode.c:313 decode_tag_number
==13983==ABORTING";


			var title = RunCommand.AsanTitle.Match(example);
			var bucket = RunCommand.AsanBucket.Match(example);
			var desc = RunCommand.AsanMessage.Match(example);

			var data = new MonitorData
			{
				Data = new Dictionary<string, Stream>(),
				Title = title.Groups[1].Value,
				Fault = new MonitorData.Info
				{
					Description = example.Substring(desc.Index, desc.Length),
					MajorHash = Monitor2.Hash(bucket.Groups[3].Value),
					MinorHash = Monitor2.Hash(bucket.Groups[2].Value),
					Risk = bucket.Groups[1].Value,
				}
			};

			Assert.AreEqual("SEGV on unknown address 0x00002f10b7d6 (pc 0x0000004ee255 bp 0x7ffc0abb72d0 sp 0x7ffc0abb72d0 T0)", data.Title);
			Assert.AreEqual("SEGV", data.Fault.Risk);
			Assert.AreEqual(example, data.Fault.Description);
			Assert.AreEqual("EB7CE44C", data.Fault.MajorHash);
			Assert.AreEqual("2E409A3D", data.Fault.MinorHash);

		}
	}
}