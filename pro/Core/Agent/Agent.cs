
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NLog;

namespace Peach.Core.Agent
{
	/// <summary>
	/// Agent logic.  This class is typically
	/// called from the server side of agent channels.
	/// </summary>
	public class Agent : IAgent
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		List<Monitor> monitors = new List<Monitor>();

		public string name { get; private set; }

		public Agent()
		{
		}

		#region Publisher Helpers

		public Publisher CreatePublisher(string cls, IEnumerable<KeyValuePair<string, Variant>> args)
		{
			var newArgs = AsDict(args);
			return CreatePublisher(cls, newArgs);
		}

		public Publisher CreatePublisher(string cls, Dictionary<string, Variant> args)
		{
			logger.Trace("CreatePublisher: {0}", cls);

			var type = ClassLoader.FindTypeByAttribute<PublisherAttribute>((x, y) => y.Name == cls);
			if (type == null)
				throw new PeachException("Error, unable to locate Pubilsher '" + cls + "'");

			try
			{
				var pub = (Publisher)Activator.CreateInstance(type, args);
				return pub;
			}
			catch (TargetInvocationException ex)
			{
				throw new PeachException("Could not start publisher \"" + cls + "\".  " + ex.InnerException.Message, ex);
			}
		}

		public void StartMonitor(string name, string cls, IEnumerable<KeyValuePair<string, Variant>> args)
		{
			var newArgs = AsDict(args);
			StartMonitor(name, cls, newArgs);
		}

		#endregion

		#region IAgent Members

		public void AgentConnect()
		{
			logger.Trace("AgentConnect");
		}

		public void AgentDisconnect()
		{
			logger.Trace("AgentDisconnect");

			StopAllMonitors();
		}

		public void StartMonitor(string name, string cls, Dictionary<string, Variant> args)
		{
			logger.Debug("StartMonitor: {0} {1}", name, cls);

			var type = ClassLoader.FindTypeByAttribute<MonitorAttribute>((x, y) => y.Name == cls);
			if (type == null)
				throw new PeachException("Error, unable to locate Monitor '" + cls + "'");

			try
			{
				var mon = (Monitor)Activator.CreateInstance(type, (IAgent)this, name, args);
				monitors.Add(mon);
			}
			catch (TargetInvocationException ex)
			{
				throw new PeachException("Could not start monitor \"" + cls + "\".  " + ex.InnerException.Message, ex);
			}

		}

		public void StopAllMonitors()
		{
			logger.Trace("StopAllMonitors");

			foreach (var mon in monitors.Reverse<Monitor>())
			{
				Guard(mon, "StopMonitor", () =>
				{
					mon.StopMonitor();
				});
			}

			monitors.Clear();
		}

		public void SessionStarting()
		{
			foreach (var mon in monitors)
			{
				logger.Debug("SessionStarting: {0}", mon.Name);
				mon.SessionStarting();
			}
		}

		public void SessionFinished()
		{
			foreach (var mon in monitors.Reverse<Monitor>())
			{
				Guard(mon, "SessionFinished", () =>
				{
					mon.SessionFinished();
				});
			}
		}

		public void IterationStarting(uint iterationCount, bool isReproduction)
		{
			logger.Trace("IterationStarting: {0} {1}", iterationCount, isReproduction);

			foreach (var mon in monitors)
			{
				mon.IterationStarting(iterationCount, isReproduction);
			}
		}

		public bool IterationFinished()
		{
			logger.Trace("IterationFinished");

			var replay = false;

			foreach (var mon in monitors.Reverse<Monitor>())
			{
				Guard(mon, "IterationFinished", () =>
				{
					replay |= mon.IterationFinished();
				});
			}

			return replay;
		}

		public bool DetectedFault()
		{
			logger.Trace("DetectedFault");

			var detectedFault = false;

			foreach (var mon in monitors)
			{
				Guard(mon, "DetectedFault", () =>
				{
					detectedFault |= mon.DetectedFault();
				});
			}

			return detectedFault;
		}

		public Fault[] GetMonitorData()
		{
			logger.Trace("GetMonitorData");

			var faults = new List<Fault>();

			foreach (var mon in monitors)
			{
				Guard(mon, "GetMonitorData", () =>
				{
					var fault = mon.GetMonitorData();

					if (fault != null)
					{
						fault.monitorName = mon.Name;

						if (string.IsNullOrEmpty(fault.detectionSource))
							fault.detectionSource = mon.Class;

						faults.Add(fault);
					}
				});
			}

			return faults.ToArray();
		}

		public bool MustStop()
		{
			logger.Trace("MustStop");

			var mustStop = false;

			foreach (var mon in monitors)
			{
				Guard(mon, "MustStop", () =>
				{
					mustStop |= mon.MustStop();
				});
			}

			return mustStop;
		}

		public Variant Message(string name, Variant data)
		{
			logger.Trace("Message: {0}", name);

			Variant ret = null;

			foreach (var monitor in monitors)
			{
				var tmp = monitor.Message(name, data);
				if (tmp != null)
					ret = tmp;
			}

			return ret;
		}

		#endregion

		/// <summary>
		/// Send an information request (query) to all local monitors.
		/// </summary>
		/// <remarks>
		/// Monitors may expose information that other monitors can query.  For example a
		/// debugger monitor may expose a "QueryPid" to get the current process id.  This
		/// information could be useful to a window closing monitor that monitors windows created
		/// by the process id and closes them if needed.
		/// </remarks>
		/// <param name="query">Query to send to each monitor</param>
		/// <returns>Query response or null</returns>
		public object QueryMonitors(string query)
		{
			logger.Trace("QueryMonitors: {0}", query);

			foreach (var mon in monitors)
			{
				var ret = mon.ProcessQueryMonitors(query);
				if (ret != null)
					return ret;
			}

			return null;
		}

		private static Dictionary<string, Variant> AsDict(IEnumerable<KeyValuePair<string, Variant>> sequence)
		{
			var ret = new Dictionary<string, Variant>();

			foreach (var item in sequence)
				ret.Add(item.Key, item.Value);

			return ret;
		}

		private static void Guard(Monitor mon, string what, System.Action action)
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				logger.Warn("Ignoring {0} calling '{1}' on {2} monitor {3}: {4}",
					ex.GetType().Name, what, mon.Class, mon.Name, ex.Message);

				logger.Trace("\n{0}", ex);
			}
		}
	}
}
