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
		Dictionary<SnmpOid, int> _state = new Dictionary<SnmpOid, int>();

		public int Get(SnmpOid oid)
		{
			return _state[oid];
		}

		public int Set(SnmpOid oid, int code)
		{
			_state[oid] = code;
			return _state[oid];
		}
	}

	class FakeSnmpAgentReadOnly : ISnmpAgent
	{
		int _unrecognizedSwitchState = -1;

		public int Get(SnmpOid oid)
		{
			return _unrecognizedSwitchState;
		}

		public int Set(SnmpOid oid, int code)
		{
			throw new Exception("Expected failure; agent is read-only for testing");
		}
	}

	[TestFixture]
	class SnmpPowerDistributionUnitTests
	{
		static IReadOnlyList<SnmpOid> OIDs = new List<SnmpOid>() {
			new SnmpOid(".1.3.6.1.4.1.318.1.1.4.4.2.1.3.6"),
			new SnmpOid(".1.3.6.1.4.1.318.1.1.4.4.2.1.3.7"),
			new SnmpOid(".1.3.6.1.4.1.318.1.1.4.4.2.1.3.8"),
		}.AsReadOnly();

		#region FakedSnmpAgent
		[Test]
		public void TurnOffSnmpPdu()
		{
			var spdu = new SnmpPowerDistributionUnit(new FakeSnmpAgent(), OIDs);
			spdu.Switch(SwitchState.Off);
			Assert.AreEqual(SwitchState.Off, spdu.CurrentState());
		}

		[Test]
		public void TurnOnSnmpPdu()
		{
			var spdu = new SnmpPowerDistributionUnit(new FakeSnmpAgent(), OIDs);
			spdu.Switch(SwitchState.On);
			Assert.AreEqual(SwitchState.On, spdu.CurrentState());
		}

		[Test]
		public void ResetSnmpPdu()
		{
			var spdu = new SnmpPowerDistributionUnit(new FakeSnmpAgent(), OIDs);
			spdu.Reset();
			Assert.AreEqual(SwitchState.On, spdu.CurrentState());
		}

		[Test]
		public void ResetSnmpPduPauseDuration()
		{
			const int percentMarginOfError = 5;

			var shortDuration = 1;
			var longDuration = 500;

			var fastResetSpdu = new SnmpPowerDistributionUnit(new FakeSnmpAgent(), OIDs, shortDuration);
			var slowResetSpdu = new SnmpPowerDistributionUnit(new FakeSnmpAgent(), OIDs, longDuration);

			var timer = new Stopwatch();

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
			var readOnlySpdu = new SnmpPowerDistributionUnit(new FakeSnmpAgentReadOnly(), OIDs);

			var enteredHandler = false;
			readOnlySpdu.StateChangeAttempt += (object sender, StateChangeAttemptArgs e) => {
				enteredHandler = true;

				Assert.AreEqual(SwitchState.Off, e.TargetState);
				Assert.AreEqual(SwitchState.Unknown, e.CurrentState);
				Assert.IsFalse(e.Successful);
			};

			readOnlySpdu.Switch(SwitchState.Off);

			Assert.True(enteredHandler);
		}
		#endregion


		#region RealSnmpAgent
		SnmpPowerDistributionUnit APC_SWITCHED_RACK_POWER_DISTRIBUTION_UNIT_AP7900 =
			new SnmpPowerDistributionUnit(
				new SnmpAgent(
					"10.0.1.101",
					161,
					"public",
					"private"),
				OIDs);

		private void RunTestAgainstActualDevice(SnmpPowerDistributionUnit spdu)
		{
			spdu.StateChangeAttempt += (object sender, StateChangeAttemptArgs e) => {
				if (e.Successful)
				{
					Assert.AreEqual(e.TargetState, e.CurrentState);
				}
				else
				{
					Assert.Fail("Failed to change to state {0}. Currently in state {1}.",
						e.TargetState,
						e.CurrentState);
				}
			};

			spdu.Switch(SwitchState.On);
			Assert.AreEqual(SwitchState.On, spdu.CurrentState());

			spdu.Switch(SwitchState.Off);
			Assert.AreEqual(SwitchState.Off, spdu.CurrentState());

			spdu.Reset();
			Assert.AreEqual(SwitchState.On, spdu.CurrentState());
		}

		[Test]
		[Ignore("Assumes APC Power Distribution Unit is ready and waiting")]
		// TODO: dynamically start a real test SnmpAgent
		public void TestApcSwitchedRackPowerDistributionUnit()
		{
			RunTestAgainstActualDevice(APC_SWITCHED_RACK_POWER_DISTRIBUTION_UNIT_AP7900);
		}
		#endregion
	}

	[TestFixture]
	class ApcPowerMonitorTests
	{
		private ConcurrentQueue<string> _history;

		private MonitorRunner SnmpPowerRunner(Dictionary<string, string> args)
		{
			args.Add("Host", "10.0.1.101");
			args.Add("Port", "161");
			args.Add("OIDs", ".1.3.6.1.4.1.318.1.1.4.4.2.1.3.6,.1.3.6.1.4.1.318.1.1.4.4.2.1.3.7,.1.3.6.1.4.1.318.1.1.4.4.2.1.3.8");
			// TODO: check for other important args

			var runner = new MonitorRunner("ApcPower", args)
				{
					SessionStarting = m =>
						{
							m.InternalEvent += (s, e) =>
								{
									var sce = (StateChangeAttemptArgs)e;
									var monitor = (ApcPowerMonitor)s;

									_history.Enqueue("Port {0}: {1}".Fmt(monitor.Port, sce.CurrentState));
								};

							m.SessionStarting();
						},
					DetectedFault = m =>
						{
							Assert.False(m.DetectedFault(), "Monitor should not have detected fault");
							// Force data collection
							return true;
						}
				};
			return runner;
		}

		[Test]
		public void TestSnmpPowerBasic()
		{
			var runner = SnmpPowerRunner(new Dictionary<string, string>());
			var faults = runner.Run();
			Assert.AreEqual(0, faults.Length, "Faults length mismatch");

			var expected = new[]
				{
					"Port 161: On",
					"Port 161: Off",
					"Port 161: On",
				};

			Assert.That(_history.ToArray(), Is.EqualTo(expected), "History mismatch");
		}
	}
}

