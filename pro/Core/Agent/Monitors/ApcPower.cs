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
	// TODO: write tests for the monitor itself (see existing IpPower9258Monitor tests)
	// TODO: add logging
	// TODO: test more devices

	// TODO: docstrings for all parameters
	[Monitor("ApcPower")]
	[Description("Controls an APC Switched Power Distribution Unit")]
	[Parameter("Host", typeof(string), "IP address of the switched power distribution unit")]
	[Parameter("Port", typeof(int), "SNMP port", "161")]
	[Parameter("ReadCommunity", typeof(string), "", "public")]
	[Parameter("WriteCommunity", typeof(string), "", "private")]
	[Parameter("OIDs", typeof(string), "Comma-separated list of OIDs for the power outlets")]
	[Parameter("When", typeof(MonitorWhen), "When to toggle power on the specified port", "OnFault")]
	[Parameter("StartOnCall", typeof(string), "Run when signaled by the state machine", "")]
	[Parameter("PowerOffOnEnd", typeof(bool), "Power off when session completes (default is false)", "false")]
	[Parameter("PowerOnOffPause", typeof(int), "Pause in milliseconds between power off/power on (default is 1/2 second)", "500")]
	public class ApcPower : Monitor
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public string Host { get; set; }
		public int Port { get; set; }
		public string ReadCommunity { get; set; }
		public string WriteCommunity { get; set; }
		public string OIDs { get; set; }

		// TODO: verify all these are used correctly
		public MonitorWhen When { get; set; }
		public string StartOnCall { get; set; }
		public bool PowerOffOnEnd { get; set; }
		public int PowerOnOffPause { get; set; }

		private SnmpPowerDistributionUnit _control;

		public ApcPower(string name)
			: base(name)
		{
			Logger.Debug("Constructed!");
		}

		public override void StartMonitor(System.Collections.Generic.Dictionary<string, string> args)
		{
//			string val;
//			if (args.TryGetValue("ResetEveryIteration", out val) && !args.ContainsKey("When"))
//			{
//				Logger.Info("The parameter 'ResetEveryIteration' on the monitor 'IpPower9258' is deprecated.  Set the parameter 'When' to 'OnIterationStart' instead.");
//
//				args["When"] = "OnIterationStart";
//				args.Remove("ResetEveryIteration");
//			}

			if (string.IsNullOrEmpty(OIDs))
				throw new PeachException("OIDs is null or empty. Expected at least one OID.");

			base.StartMonitor(args);

			var oids = from oid in OIDs.Split(',')
			           select new SnmpOid(oid);

			_control = new SnmpPowerDistributionUnit(
				new SnmpAgent(Host, Port, ReadCommunity, WriteCommunity),
				oids);
		}

		public override void SessionStarting()
		{
			_control.Switch(SwitchState.On);
			if (_control.CurrentState() != SwitchState.On)
				throw new PeachException("Power monitor failed to turn on outlets");

			if (When.HasFlag(MonitorWhen.OnStart))
				_control.Reset();
		}

		public override void SessionFinished()
		{
			if (When.HasFlag(MonitorWhen.OnEnd))
				_control.Reset();

			if (PowerOffOnEnd)
				_control.Switch(SwitchState.Off);
		}
		public override void IterationStarting(IterationStartingArgs args)
		{
			if (When.HasFlag(MonitorWhen.OnIterationStart) ||
			    (args.LastWasFault && When.HasFlag(MonitorWhen.OnIterationStartAfterFault)))
				_control.Reset();
		}

		public override void IterationFinished()
		{
			if (When.HasFlag(MonitorWhen.OnIterationEnd))
				_control.Reset();
		}

		public override bool DetectedFault()
		{
			if (When.HasFlag(MonitorWhen.DetectFault))
				_control.Reset();

			return false;
		}

		public override MonitorData GetMonitorData()
		{
			if (When.HasFlag(MonitorWhen.OnFault))
				_control.Reset();

			return null;
		}

		public override void Message(string msg)
		{
			if (When.HasFlag(MonitorWhen.OnCall) && StartOnCall == msg)
				_control.Reset();
		}
	}


	internal enum SwitchState { On, Off, Unknown };

	internal class StateChangeAttemptArgs : EventArgs
	{
		public SwitchState TargetState { get; }
		public SwitchState CurrentState { get; }
		public bool Successful {
			get { return CurrentState == TargetState; }
		}

		public StateChangeAttemptArgs(SwitchState target, SwitchState current)
		{
			TargetState = target;
			CurrentState = current;
		}
	}

	internal abstract class PowerSwitch
	{
		public delegate void StateChangeAttemptHandler(object sender, StateChangeAttemptArgs e);
		public event StateChangeAttemptHandler StateChangeAttempt;

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

			OnAttemptedStateChange(new StateChangeAttemptArgs(targetState, CurrentState()));
		}

		/// <summary>
		/// Change to the specified switch state.
		///
		/// Exceptions will be caught and ignored by the calling Switch method. To detect failed
		/// switch attempts, handle the StateChangeAttempted event and inspect the event arg's
		/// Successful property.
		/// </summary>
		/// <param name="toState">Target switch state</param>
		protected abstract void DoSwitch(SwitchState toState);

		protected void OnAttemptedStateChange(StateChangeAttemptArgs e)
		{
			if (StateChangeAttempt != null)
				StateChangeAttempt(this, e);
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
				outlet.StateChangeAttempt += new StateChangeAttemptHandler(OutletSwitchAttempt);
			}
		}

		private void OutletSwitchAttempt(object sender, StateChangeAttemptArgs e)
		{
			OnAttemptedStateChange(e);
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

