using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Peach.Core.Agent;

namespace Peach.Core.Test
{
	public class MonitorRunner
	{
		public delegate void Callback();

		/// <summary>
		/// Raised before IterationStarting occurs on the monitor.
		/// </summary>
		public event Callback IterationStarting;

		/// <summary>
		/// Raised before IterationFinished occurs on the monitor.
		/// </summary>
		public event Callback IterationFinished;

		public MonitorRunner(string monitorClass, Dictionary<string, string> parameters)
		{
			var type = ClassLoader.FindTypeByAttribute<MonitorAttribute>((x, y) => y.Name == monitorClass);
			Assert.NotNull(type, "Unable to locate monitor '{0}'".Fmt(monitorClass));

			var asDict = parameters.ToDictionary(t => t.Key, t => new Variant(t.Value));
			_monitor = (Monitor)Activator.CreateInstance(type, (IAgent)null, "Unnamed", asDict);
		}

		public Fault Run()
		{
			Fault ret = null;

			_monitor.SessionStarting();

			if (IterationStarting != null)
				IterationStarting();

			_monitor.IterationStarting(1, false);

			if (IterationFinished != null)
				IterationFinished();

			_monitor.IterationFinished();

			if (_monitor.DetectedFault())
			{
				ret = _monitor.GetMonitorData();
			}

			_monitor.MustStop();
			_monitor.SessionFinished();
			_monitor.StopMonitor();

			return ret;
		}

		private readonly Monitor _monitor;
	}
	
}