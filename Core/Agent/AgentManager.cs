
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
using System.Text;
using System.Linq;
using System.Reflection;
using Peach.Core.Dom;
using NLog;
using Peach.Core.Agent;
using Peach.Core.IO;

namespace Peach.Core.Agent
{
	/// <summary>
	/// Manages all agents.  This includes
	/// full lifetime.
	/// </summary>
	public class AgentManager
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		OwnedCollection<AgentManager, AgentClient> agents;

		public RunContext Context { get; private set; }

		public AgentManager(RunContext context)
		{
			agents = new OwnedCollection<AgentManager,AgentClient>(this);

			Context = context;

			context.CollectFaults += OnCollectFaults;
		}

		public void AgentConnect(Dom.Agent agentDef)
		{
			logger.Trace("AgentConnect: {0}", agentDef.name);

			AgentClient agent;

			if (!agents.TryGetValue(agentDef.name, out agent))
			{
				var uri = new Uri(agentDef.location);
				var type = ClassLoader.FindTypeByAttribute<AgentAttribute>((x, y) => y.protocol == uri.Scheme);
				if (type == null)
					throw new PeachException("Error, unable to locate agent that supports the '" + uri.Scheme + "' protocol.");

				agent = Activator.CreateInstance(type, agentDef.name, agentDef.location, agentDef.password) as AgentClient;

				agents.Add(agent);
			}

			try
			{
				agent.AgentConnect();
			}
			catch
			{
				// Remove from our collection so exception
				// cleanup code doesn't cause a new exception
				// to be raised.
				agents.Remove(agent);
				throw;
			}

			foreach (var mon in agentDef.monitors)
			{
				logger.Trace("StartMonitor: {0} {1} {2}", agentDef.name, mon.name, mon.cls);
				agent.StartMonitor(mon.name, mon.cls, mon.parameters);
			}
		}

		public AgentClient GetAgent(string name)
		{
			return agents[name];
		}

		void OnCollectFaults(RunContext context)
		{
			// If the engine has recorded faults or any monitor detected a fault,
			// gather data from all monitors.
			// NOTE: We must test DetectedFault() first, as monitors expect this
			// call to occur before any call to GetMonitorData()
			if (DetectedFault() || context.faults.Count > 0)
			{
				logger.Debug("Fault detected.  Collecting monitor data.");

				var agentFaults = GetMonitorData();

				foreach (var item in agentFaults)
				{
					var faults = item.Value;

					foreach (var fault in faults)
					{
						if (fault == null)
							continue;

						fault.agentName = item.Key.name;
						context.faults.Add(fault);
					}
				}
			}
		}

		#region AgentServer

		public virtual IPublisher CreatePublisher(string agentName, string cls, Dictionary<string, Variant> args)
		{
			logger.Trace("CreatePublisher: {0} {1}", agentName, cls);

			AgentClient agent;
			if (!agents.TryGetValue(agentName, out agent))
				throw new KeyNotFoundException("Could not find agent named '" + agentName + "'.");

			return agent.CreatePublisher(cls, args);
		}

		public virtual void StopAllMonitors()
		{
			logger.Trace("StopAllMonitors");

			foreach (var agent in agents.Reverse())
			{
				Guard("StopAllMonitors", () =>
				{
					agent.StopAllMonitors();
				});
			}
		}

		public virtual void Shutdown()
		{
			logger.Trace("Shutdown");

			foreach (var agent in agents.Reverse())
			{
				Guard("Shutdown", () =>
				{
					agent.AgentDisconnect();
				});
			}
		}

		public virtual void SessionStarting()
		{
			logger.Trace("SessionStarting");

			foreach (var agent in agents)
			{
				agent.SessionStarting();
			}
		}

		public virtual void SessionFinished()
		{
			logger.Trace("SessionFinished");

			foreach (var agent in agents.Reverse())
			{
				Guard("SessionFinished", () =>
				{
					agent.SessionFinished();
				});
			}
		}

		public virtual void IterationStarting(uint iterationCount, bool isReproduction)
		{
			logger.Trace("IterationStarting");

			foreach (var agent in agents)
			{
				Guard("IterationStarting", () =>
				{
					agent.IterationStarting(iterationCount, isReproduction);
				});
			}
		}

		public virtual bool IterationFinished()
		{
			logger.Trace("IterationFinished");

			var ret = false;

			foreach (var agent in agents.Reverse())
			{
				Guard("IterationFinished", () =>
				{
					ret |= agent.IterationFinished();
				});
			}

			return ret;
		}

		public virtual bool DetectedFault()
		{
			var ret = false;

			foreach (var agent in agents)
			{
				Guard("DetectedFault", () =>
				{
					ret |= agent.DetectedFault();
				});
			}

			logger.Trace("DetectedFault: {0}", ret);

			return ret;
		}

		public virtual Dictionary<AgentClient, Fault[]> GetMonitorData()
		{
			logger.Trace("GetMonitorData");

			var faults = new Dictionary<AgentClient, Fault[]>();

			foreach (var agent in agents)
			{
				Guard("GetMonitorData", () =>
				{
					faults[agent] = agent.GetMonitorData();
				});
			}

			return faults;
		}

		public virtual bool MustStop()
		{
			var ret = false;

			foreach (var agent in agents)
			{
				Guard("MustStop", () =>
				{
					ret |= agent.MustStop();
				});
			}

			logger.Trace("MustStop: {0}", ret);

			return ret;
		}

		public virtual Variant Message(string name, Variant data)
		{
			logger.Debug("Message: {0} => {1}", name, data.ToString());

			Variant ret = null;

			foreach (var agent in agents)
			{
				Guard("Message", () =>
				{
					var tmp = agent.Message(name, data);
					if (tmp != null)
						ret = tmp;
				});
			}

			return ret;
		}

		private static void Guard(string what, System.Action action)
		{
			try
			{
				action();
			}
			catch (SoftException)
			{
				throw;
			}
			catch (PeachException)
			{
				throw;
			}
			catch (Exception ex)
			{
				logger.Warn("Ignoring {0} calling '{1}': {2}", ex.GetType().Name, what, ex.Message);
			}
		}

		#endregion
	}
}
