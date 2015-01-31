using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Monitors
{
	[TestFixture]
	[Category("Peach")]
	class PingMonitorTests
	{
		[Test]
		public void TestSuccess()
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", "127.0.0.1" },
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestFailure()
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", "234.5.6.7" },
			});

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Ping", faults[0].DetectionSource);
			Assert.That(faults[0].Title, Is.StringContaining("The ICMP echo Reply was not received within the allotted time."));
			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual("Ping", faults[0].Fault.MajorHash);
			Assert.AreEqual("Failure", faults[0].Fault.MinorHash);
			Assert.NotNull(faults[0].Data);
			Assert.AreEqual(0, faults[0].Data.Count);
		}

		[Test]
		public void TestFaultSuccess()
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", "234.5.6.7" },
				{ "FaultOnSuccess", "true" },
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestFaultFailure()
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", "127.0.0.1" },
				{ "FaultOnSuccess", "true" },
			});

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Ping", faults[0].DetectionSource);
			Assert.That(faults[0].Title, Is.StringContaining("Reply from 127.0.0.1: bytes=32 time="));
			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual("Ping", faults[0].Fault.MajorHash);
			Assert.AreEqual("Success", faults[0].Fault.MinorHash);
			Assert.NotNull(faults[0].Data);
			Assert.AreEqual(0, faults[0].Data.Count);
		}

		[Test]
		public void TestSuccessData()
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", "127.0.0.1" },
			})
			{
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Monitor should not detect fault");

					// Trigger data collection
					return true;
				}
			};

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Ping", faults[0].DetectionSource);
			Assert.That(faults[0].Title, Is.StringContaining("Reply from 127.0.0.1: bytes=32 time="));
			Assert.Null(faults[0].Fault);
			Assert.NotNull(faults[0].Data);
			Assert.AreEqual(0, faults[0].Data.Count);
		}

		[Test]
		public void TestFaultSuccessData()
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", "234.5.6.7" },
				{ "FaultOnSuccess", "true" },
			})
			{
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Monitor should not detect fault");

					// Trigger data collection
					return true;
				}
			};

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Ping", faults[0].DetectionSource);
			Assert.That(faults[0].Title, Is.StringContaining("The ICMP echo Reply was not received within the allotted time."));
			Assert.Null(faults[0].Fault);
			Assert.NotNull(faults[0].Data);
			Assert.AreEqual(0, faults[0].Data.Count);
		}

		[Test]
		public void TestBadHost()
		{
			// RFC6761 says .invalid is guranteed to be an invalid TLD
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", "some.host.invalid" },
			});

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Ping", faults[0].DetectionSource);
			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual("Ping", faults[0].Fault.MajorHash);
			Assert.AreEqual("Exception", faults[0].Fault.MinorHash);
			Assert.NotNull(faults[0].Data);
			Assert.AreEqual(0, faults[0].Data.Count);
			StringAssert.IsMatch("(Could not resolve host)|(No such host is known)", faults[0].Title);
		}

		[Test]
		public void TestBadHostSuccess()
		{
			// RFC6761 says .invalid is guranteed to be an invalid TLD
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", "some.host.invalid" },
				{ "FaultOnSuccess", "true" },
			});

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Ping", faults[0].DetectionSource);
			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual("Ping", faults[0].Fault.MajorHash);
			Assert.AreEqual("Exception", faults[0].Fault.MinorHash);
			Assert.NotNull(faults[0].Data);
			Assert.AreEqual(0, faults[0].Data.Count);
			StringAssert.IsMatch("(Could not resolve host)|(No such host is known)", faults[0].Title);
		}

		[TestCase(1000)]
		[TestCase(2000)]
		[TestCase(3000)]
		public void TestTimeout(long timeout)
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", "234.5.6.7" },
				{ "Timeout", timeout.ToString(CultureInfo.InvariantCulture) },
			})
			{
				DetectedFault = m =>
				{
					var sw = new Stopwatch();

					sw.Start();
					Assert.True(m.DetectedFault(), "Monitor should not have detected fault");
					sw.Stop();

					var elapsed = sw.ElapsedMilliseconds;

					Assert.Greater(elapsed, timeout - 250);
					Assert.Less(elapsed, timeout + 250);

					return true;
				}
			};

			var faults = runner.Run();


			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Ping", faults[0].DetectionSource);
			Assert.That(faults[0].Title, Is.StringContaining("The ICMP echo Reply was not received within the allotted time."));
			Assert.Null(faults[0].Fault);
			Assert.NotNull(faults[0].Data);
			Assert.AreEqual(0, faults[0].Data.Count);
		}

		[Test]
		public void TestData()
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", "127.0.0.1" },
				{ "FaultOnSuccess", "true" },
				{ "Data", new string('a', 70) },
			});

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Ping", faults[0].DetectionSource);
			Assert.That(faults[0].Title, Is.StringContaining("Reply from 127.0.0.1: bytes=32 time="));
			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual("Ping", faults[0].Fault.MajorHash);
			Assert.AreEqual("Success", faults[0].Fault.MinorHash);
			Assert.NotNull(faults[0].Data);
			Assert.AreEqual(0, faults[0].Data.Count);
		}
	}
}
