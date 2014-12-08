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
			Assert.AreEqual("PingMonitor", faults[0].detectionSource);
			Assert.AreEqual(FaultType.Fault, faults[0].type);
			Assert.That(faults[0].description, Is.StringContaining("The ICMP echo Reply was not received within the allotted time."));
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
			Assert.AreEqual("PingMonitor", faults[0].detectionSource);
			Assert.AreEqual(FaultType.Fault, faults[0].type);
			Assert.That(faults[0].description, Is.StringContaining("RoundTrip time"));
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
			Assert.AreEqual("PingMonitor", faults[0].detectionSource);
			Assert.AreEqual(FaultType.Data, faults[0].type);
			Assert.That(faults[0].description, Is.StringContaining("RoundTrip time"));
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
			Assert.AreEqual("PingMonitor", faults[0].detectionSource);
			Assert.AreEqual(FaultType.Data, faults[0].type);
			Assert.That(faults[0].description, Is.StringContaining("The ICMP echo Reply was not received within the allotted time."));
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
			Assert.AreEqual("PingMonitor", faults[0].detectionSource);
			Assert.AreEqual(FaultType.Fault, faults[0].type);

			if (Platform.GetOS() == Platform.OS.Windows)
				Assert.That(faults[0].description, Is.StringContaining("No such host is known"));
			else
				Assert.That(faults[0].description, Is.StringContaining("Could not resolve host"));
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
			Assert.AreEqual("PingMonitor", faults[0].detectionSource);
			Assert.AreEqual(FaultType.Fault, faults[0].type);

			if (Platform.GetOS() == Platform.OS.Windows)
				Assert.That(faults[0].description, Is.StringContaining("No such host is known"));
			else
				Assert.That(faults[0].description, Is.StringContaining("Could not resolve host"));
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
			Assert.AreEqual("PingMonitor", faults[0].detectionSource);
			Assert.AreEqual(FaultType.Fault, faults[0].type);
			Assert.That(faults[0].description, Is.StringContaining("The ICMP echo Reply was not received within the allotted time."));
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
			Assert.AreEqual("PingMonitor", faults[0].detectionSource);
			Assert.AreEqual(FaultType.Fault, faults[0].type);
			Assert.That(faults[0].description, Is.StringContaining("Buffer size: 70"));
		}
	}
}
