using System;
using NUnit.Framework;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib;
using System.Collections.Generic;
using System.Net;
using Peach.Pro.Core.Agent.Monitors;

namespace Peach.Pro.Test.Core.Monitors
{
	class MockSnmpAgent : ISnmpAgent
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

	class MockSnmpAgentReadOnly : ISnmpAgent
	{
		public int Get(SnmpOid oid)
		{
			return -1;  // TODO: change API to ensure this decodes to PowerState.Unknown
		}

		public int Set(SnmpOid oid, int code)
		{
			throw new Exception("Expected failure; agent is read-only for testing");
		}
	}

	[TestFixture]
	class SnmpPowerDistributionUnitTests
	{
		List<SnmpOid> oids = new List<SnmpOid>() {
			new SnmpOid(".1.3.6.1.4.1.318.1.1.4.4.2.1.3.6"),
			new SnmpOid(".1.3.6.1.4.1.318.1.1.4.4.2.1.3.7"),
			new SnmpOid(".1.3.6.1.4.1.318.1.1.4.4.2.1.3.8"),
		};

		#region MockedSnmpAgent
		[Test]
		public void TurnOffSnmpPdu()
		{
			var spdu = new SnmpPowerDistributionUnit(new MockSnmpAgent(), oids);
			spdu.Switch(SwitchState.Off);
			Assert.AreEqual(SwitchState.Off, spdu.CurrentState());
		}

		[Test]
		public void TurnOnSnmpPdu()
		{
			var spdu = new SnmpPowerDistributionUnit(new MockSnmpAgent(), oids);
			spdu.Switch(SwitchState.On);
			Assert.AreEqual(SwitchState.On, spdu.CurrentState());
		}

		[Test]
		public void ResetSnmpPdu()
		{
			var spdu = new SnmpPowerDistributionUnit(new MockSnmpAgent(), oids);
			spdu.Reset();
			Assert.AreEqual(SwitchState.On, spdu.CurrentState());
		}

		[Test]
		public void TestFailedStateChangeEvent()
		{
			var readOnlySpdu = new SnmpPowerDistributionUnit(new MockSnmpAgentReadOnly(), oids);

			var enteredHandler = false;
			readOnlySpdu.FailedStateChange += (object sender, FailedStateChangeArgs e) => {
				enteredHandler = true;

				Assert.AreEqual(SwitchState.Off, e.TargetState);
				Assert.AreEqual(SwitchState.Unknown, e.CurrentState);
			};

			readOnlySpdu.Switch(SwitchState.Off);

			Assert.True(enteredHandler);
		}
		#endregion


		#region RealSnmpAgent
		[Test]
		[Ignore("Assumes APC Power Distribution Unit is ready and waiting")]
		// TODO: dynamically start a real test SnmpAgent
		public void TestRealSnmpAgent()
		{
			var host = "10.0.1.101";
			var port = 161;
			var spdu = new SnmpPowerDistributionUnit(
				new SnmpAgent(
					host,
					port,
					"public",
					"private"),
				oids);

			spdu.FailedStateChange += (object sender, FailedStateChangeArgs e) => {
				Assert.Fail("Failed to change to state {0}. Currently in state {1}.",
					e.TargetState,
					e.CurrentState);
			};

			spdu.Switch(SwitchState.On);
			Assert.AreEqual(SwitchState.On, spdu.CurrentState());

			spdu.Switch(SwitchState.Off);
			Assert.AreEqual(SwitchState.Off, spdu.CurrentState());

			spdu.Reset();
			Assert.AreEqual(SwitchState.On, spdu.CurrentState());
		}
		#endregion
	}
}

