using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Monitors
{
	[TestFixture] [Category("Peach")]
	class MemoryMonitorTests
	{
		string _thisPid;

		[SetUp]
		public void SetUp()
		{
			using (var p = Process.GetCurrentProcess())
			{
				_thisPid = p.Id.ToString(CultureInfo.InvariantCulture);
			}
		}

		[Test]
		public void TestBadPid()
		{
			var runner = new MonitorRunner("Memory", new Dictionary<string,string>
			{
				{ "Pid", "2147483647" },
			});

			var faults = runner.Run();

			Assert.NotNull(faults);
			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Unable to locate process with Pid 2147483647.", faults[0].title);
		}

		[Test]
		public void TestBadProcName()
		{
			var runner = new MonitorRunner("Memory", new Dictionary<string, string>
			{
				{ "ProcessName", "some_invalid_process" },
			});

			var faults = runner.Run();

			Assert.NotNull(faults);
			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Unable to locate process \"some_invalid_process\".", faults[0].title);
		}

		[Test]
		public void TestNoParams()
		{
			var ex = Assert.Throws<PeachException>(() =>
				new MonitorRunner("Memory", new Dictionary<string, string>())
			);

			const string msg = "Could not start monitor \"Memory\".  Either pid or process name is required.";

			Assert.AreEqual(msg, ex.Message);
		}

		[Test]
		public void TestAllParams()
		{
			var ex = Assert.Throws<PeachException>(() => 
				new MonitorRunner("Memory", new Dictionary<string, string>
				{
					{ "Pid", "1" },
					{ "ProcessName", "name" },
				})
			);

			const string msg = "Could not start monitor \"Memory\".  Only specify pid or process name, not both.";

			Assert.AreEqual(msg, ex.Message);
		}

		[Test]
		public void TestNoFault()
		{
			// If no fault occurs, no data should be returned

			var runner = new MonitorRunner("Memory", new Dictionary<string, string>
			{
				{ "Pid", _thisPid },
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestFaultData()
		{
			// If a different fault occurs, monitor should always return data

			var runner = new MonitorRunner("Memory", new Dictionary<string, string>
			{
				{ "Pid", _thisPid },
			})
			{
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Memory monitor should not have detected fault");

					// Cause GetMonitorData() to be called
					return true;
				}
			};

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("MemoryMonitor", faults[0].detectionSource);
			Assert.AreEqual(FaultType.Data, faults[0].type);

			Console.WriteLine("{0}\n{1}\n", faults[0].title, faults[0].description);
		}

		[Test]
		public void TestFaultProcessId()
		{
			// Verify can generate faults using process id

			var runner = new MonitorRunner("Memory", new Dictionary<string, string>
			{
				{ "Pid", _thisPid },
				{ "MemoryLimit", "1" },
			});

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("MemoryMonitor", faults[0].detectionSource);
			Assert.AreEqual(FaultType.Fault, faults[0].type);

			Console.WriteLine("{0}\n{1}\n", faults[0].title, faults[0].description);
		}

		[Test]
		public void TestFaultProcessName()
		{
			// Verify can generate faults using process name

			var proc = Platform.GetOS() == Platform.OS.Windows ? "explorer.exe" : "sshd";

			var runner = new MonitorRunner("Memory", new Dictionary<string, string>
			{
				{ "ProcessName", proc },
				{ "MemoryLimit", "1" },
			});

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("MemoryMonitor", faults[0].detectionSource);
			Assert.AreEqual(FaultType.Fault, faults[0].type);

			Console.WriteLine("{0}\n{1}\n", faults[0].title, faults[0].description);
		}
	}
}
