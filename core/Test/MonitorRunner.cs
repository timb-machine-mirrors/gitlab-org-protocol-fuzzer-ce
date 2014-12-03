using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Peach.Core.Agent;

namespace Peach.Core.Test
{
	public class MonitorRunner
	{
		/// <summary>
		/// Controls the SessionStarting behaviour for each monitor.
		/// The default is m => m.SessionStarting()
		/// </summary>
		public Action<Monitor> SessionStarting { get; set; }

		/// <summary>
		/// Controls the IterationStarting behaviour for each monitor.
		/// The default is (m, it, repro) => m.IterationFinished(it, repro)
		/// </summary>
		public Action<Monitor, uint, bool> IterationStarting { get; set; }

		/// <summary>
		/// Controls the Message behaviour for each monitor.
		/// The default is m => {}
		/// To test sending messages to monitors, tests can have
		/// implementations that do m => m.Message("Action.Call", new Variant("ScoobySnacks")
		/// </summary>
		public Action<Monitor> Message { get; set; }

		/// <summary>
		/// Controls the IterationFinished behaviour for each monitor.
		/// The default is m => m.IterationFinished()
		/// </summary>
		public Action<Monitor> IterationFinished { get; set; }

		/// <summary>
		/// Controls the DetectedFault behaviour for each monitor.
		/// The default is m => m.DetectedFault()
		/// </summary>
		public Func<Monitor, bool> DetectedFault { get; set; }

		/// <summary>
		/// Controls the GetMonitorData behaviour for each monitor.
		/// The default is m => m.GetMonitorData()
		/// </summary>
		public Func<Monitor, Fault> GetMonitorData { get; set; }

		/// <summary>
		/// Controls the MustStop behaviour for each monitor.
		/// The default is m => m.MustStop()
		/// </summary>
		public Func<Monitor, bool> MustStop { get; set; }

		/// <summary>
		/// Controls the SessionFinished behaviour for each monitor.
		/// The default is m => m.IterationFinished()
		/// </summary>
		public Action<Monitor> SessionFinished { get; set; }

		/// <summary>
		/// Controls the StopMonitor behaviour for each monitor.
		/// The default is m => m.StopMonitor()
		/// </summary>
		public Action<Monitor> StopMonitor { get; set; }


		public MonitorRunner(string monitorClass, Dictionary<string, string> parameters)
			: this()
		{
			Add(monitorClass, parameters);
		}

		public MonitorRunner()
		{
			_monitors = new List<Monitor>();

			SessionStarting = m => m.SessionStarting();
			IterationStarting = (m, it, repro) => m.IterationStarting(it, repro);
			Message = m => { };
			IterationFinished = m => m.IterationFinished();
			DetectedFault = m => m.DetectedFault();
			GetMonitorData = m => m.GetMonitorData();
			MustStop = m => m.MustStop();
			SessionFinished = m => m.SessionFinished();
			StopMonitor = m => m.StopMonitor();
		}

		public void Add(string monitorClass, Dictionary<string, string> parameters)
		{
			var type = ClassLoader.FindTypeByAttribute<MonitorAttribute>((x, y) => y.Name == monitorClass);
			Assert.NotNull(type, "Unable to locate monitor '{0}'".Fmt(monitorClass));

			var name = "Mon_{0}".Fmt(_monitors.Count);
			var asDict = parameters.ToDictionary(t => t.Key, t => new Variant(t.Value));

			try
			{
				_monitors.Add((Monitor)Activator.CreateInstance(type, (IAgent)null, name, asDict));
			}
			catch (TargetInvocationException ex)
			{
				// This here so the test runner works the same way the AgentManager does.
				// This allows tests to assert on the same exceptions that would occur
				// in the real world.
				throw new PeachException("Could not start monitor \"" + monitorClass + "\".  " + ex.InnerException.Message, ex);
			}
		}

		public Fault[] Run()
		{
			return Run(1);
		}

		public Fault[] Run(int iterations)
		{
			// Runs the monitor in the exact same way the AgentManager would.
			// Only difference is this doesn't eat any exceptions.

			var ret = new List<Fault>();

			Forward.ForEach(m => SessionStarting(m));

			for (uint i = 1; i <= iterations; ++i)
			{
				var it = i; // Capture variable for IterationStarting closure

				Forward.ForEach(m => IterationStarting(m, it, false));

				Forward.ForEach(m => Message(m));

				Reverse.ForEach(IterationFinished);

				// Note: Use Count() > 0 so we call DetectedFault on every monitor.
				// This is part of the monitor api contract.
				if (Forward.Count(DetectedFault) > 0)
				{
					// Once DetectedFault is called on every monitor we can get monitor data.
					ret.AddRange(Forward.Select(m =>
					{
						var f = m.GetMonitorData();
						if (f != null)
						{
							// Agent normally does this, so set the monitor class & name
							if (string.IsNullOrEmpty(f.detectionSource))
								f.detectionSource = m.Class;
							if (string.IsNullOrEmpty(f.monitorName))
								f.monitorName = m.Name;

							f.iteration = i;
						}
						return f;
					}).Where(f => f != null));
				}

				Reverse.ForEach(m => m.MustStop());
			}

			Reverse.ForEach(m => m.SessionFinished());

			Reverse.ForEach(m => m.StopMonitor());

			return ret.ToArray();
		}

		private IEnumerable<Monitor> Forward { get { return _monitors; } }

		private IEnumerable<Monitor> Reverse { get { return Forward.Reverse(); } }

		private readonly List<Monitor> _monitors;
	}
	
}