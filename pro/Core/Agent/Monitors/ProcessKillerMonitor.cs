using System;
using System.Diagnostics;
using NLog;
using Peach.Core;
using Peach.Core.Agent;

namespace Peach.Pro.Core.Agent.Monitors
{
	[Monitor("ProcessKiller")]
	[Description("Terminates the specified processes after each iteration")]
	[Parameter("ProcessNames", typeof(string[]), "Comma seperated list of process to kill.")]
	public class ProcessKillerMonitor : Monitor
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public string[] ProcessNames { get; set; }

		public ProcessKillerMonitor(string name)
			: base(name)
		{
		}

		public override void IterationFinished()
		{
			foreach (var item in ProcessNames)
				Kill(item);
		}

		private static void Kill(string processName)
		{
			var procs = Process.GetProcessesByName(processName);

			foreach (var p in procs)
			{
				try
				{
					if (!p.HasExited)
					{
						p.Kill();
						p.WaitForExit();
					}
				}
				catch (Exception ex)
				{
					Logger.Debug("Unable to kill process '{0}' (pid: {2}). {1}", processName, p.Id, ex.Message);
				}
				finally
				{
					p.Close();
				}
			}
		}
	}
}
