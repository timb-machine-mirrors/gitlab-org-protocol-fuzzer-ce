using System;

namespace Peach.Core.Agent
{
	public abstract class StartStopRestartMonitor : Monitor2
	{
		public MonitorWhen When { get; set; }

		protected IStartStopRestart Control { get; set; }

		protected StartStopRestartMonitor(string name)
			: base(name)
		{
		}

		public override void SessionStarting()
		{
			Control.Start();

			if (When.HasFlag(MonitorWhen.OnStart))
				Control.Restart();
		}

		public abstract bool StopOnEnd();

		public override void SessionFinished()
		{
			if (When.HasFlag(MonitorWhen.OnEnd))
				Control.Restart();

			if (StopOnEnd())
				Control.Stop();
		}
		public override void IterationStarting(IterationStartingArgs args)
		{
			if (When.HasFlag(MonitorWhen.OnIterationStart) ||
			    (args.LastWasFault && When.HasFlag(MonitorWhen.OnIterationStartAfterFault)))
				Control.Restart();
		}

		public override void IterationFinished()
		{
			if (When.HasFlag(MonitorWhen.OnIterationEnd))
				Control.Restart();
		}

		public override bool DetectedFault()
		{
			if (When.HasFlag(MonitorWhen.DetectFault))
				Control.Restart();

			return false;
		}

		public override MonitorData GetMonitorData()
		{
			if (When.HasFlag(MonitorWhen.OnFault))
				Control.Restart();

			return null;
		}

		public abstract string RestartOnCall();

		public override void Message(string msg)
		{
			if (When.HasFlag(MonitorWhen.OnCall) && RestartOnCall() == msg)
				Control.Restart();
		}
	}
}

