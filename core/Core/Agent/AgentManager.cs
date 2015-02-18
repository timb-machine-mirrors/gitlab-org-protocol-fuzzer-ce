﻿
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
using System.IO;
using System.Linq;
using NLog;

namespace Peach.Core.Agent
{
	/// <summary>
	/// Manages all agents.  This includes full lifetime.
	/// </summary>
	public class AgentManager : IDisposable
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		private readonly NamedCollection<AgentClient> _agents = new NamedCollection<AgentClient>();

		public RunContext Context { get; private set; }

		public AgentManager(RunContext context)
		{
			Context = context;
		}

		public void Connect(Dom.Agent agentDef)
		{
			Logger.Trace("AgentConnect: {0}", agentDef.Name);

			AgentClient agent;

			if (!_agents.TryGetValue(agentDef.Name, out agent))
			{
				var uri = new Uri(agentDef.location);
				var type = ClassLoader.FindPluginByName<AgentAttribute>(uri.Scheme);
				if (type == null)
					throw new PeachException("Error, unable to locate agent that supports the '" + uri.Scheme + "' protocol.");

				agent = (AgentClient)Activator.CreateInstance(type, agentDef.Name, agentDef.location, agentDef.password);

				_agents.Add(agent);
			}

			try
			{
				Logger.Trace("AgentConnect: {0}", agent.Name);
				Context.OnAgentConnect(agent);
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
				Logger.Trace("StartMonitor: {0} {1} {2}", agentDef.Name, mon.Name, mon.cls);
				Context.OnStartMonitor(agent, mon.Name, mon.cls);
				agent.StartMonitor(mon.Name, mon.cls, mon.parameters.ToDictionary(i => i.Key, i => (string)i.Value));
			}

			Logger.Trace("SessionStarting: {0}", agent.Name);
			Context.OnSessionStarting(agent);
			agent.SessionStarting();
		}

		public void CollectFaults()
		{
			// If the engine has recorded faults or any monitor detected a fault,
			// gather data from all monitors.
			// NOTE: We must test DetectedFault() first, as monitors expect this
			// call to occur before any call to GetMonitorData()
			if (DetectedFault() || Context.faults.Count > 0)
			{
				Logger.Debug("Fault detected.  Collecting monitor data.");
				GetMonitorData();
			}
		}

		public IPublisher CreatePublisher(string agentName, string name, string cls, Dictionary<string, string> args)
		{
			AgentClient agent;
			if (!_agents.TryGetValue(agentName, out agent))
				throw new KeyNotFoundException("Could not find agent named '" + agentName + "'.");

			Logger.Trace("CreatePublisher: {0} {1} {2}", agentName, name, cls);
			Context.OnCreatePublisher(agent, name, cls);
			return agent.CreatePublisher(name, cls, args);
		}

		public void Dispose()
		{
			foreach (var agent in _agents.Reverse())
			{
				Logger.Trace("SessionFinished: {0}", agent.Name);
				Context.OnSessionFinished(agent);
				Guard(agent, "SessionFinished", a => a.SessionFinished());
			}

			foreach (var agent in _agents.Reverse())
			{
				Logger.Trace("StopAllMonitors: {0}", agent.Name);
				Context.OnStopAllMonitors(agent);
				Guard(agent, "StopAllMonitors", a => a.StopAllMonitors());
			}

			foreach (var agent in _agents.Reverse())
			{
				Logger.Trace("AgentDisconnect: {0}", agent.Name);
				Context.OnAgentDisconnect(agent);
				Guard(agent, "Shutdown", a => a.AgentDisconnect());
			}
		}

		public void IterationStarting(bool isReproduction, bool lastWasFault)
		{
			var args = new IterationStartingArgs
			{
				IsReproduction = isReproduction,
				LastWasFault = lastWasFault
			};

			foreach (var agent in _agents)
			{
				Logger.Trace("IterationStarting: {0} {1} {2}", agent.Name, args.IsReproduction, args.LastWasFault);
				Context.OnIterationStarting(agent);
				Guard(agent, "IterationStarting", a => a.IterationStarting(args));
			}
		}

		public void IterationFinished()
		{

			foreach (var agent in _agents.Reverse())
			{
				Logger.Trace("IterationFinished: {0}", agent.Name);
				Context.OnIterationFinished(agent);
				Guard(agent, "IterationFinished", a => a.IterationFinished());
			}
		}

		public void Message(string msg)
		{
			foreach (var agent in _agents)
			{
				Logger.Debug("Message: {0} {1}", agent.Name, msg);
				Context.OnMessage(agent, msg);
				Guard(agent, "Message", a => a.Message(msg));
			}
		}

		private bool DetectedFault()
		{
			var ret = false;

			foreach (var agent in _agents)
			{
				Logger.Debug("DetectedFault: {0}", agent.Name);
				Context.OnDetectedFault(agent);
				Guard(agent, "DetectedFault", a => ret |= a.DetectedFault());
			}

			Logger.Trace("DetectedFault: {0}", ret);

			return ret;
		}

		private void GetMonitorData()
		{
			foreach (var agent in _agents)
			{
				Logger.Trace("GetMonitorData {0}", agent.Name);
				Context.OnGetMonitorData(agent);
				Guard(agent, "GetMonitorData", a => Context.faults.AddRange(a.GetMonitorData().Select(AsFault)));
			}
		}

		private static Fault AsFault(MonitorData data)
		{
			var ret = new Fault
			{
				agentName = data.AgentName,
				monitorName = data.MonitorName,
				detectionSource = data.DetectionSource,
				title = data.Title,
				type = FaultType.Data,
			};

			ret.collectedData.AddRange(data.Data.Select(d => new Fault.Data {Key = d.Key, Value = ToByteArray(d.Value)}));

			if (data.Fault != null)
			{
				ret.type = FaultType.Fault;
				ret.description = data.Fault.Description;
				ret.majorHash = data.Fault.MajorHash;
				ret.minorHash = data.Fault.MinorHash;
				ret.exploitability = data.Fault.Risk;
				ret.mustStop = data.Fault.MustStop;
			}

			return ret;
		}

		private static byte[] ToByteArray(Stream strm)
		{
			var buf = new byte[strm.Length];

			strm.Seek(0, SeekOrigin.Begin);
			strm.Read(buf, 0, buf.Length);

			return buf;
		}

		private static void Guard(AgentClient agent, string what, Action<AgentClient> action)
		{
			try
			{
				action(agent);
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
				Logger.Warn("Ignoring {0} calling '{1}' on '{2}': {3}", ex.GetType().Name, what, agent.Name, ex.Message);
			}
		}
	}
}
