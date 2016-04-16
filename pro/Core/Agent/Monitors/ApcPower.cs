using System;
using System.Linq;
using Peach.Core;
using Peach.Core.Agent;
using NLog;
using System.Collections.Generic;
using Monitor = Peach.Core.Agent.Monitor2;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using System.Threading;
using System.Diagnostics;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib;
using System.Net;

namespace Peach.Pro.Core.Agent.Monitors
{
	// TODO: implement Monitor using SnmpPowerDistributionUnit
	// TODO: test more devices
	// TODO: integrate #SNMP lib into 3rdParty

	[Monitor("ApcPower")]
	[Description("Controls an APC Switched Power Distribution Unit")]
	public class ApcPower : Monitor
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public ApcPower(string name)
			: base(name)
		{
			Logger.Debug("Constructed!");
		}
	}


	internal enum SwitchState { On, Off, Unknown };

	internal class FailedStateChangeArgs : EventArgs
	{
		public SwitchState TargetState { get; }
		public SwitchState CurrentState { get; }

		public FailedStateChangeArgs(SwitchState target, SwitchState current)
		{
			TargetState = target;
			CurrentState = current;
		}
	}

	internal delegate void FailedStateChangeHandler(object sender, FailedStateChangeArgs e);

	internal abstract class PowerSwitch
	{
		public abstract SwitchState CurrentState();

		public void Switch(SwitchState targetState)
		{
			if (targetState == SwitchState.Unknown)
			{
				throw new ArgumentException("Switching to SwitchState.Unknown is not permitted!");
			}

			try
			{
				DoSwitch(targetState);
			}
			catch
			{
			}

			var postState = CurrentState();
			if (postState != targetState)
			{
				OnFailedStateChange(new FailedStateChangeArgs(targetState, postState));
			}
		}

		protected abstract void DoSwitch(SwitchState toState);

		public event FailedStateChangeHandler FailedStateChange;
		protected virtual void OnFailedStateChange(FailedStateChangeArgs e)
		{
			if (FailedStateChange != null)
				FailedStateChange(this, e);
		}

		public void Reset()
		{
			if (CurrentState() != SwitchState.Off)
			{
				Switch(SwitchState.Off);
			}
			Thread.Sleep(ResetPauseDuration());
			Switch(SwitchState.On);
		}

		protected virtual int ResetPauseDuration()
		{
			return 100;
		}
	}

	internal class SnmpOid
	{
		public string Value { get; }

		public SnmpOid(string oid)
		{
			Value = oid;
		}
	}

	internal interface ISnmpAgent
	{
		int Get(SnmpOid oid);

		int Set(SnmpOid oid, int code);
	}

	internal class SnmpAgent : ISnmpAgent
	{
		string Host { get; }
		int Port { get; }
		string ReadCommunity { get; }
		string WriteCommunity { get; }
		int RequestTimeout { get; set; }

		public SnmpAgent(string host, int port, string readCommunity, string writeCommunity)
		{
			Host = host;
			Port = port;
			ReadCommunity = readCommunity;
			WriteCommunity = writeCommunity;
		}

		public int Get(SnmpOid oid)
		{
			var response = Messenger.Get(
				VersionCode.V1,
				new IPEndPoint(IPAddress.Parse(Host), Port),
				new OctetString(ReadCommunity),
				new List<Variable> {
					new Variable(new ObjectIdentifier(oid.Value))
				},
				RequestTimeout);
			// TODO: handle unexpected response
			return ((Integer32)response[0].Data).ToInt32();
		}

		public int Set(SnmpOid oid, int code)
		{
			var response = Messenger.Set(
				VersionCode.V1,
				new IPEndPoint(IPAddress.Parse(Host), Port),
				new OctetString(WriteCommunity),
				new List<Variable> {
					new Variable(new ObjectIdentifier(oid.Value), new Integer32(code))
				},
				RequestTimeout);
			// TODO: handle unexpected response
			return ((Integer32)response[0].Data).ToInt32();
		}
	}

	internal class SnmpPowerSwitch : PowerSwitch
	{
		const int OnCode = 1;
		const int OffCode = 2;

		ISnmpAgent Agent { get; }
		SnmpOid Oid { get; }

		public SnmpPowerSwitch(ISnmpAgent agent, SnmpOid oid)
		{
			Agent = agent;
			Oid = oid;
		}

		private SwitchState DecodeState(int stateCode)
		{
			switch (stateCode) {
				case OnCode:
					return SwitchState.On;
				case OffCode:
					return SwitchState.Off;
				default:
					return SwitchState.Unknown;
			}
		}

		public override SwitchState CurrentState()
		{
			try
			{
				return DecodeState(Agent.Get(Oid));
			}
			catch
			{
				return SwitchState.Unknown;
			}
		}

		protected override void DoSwitch(SwitchState toState)
		{
			if (toState == SwitchState.Unknown)
				return;
			Agent.Set(Oid, toState == SwitchState.On ? OnCode : OffCode);
		}
	}

	internal class SnmpPowerDistributionUnit : PowerSwitch
	{
		IEnumerable<SnmpPowerSwitch> PowerSwitches { get; }

		public SnmpPowerDistributionUnit(ISnmpAgent agent, IEnumerable<SnmpOid> oids)
		{
			PowerSwitches = from oid in oids
			                select new SnmpPowerSwitch(agent, oid);
			AttachEventHandlers();
		}

		private void AttachEventHandlers()
		{
			foreach (var outlet in PowerSwitches)
			{
				outlet.FailedStateChange += new FailedStateChangeHandler(FailedOutletSwitchAttempt);
			}
		}

		private void FailedOutletSwitchAttempt(object sender, FailedStateChangeArgs e)
		{
			OnFailedStateChange(e);
		}

		public override SwitchState CurrentState()
		{
			if (PowerSwitches.Count() == 0)
			{
				return SwitchState.Off;
			}

			var states = from sw in PowerSwitches
			             select sw.CurrentState();
			var allSame = states.All(s => s == states.First());
			return allSame ? states.First() : SwitchState.Unknown;
		}

		protected override void DoSwitch(SwitchState toState)
		{
			foreach (var outlet in PowerSwitches)
			{
				outlet.Switch(toState);
			}
		}
	}
}

