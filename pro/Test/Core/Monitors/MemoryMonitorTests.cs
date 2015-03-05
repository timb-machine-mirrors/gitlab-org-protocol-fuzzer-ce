using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Monitors
{
	[TestFixture] [Category("Peach")]
	class MemoryMonitorTests
	{
		string _thisPid;
		string _thisProcessName;

		[SetUp]
		public void SetUp()
		{
			using (var p = Process.GetCurrentProcess())
			{
				_thisPid = p.Id.ToString(CultureInfo.InvariantCulture);
				_thisProcessName = p.ProcessName;
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
			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual("Unable to locate process with Pid 2147483647.", faults[0].Title);
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
			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual("Unable to locate process \"some_invalid_process\".", faults[0].Title);
		}

		[Test]
		public void TestNoParams()
		{
			var runner = new MonitorRunner("Memory", new Dictionary<string, string>());

			var ex = Assert.Throws<PeachException>(() => runner.Run());

			const string msg = "Could not start monitor \"Memory\".  Either pid or process name is required.";

			Assert.AreEqual(msg, ex.Message);
		}

		[Test]
		public void TestAllParams()
		{
			var runner = new MonitorRunner("Memory", new Dictionary<string, string>
			{
				{ "Pid", "1" },
				{ "ProcessName", "name" },
			});

			var ex = Assert.Throws<PeachException>(() => runner.Run());

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

			Verify(faults, false);
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

			Verify(faults, true);
		}

		[Test]
		public void TestFaultProcessName()
		{
			// Verify can generate faults using process name

			var runner = new MonitorRunner("Memory", new Dictionary<string, string>
			{
				{ "ProcessName", _thisProcessName },
				{ "MemoryLimit", "1" },
			});

			var faults = runner.Run();

			Verify(faults, true);
		}

		static void Verify(MonitorData[] faults, bool isFault)
		{
			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Memory", faults[0].DetectionSource);

			if (!isFault)
			{
				Assert.Null(faults[0].Fault);

				StringAssert.IsMatch("\\w+ \\(pid: \\d+\\) memory usage", faults[0].Title);
				Assert.NotNull(faults[0].Data);
				Assert.True(faults[0].Data.ContainsKey("Usage.txt"));

				var usage = faults[0].Data["Usage.txt"].AsString();

				StringAssert.Contains("PrivateMemorySize", usage);
				StringAssert.Contains("WorkingSet", usage);
				StringAssert.Contains("PeakWorkingSet", usage);
				StringAssert.Contains("VirtualMemorySize", usage);
				StringAssert.Contains("PeakVirtualMemorySize", usage);
			}
			else
			{
				Assert.NotNull(faults[0].Fault);
				Assert.AreEqual("Memory", faults[0].DetectionSource);
				StringAssert.IsMatch("\\w+ \\(pid: \\d+\\) exceeded memory limit", faults[0].Title);
				Assert.NotNull(faults[0].Data);
				Assert.AreEqual(0, faults[0].Data.Count);
				Assert.Null(faults[0].Fault.MajorHash);
				Assert.Null(faults[0].Fault.MinorHash);
				Assert.Null(faults[0].Fault.Risk);

				var usage = faults[0].Fault.Description;

				StringAssert.Contains("PrivateMemorySize", usage);
				StringAssert.Contains("WorkingSet", usage);
				StringAssert.Contains("PeakWorkingSet", usage);
				StringAssert.Contains("VirtualMemorySize", usage);
				StringAssert.Contains("PeakVirtualMemorySize", usage);
			}
		}
	}
}
