﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using NLog;
using Peach.Core;
using Peach.Core.Agent;

namespace Peach.Pro.Core.Agent.Monitors
{
	[Monitor("ProcessKiller", true)]
	[Description("Terminates the specified processes after each iteration")]
	[Parameter("ProcessNames", typeof(string[]), "Comma seperated list of process to kill.")]
	public class ProcessKillerMonitor : Peach.Core.Agent.Monitor
	{
		public string[] ProcessNames { get; private set; }

		public ProcessKillerMonitor(IAgent agent, string name, Dictionary<string, Variant> args)
			: base(agent, name, args)
		{
			ParameterParser.Parse(this, args);
		}

		public override void StopMonitor()
		{
		}

		public override void SessionStarting()
		{
		}

		public override void SessionFinished()
		{
		}

		public override void IterationStarting(uint iterationCount, bool isReproduction)
		{
		}

		public override void IterationFinished()
		{
			foreach (var item in ProcessNames)
				Kill(item);
		}

		public override bool DetectedFault()
		{
			return false;
		}

		public override Fault GetMonitorData()
		{
			return null;
		}

		public override bool MustStop()
		{
			return false;
		}

		public override Variant Message(string name, Variant data)
		{
			return null;
		}

		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		void Kill(string processName)
		{
			Process[] procs = Process.GetProcessesByName(processName);

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
					logger.Debug("Unable to kill process '{0}' (pid: {2}). {1}", processName, p.Id, ex.Message);
				}
				finally
				{
					p.Close();
				}
			}
		}
	}
}
