using System.Collections.Generic;
using System.IO;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor2;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Agent.Monitors
{
	[Monitor("Null")]
	[Description("A monitor that does reports no faults and optionally logs events to a file.")]
	[Parameter("LogFile", typeof(string), "Log monitor events to the specified file.", "")]
	public class NullMonitor : Monitor
	{
		public string LogFile { get; set; }

		void Log(string msg, params object[] args)
		{
			if (!string.IsNullOrEmpty(LogFile))
			{
				using (var writer = new StreamWriter(LogFile, true))
				{
					writer.WriteLine(msg, args);
				}
			}
		}

		public NullMonitor(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			base.StartMonitor(args);

			Log("{0}.StartMonitor", Name);
		}

		public override void StopMonitor()
		{
			Log("{0}.StopMonitor", Name);
		}

		public override void SessionStarting()
		{
			Log("{0}.SessionStarting", Name);
		}

		public override void SessionFinished()
		{
			Log("{0}.SessionFinished", Name);
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			Log("{0}.IterationStarting {1} {2}", Name, args.IsReproduction, args.LastWasFault);
		}

		public override void IterationFinished()
		{
			Log("{0}.IterationFinished", Name);
		}

		public override bool DetectedFault()
		{
			Log("{0}.DetectedFault", Name);

			return false;
		}

		public override MonitorData GetMonitorData()
		{
			Log("{0}.GetMonitorData", Name);

			return null;
		}

		public override void Message(string msg)
		{
			Log("{0}.Message {1}", Name, msg);
		}
	}
}
