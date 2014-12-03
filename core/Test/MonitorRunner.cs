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
		/// Controls the IterationStarting behaviour for each monitor.
		/// The default is (m, it, repro) => m.IterationFinished(it, repro)
		/// </summary>
		public Action<Monitor, uint, bool> IterationStarting { get; set; }

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

		public MonitorRunner(string monitorClass, Dictionary<string, string> parameters)
			: this()
		{
			Add(monitorClass, parameters);
		}

		public MonitorRunner()
		{
			_monitors = new List<Monitor>();

			IterationStarting = (m, it, repro) => m.IterationStarting(it, repro);
			IterationFinished = m => m.IterationFinished();
			DetectedFault = m => m.DetectedFault();
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
				throw new PeachException("Could not start monitor \"" + monitorClass + "\".  " + ex.InnerException.Message, ex);
			}
		}

		public Fault[] Run()
		{
			return Run(1);
		}

		public Fault[] Run(int iterations)
		{
			var ret = new List<Fault>();

			Forward.ForEach(m => m.SessionStarting());

			for (uint i = 1; i <= iterations; ++i)
			{
				Forward.ForEach(m => IterationStarting(m, i, false));

				Reverse.ForEach(IterationFinished);

				if (Forward.Count(DetectedFault) > 0)
				{
					ret.AddRange(Forward.Select(m =>
					{
						var f = m.GetMonitorData();
						if (f != null)
						{
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