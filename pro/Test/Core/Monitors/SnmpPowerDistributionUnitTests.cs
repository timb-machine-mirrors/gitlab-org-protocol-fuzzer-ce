using System;
using NUnit.Framework;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib;
using System.Collections.Generic;
using System.Net;
using Peach.Pro.Core.Agent.Monitors;
using System.Diagnostics;
using Peach.Core.Test;
using System.Collections.Concurrent;
using Peach.Core;

namespace Peach.Pro.Test.Core.Monitors
{
	class FakeSnmpAgent : ISnmpAgent
	{
		Dictionary<string, int> _state = new Dictionary<string, int>();

		public int Get(string oid)
		{
			return _state[oid];
		}

		public int Set(string oid, int code)
		{
			_state[oid] = code;
			return _state[oid];
		}
	}

	class FakeSnmpAgentReadOnly : ISnmpAgent
	{
		int _unrecognizedSwitchState = -1;

		public int Get(string oid)
		{
			return _unrecognizedSwitchState;
		}

		public int Set(string oid, int code)
		{
			throw new Exception("Expected failure; agent is read-only for testing");
		}
	}

	[TestFixture]
	class SnmpPowerDistributionUnitTests
	{
		static IReadOnlyList<string> OIDs = new List<string>() {
			".1.3.6.1.4.1.318.1.1.4.4.2.1.3.1",
			".1.3.6.1.4.1.318.1.1.4.4.2.1.3.2",
			".1.3.6.1.4.1.318.1.1.4.4.2.1.3.3",
		}.AsReadOnly();
		const int OnCode = 1;
		const int OffCode = 2;

		void AssertAgentState(SnmpPowerDistributionUnit spdu, OutletState targetState)
		{
			foreach (var oid in OIDs)
			{
				Assert.AreEqual(spdu.EncodeState(targetState), spdu.Agent.Get(oid));
			}
		}

		#region FakedSnmpAgent
		[Test]
		public void TurnOffSnmpPdu()
		{
			var agent = new FakeSnmpAgent();
			var spdu = new SnmpPowerDistributionUnit(agent, OIDs, OnCode, OffCode);

			spdu.Switch(OutletState.Off);
			AssertAgentState(spdu, OutletState.Off);
		}

		[Test]
		public void TurnOnSnmpPdu()
		{
			var agent = new FakeSnmpAgent();
			var spdu = new SnmpPowerDistributionUnit(agent, OIDs, OnCode, OffCode);

			spdu.Switch(OutletState.On);
			AssertAgentState(spdu, OutletState.On);
		}

		[Test]
		public void ResetSnmpPdu()
		{
			var agent = new FakeSnmpAgent();
			var spdu = new SnmpPowerDistributionUnit(agent, OIDs, OnCode, OffCode);

			spdu.Reset();
			AssertAgentState(spdu, OutletState.On);
		}

		[Test]
		public void ResetSnmpPduPauseDuration()
		{
			const int percentMarginOfError = 5;
			var timer = new Stopwatch();

			var shortDuration = 1;
			var longDuration = 500;

			Func<int, SnmpPowerDistributionUnit> newSpdu = (int duration) =>
				new SnmpPowerDistributionUnit(new FakeSnmpAgent(), OIDs, OnCode, OffCode, duration);
			var fastResetSpdu = newSpdu(shortDuration);
			var slowResetSpdu = newSpdu(longDuration);

			timer.Start(); fastResetSpdu.Reset(); timer.Stop();
			var fastTime = timer.ElapsedMilliseconds;

			timer.Restart(); slowResetSpdu.Reset();	timer.Stop();
			var slowTime = timer.ElapsedMilliseconds;

			Assert.That(
				fastTime / slowTime,
				Is.EqualTo(shortDuration / longDuration).Within(percentMarginOfError).Percent);
		}

		[Test]
		public void TestFailedStateChangeEvent()
		{
			var readOnlySpdu = new SnmpPowerDistributionUnit(new FakeSnmpAgentReadOnly(), OIDs, OnCode, OffCode);

			Assert.Throws<Exception>(() => readOnlySpdu.Switch(OutletState.Off));
		}
		#endregion


		#region RealSnmpAgent
		[Test]
		[Ignore("APC Power Distribution Unit must be set up for test")]
		/*
		 * Directions for setting up test device:
		 *
		 *   1. Power on APC Switched Racke Power Distribution Unit (AP7900).
		 *   2. Connect it to the LAN via ethernet cable.
		 *   3. Using its MAC address, assign it an unused IP address in the local ARP table:
		 *        $ sudo arp -s 10.0.1.101 00:c0:b7:8a:13:3b
		 *   4. Send it a 113-byte ping to make it aware of the assigned IP:
		 *        $ ping 10.0.1.101 -s 113
		 *   5. If the test fails at this point, install APC's powernet417.mib and try:
		 *        $ snmpwalk -v 1 -c public 10.0.1.101 enterprises.318
		 *   6. Confirm 'PowerNet-MIB::sPDUOutletCtl.1 = INTEGER:' is in the output.
		 */
		public void TestApcSwitchedRackPowerDistributionUnit()
		{
			var agent = new SnmpAgent("10.0.1.101",	161, "public", "private", 1000);
			var apc = new SnmpPowerDistributionUnit(agent, OIDs, OnCode, OffCode);

			Assert.DoesNotThrow(() => apc.SanityCheck(timeout: 3000));
		}
		#endregion
	}

	[TestFixture]
	class SnmpPowerMonitorTests
	{
		private ConcurrentQueue<string> _history = new ConcurrentQueue<string>();

		private MonitorRunner SnmpPowerRunner(Dictionary<string, string> args)
		{
			var defaultArgs = new Dictionary<string, string>
			{
				{ "Host", "10.0.1.101" },
				{ "OIDs", ".1.3.6.1.4.1.318.1.1.4.4.2.1.3.1,.1.3.6.1.4.1.318.1.1.4.4.2.1.3.2,.1.3.6.1.4.1.318.1.1.4.4.2.1.3.3" },
			};

			defaultArgs.ForEach(pair =>
				{
					if (!args.ContainsKey(pair.Key))
						args.Add(pair.Key, pair.Value);
				});

			var runner = new MonitorRunner("SnmpPower", args)
				{
					SessionStarting = m =>
						{
							m.InternalEvent += (s, e) =>
								{
									var sce = (StateChangeAttemptArgs)e;

									_history.Enqueue(sce.TargetState.ToString());
								};

							m.SessionStarting();
						},
					DetectedFault = m =>
						{
							Assert.False(m.DetectedFault(), "Monitor should not have detected fault");
							return true;
						}
				};
			return runner;
		}

		[Test]
		[Ignore("APC Power Distribution Unit must be set up for test")]
		/* See directions above for TestApcSwitchedRackPowerDistributionUnit */
		public void TestSnmpPowerBasic()
		{
			var runner = SnmpPowerRunner(new Dictionary<string, string>());
			var faults = runner.Run();
			Assert.AreEqual(0, faults.Length, "Faults length mismatch");

			var expected = new[]
				{
					"On",
					"Off",
					"On",
				};

			Assert.That(_history.ToArray(), Is.EqualTo(expected), "History mismatch");
		}

		[Test]
		public void ExceptionForMissingOIDs()
		{
			var runner = SnmpPowerRunner(new Dictionary<string, string>
				{
					{ "OIDs", "" },
				});

			Assert.Throws(Is.TypeOf<PeachException>().And.Message.Contains("Expected at least one OID"),
				() => runner.Run());
		}

		[Test]
		public void ExceptionForInvalidOID()
		{
			var runner = SnmpPowerRunner(new Dictionary<string, string>
				{
					{ "OIDs", "xyz" },
				});

			Assert.Throws(Is.TypeOf<PeachException>().And.Message.Contains("Invalid SNMP OID 'xyz'"),
				() => runner.Run());
		}

		[Test]
		public void UnavailableOID()
		{
			var runner = SnmpPowerRunner(new Dictionary<string, string>
				{
					{ "OIDs", ".1.3.6.1.4.1.318.1.1.4.4.2.1.3.999" },
				});

			Assert.Throws(Is.TypeOf<PeachException>().And.Message.Contains("Failed to set state of SNMP OID '.1.3.6.1.4.1.318.1.1.4.4.2.1.3.999' (agent '10.0.1.101', port: '161', community: 'private'"),
				() => runner.Run());
		}

		[Test]
		public void ExceptionForSameOnOffCodes()
		{
			var runner = SnmpPowerRunner(new Dictionary<string, string>
				{
					{ "OnCode", "5" },
					{ "OffCode", "5" },
				});

			Assert.Throws(Is.TypeOf<PeachException>().And.Message.Contains("OnCode and OffCode must be different, but they're both '5'"),
				() => runner.Run());
		}
	}
}

