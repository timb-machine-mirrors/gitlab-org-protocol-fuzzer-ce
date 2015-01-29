
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
using NLog;

namespace Peach.Core.Agent
{
	/// <summary>
	/// Manages all agents.  This includes
	/// full lifetime.
	/// </summary>
	public class AgentManager
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		readonly OwnedCollection<AgentManager, AgentClient> _agents;

		public RunContext Context { get; private set; }

		public AgentManager(RunContext context)
		{
			_agents = new OwnedCollection<AgentManager,AgentClient>(this);

			Context = context;
		}

		public void AgentConnect(Dom.Agent agentDef)
		{
			logger.Trace("AgentConnect: {0}", agentDef.Name);

			AgentClient agent;

			if (!_agents.TryGetValue(agentDef.Name, out agent))
			{
				var uri = new Uri(agentDef.location);
				var type = ClassLoader.FindTypeByAttribute<AgentAttribute>((x, y) => y.protocol == uri.Scheme);
				if (type == null)
					throw new PeachException("Error, unable to locate agent that supports the '" + uri.Scheme + "' protocol.");

				agent = Activator.CreateInstance(type, agentDef.Name, agentDef.location, agentDef.password) as AgentClient;

				_agents.Add(agent);
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
				_agents.Remove(agent);
				throw;
			}

			foreach (var mon in agentDef.monitors)
			{
				logger.Trace("StartMonitor: {0} {1} {2}", agentDef.Name, mon.Name, mon.cls);
				agent.StartMonitor(mon.Name, mon.cls, mon.parameters);
			}
		}

		public AgentClient GetAgent(string name)
		{
			return _agents[name];
		}

		public void CollectFaults()
		{
			// If the engine has recorded faults or any monitor detected a fault,
			// gather data from all monitors.
			// NOTE: We must test DetectedFault() first, as monitors expect this
			// call to occur before any call to GetMonitorData()
			if (DetectedFault() || Context.faults.Count > 0)
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

						fault.agentName = item.Key.Name;
						Context.faults.Add(fault);
					}
				}
			}
		}

		#region AgentServer

		public virtual IPublisher CreatePublisher(string agentName, string cls, Dictionary<string, Variant> args)
		{
			logger.Trace("CreatePublisher: {0} {1}", agentName, cls);

			AgentClient agent;
			if (!_agents.TryGetValue(agentName, out agent))
				throw new KeyNotFoundException("Could not find agent named '" + agentName + "'.");

			return agent.CreatePublisher(cls, args);
		}

		public virtual void StopAllMonitors()
		{
			logger.Trace("StopAllMonitors");

			foreach (var agent in _agents.Reverse())
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

			foreach (var agent in _agents.Reverse())
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

			foreach (var agent in _agents)
			{
				agent.SessionStarting();
			}
		}

		public virtual void SessionFinished()
		{
			logger.Trace("SessionFinished");

			foreach (var agent in _agents.Reverse())
			{
				Guard("SessionFinished", () =>
				{
					agent.SessionFinished();
				});
			}
		}

		public virtual void IterationStarting(bool isReproduction, bool lastWasFault)
		{
			logger.Trace("IterationStarting");

			var args = new IterationStartingArgs
			{
				IsReproduction = isReproduction,
				LastWasFault = lastWasFault
			};

			foreach (var agent in _agents)
			{
				Guard("IterationStarting", () =>
				{
					agent.IterationStarting(args);
				});
			}
		}

		public virtual void IterationFinished()
		{
			logger.Trace("IterationFinished");

			foreach (var agent in _agents.Reverse())
			{
				Guard("IterationFinished", () =>
				{
					agent.IterationFinished();
				});
			}
		}

		public virtual bool DetectedFault()
		{
			var ret = false;

			foreach (var agent in _agents)
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

			foreach (var agent in _agents)
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

			foreach (var agent in _agents)
			{
				Guard("MustStop", () =>
				{
					ret |= agent.MustStop();
				});
			}

			logger.Trace("MustStop: {0}", ret);

			return ret;
		}

		public virtual void Message(string msg)
		{
			logger.Debug("Message: {0}", msg);

			foreach (var agent in _agents)
			{
				Guard("Message", () =>
				{
					agent.Message(msg);
				});
			}
		}

		private static void Guard(string what, Action action)
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
