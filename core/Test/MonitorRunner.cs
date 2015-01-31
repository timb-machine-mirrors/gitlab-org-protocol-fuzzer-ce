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
		/// Controls the StartMonitor behaviour for each monitor.
		/// The default is (m, args) => m.StartMonitor(args)
		/// </summary>
		public Action<Monitor, Dictionary<string, string>> StartMonitor { get; set; }

		/// <summary>
		/// Controls the SessionStarting behaviour for each monitor.
		/// The default is m => m.SessionStarting()
		/// </summary>
		public Action<Monitor> SessionStarting { get; set; }

		/// <summary>
		/// Controls the IterationStarting behaviour for each monitor.
		/// The default is (m, args) => m.IterationFinished(repro, args)
		/// </summary>
		public Action<Monitor, IterationStartingArgs> IterationStarting { get; set; }

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
		public Func<Monitor, MonitorData> GetMonitorData { get; set; }

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
			_monitors = new List<Holder>();

			StartMonitor = (m, args) => m.StartMonitor(args);
			SessionStarting = m => m.SessionStarting();
			IterationStarting = (m, args) => m.IterationStarting(args);
			Message = m => { };
			IterationFinished = m => m.IterationFinished();
			DetectedFault = m => m.DetectedFault();
			GetMonitorData = m => m.GetNewMonitorData();
			MustStop = m => m.MustStop();
			SessionFinished = m => m.SessionFinished();
			StopMonitor = m => m.StopMonitor();
		}

		public void Add(string monitorClass, Dictionary<string, string> parameters)
		{
			Add("Mon_{0}".Fmt(_monitors.Count), monitorClass, parameters);
		}

		public void Add(string monitorName, string monitorClass, Dictionary<string, string> parameters)
		{
			var type = ClassLoader.FindTypeByAttribute<MonitorAttribute>((x, y) => y.Name == monitorClass);
			Assert.NotNull(type, "Unable to locate monitor '{0}'".Fmt(monitorClass));

			try
			{
				var item = new Holder
				{
					Monitor = (Monitor) Activator.CreateInstance(type, monitorName),
					Args = parameters
				};

				_monitors.Add(item);
			}
			catch (Exception ex)
			{
				// This here so the test runner works the same way the AgentManager does.
				// This allows tests to assert on the same exceptions that would occur
				// in the real world.
				throw new PeachException("Could not start monitor \"" + monitorClass + "\".  " + ex.Message, ex);
			}
		}

		public MonitorData[] Run()
		{
			return Run(1);
		}

		public MonitorData[] Run(int iterations)
		{
			// Runs the monitor in the exact same way the AgentManager would.
			// Only difference is this doesn't eat any exceptions.

			var ret = new List<MonitorData>();
			var lastWasFault = false;

			_monitors.ForEach(i => StartMonitor(i.Monitor, i.Args));

			Forward.ForEach(m => SessionStarting(m));

			for (uint i = 1; i <= iterations; ++i)
			{
				var args = new IterationStartingArgs
				{
					IsReproduction = false,
					LastWasFault = lastWasFault
				};

				lastWasFault = false;

				Forward.ForEach(m => IterationStarting(m, args));

				Forward.ForEach(m => Message(m));

				Reverse.ForEach(IterationFinished);

				// Note: Use Count() > 0 so we call DetectedFault on every monitor.
				// This is part of the monitor api contract.
				if (Forward.Count(DetectedFault) > 0)
				{
					lastWasFault = true;

					// Once DetectedFault is called on every monitor we can get monitor data.
					ret.AddRange(Forward.Select(m =>
					{
						var f = GetMonitorData(m);
						if (f != null)
						{
							// Agent normally does this, so set the monitor class & name
							f.DetectionSource = f.DetectionSource ?? m.Class;
							f.MonitorName = m.Name;
						}
						return f;
					}).Where(f => f != null));
				}

				Reverse.ForEach(m => MustStop(m));
			}

			Reverse.ForEach(m => SessionFinished(m));

			Reverse.ForEach(m => StopMonitor(m));

			return ret.ToArray();
		}

		private IEnumerable<Monitor> Forward { get { return _monitors.Select(i => i.Monitor); } }

		private IEnumerable<Monitor> Reverse { get { return Forward.Reverse(); } }

		class Holder
		{
			public Monitor Monitor { get; set; }
			public Dictionary<string, string> Args { get; set; }
		}

		private readonly List<Holder> _monitors;
	}
	
}