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
//   Michael Eddington (mike@phed.org)

// $Id$

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Peach.Core.Dom;
using NLog;

namespace Peach.Core.Agent
{

	#region Event Delegates

	public delegate void AgentConnectEventHandler(Agent agent);
	public delegate void AgentDisconnectEventHandler(Agent agent);
	public delegate void StartMonitorEventHandler(Agent agent, string name, string cls, Dictionary<string, Variant> args);
	public delegate void StopMonitorEventHandler(Agent agent, string name);
	public delegate void StopAllMonitorsEventHandler(Agent agent);
	public delegate void SessionStartingEventHandler(Agent agent);
	public delegate void SessionFinishedEventHandler(Agent agent);
	public delegate void IterationStartingEventHandler(Agent agent, int iterationCount, bool isReproduction);
	public delegate void IterationFinishedEventHandler(Agent agent);
	public delegate void DetectedFaultEventHandler(Agent agent);
	public delegate void GetMonitorDataEventHandler(Agent agent);
	public delegate void MustStopEventHandler(Agent agent);
	public delegate void MessageEventHandler(Agent agent, string name, Variant data);

	#endregion

	/// <summary>
	/// Agent logic.  This class is typically
	/// called from the server side of agent channels.
	/// </summary>
	public class Agent
	{
		public object parent;
		Dictionary<string, Monitor> monitors = new Dictionary<string, Monitor>();
		string name;
		NLog.Logger logger = LogManager.GetLogger("Peach.Core.Agent.Agent");

		#region Events

		public event AgentConnectEventHandler AgentConnectEvent;
		protected void OnAgentConnectEvent()
		{
			if (AgentConnectEvent != null)
				AgentConnectEvent(this);
		}

		public event AgentDisconnectEventHandler AgentDisconnectEvent;
		protected void OnAgentDisconnectEvent()
		{
			if (AgentDisconnectEvent != null)
				AgentDisconnectEvent(this);
		}

		public event StartMonitorEventHandler StartMonitorEvent;
		protected void OnStartMonitorEvent(string name, string cls, Dictionary<string, Variant> args)
		{
			if (StartMonitorEvent != null)
				StartMonitorEvent(this, name, cls, args);
		}

		public event StopMonitorEventHandler StopMonitorEvent;
		protected void OnStopMonitorEvent(string name)
		{
			if (StopMonitorEvent != null)
				StopMonitorEvent(this, name);
		}

		public event StopAllMonitorsEventHandler StopAllMonitorsEvent;
		protected void OnStopAllMonitorsEvent()
		{
			if (StopAllMonitorsEvent != null)
				StopAllMonitorsEvent(this);
		}

		public event SessionStartingEventHandler SessionStartingEvent;
		protected void OnSessionStartingEvent()
		{
			if (SessionStartingEvent != null)
				SessionStartingEvent(this);
		}

		public event SessionFinishedEventHandler SessionFinishedEvent;
		protected void OnSessionFinishedEvent()
		{
			if (SessionFinishedEvent != null)
				SessionFinishedEvent(this);
		}

		public event IterationStartingEventHandler IterationStartingEvent;
		protected void OnIterationStartingEvent(int iterationCount, bool isReproduction)
		{
			if (IterationStartingEvent != null)
				IterationStartingEvent(this, iterationCount, isReproduction);
		}

		public event IterationFinishedEventHandler IterationFinishedEvent;
		protected void OnIterationFinishedEvent()
		{
			if (IterationFinishedEvent != null)
				IterationFinishedEvent(this);
		}

		public event DetectedFaultEventHandler DetectedFaultEvent;
		protected void OnDetectedFaultEvent()
		{
			if (DetectedFaultEvent != null)
				DetectedFaultEvent(this);
		}

		public event GetMonitorDataEventHandler GetMonitorDataEvent;
		protected void OnGetMonitorDataEvent()
		{
			if (GetMonitorDataEvent != null)
				GetMonitorDataEvent(this);
		}

		public event MustStopEventHandler MustStopEvent;
		protected void OnMustStopEvent()
		{
			if (MustStopEvent != null)
				MustStopEvent(this);
		}

		public event MessageEventHandler MessageEvent;
		protected void OnMessageEvent(string name, Variant data)
		{
			if (MessageEvent != null)
				MessageEvent(this, name, data);
		}

		#endregion

		public Agent(string name)
		{
			this.name = name;
		}

		/// <summary>
		/// Dictionary of currently loaded monitor instances.
		/// </summary>
		public Dictionary<string, Monitor> Monitors
		{
			get { return monitors; }
			protected set { monitors = value; }
		}

		#region IAgent Members

		public void AgentConnect()
		{
			logger.Trace("AgentConnect");
			OnAgentConnectEvent();
		}

		public void AgentDisconnect()
		{
			logger.Trace("AgentDisconnect");
			OnAgentDisconnectEvent();
			StopAllMonitors();
			monitors.Clear();
		}

		protected Type GetMonitorByClass(string cls)
		{
			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (Type t in a.GetExportedTypes())
				{
					if (!t.IsClass)
						continue;

					foreach (object attrib in t.GetCustomAttributes(true))
					{
						if (attrib is MonitorAttribute)
						{
							if ((attrib as MonitorAttribute).name == cls)
								return t;
						}
					}
				}
			}

			return null;
		}

		public void StartMonitor(string name, string cls, Dictionary<string, Variant> args)
		{
			logger.Trace("StartMonitor: {0} {1}", name, cls);
			OnStartMonitorEvent(name, cls, args);

			Type tMonitor = GetMonitorByClass(cls);
			if (tMonitor == null)
				throw new PeachException("Error, unable to locate Monitor '" + cls + "'");

			ConstructorInfo co = tMonitor.GetConstructor(new Type[] { typeof(string), typeof(Dictionary<string, Variant>) });
			Monitor monitor = (Monitor)co.Invoke(new object[] { name, args });

			this.monitors.Add(name, monitor);
		}

		public void StopMonitor(string name)
		{
			logger.Trace("StopMonitor: {0}", name);
			OnStopMonitorEvent(name);
			monitors[name].StopMonitor();
			monitors.Remove(name);
		}

		public void StopAllMonitors()
		{
			logger.Trace("StopAllMonitors");
			OnStopAllMonitorsEvent();

			foreach (Monitor monitor in monitors.Values)
				monitor.StopMonitor();

			monitors.Clear();
		}

		public void SessionStarting()
		{
			logger.Trace("SessionStarting");
			OnSessionStartingEvent();

			foreach (Monitor monitor in monitors.Values)
				monitor.SessionStarting();
		}

		public void SessionFinished()
		{
			logger.Trace("SessionFinished");
			OnSessionFinishedEvent();

			foreach (Monitor monitor in monitors.Values)
				monitor.SessionFinished();
		}

		public void IterationStarting(int iterationCount, bool isReproduction)
		{
			logger.Trace("IterationStarting: {0} {1}", iterationCount, isReproduction);
			OnIterationStartingEvent(iterationCount, isReproduction);

			foreach (Monitor monitor in monitors.Values)
				monitor.IterationStarting(iterationCount, isReproduction);
		}

		public bool IterationFinished()
		{
			logger.Trace("IterationFinished");
			OnIterationFinishedEvent();

			bool replay = false;
			foreach (Monitor monitor in monitors.Values)
				if (monitor.IterationFinished())
					replay = true;

			return replay;
		}

		public bool DetectedFault()
		{
			logger.Trace("DetectedFault");
			OnDetectedFaultEvent();

			bool detectedFault = false;
			foreach (Monitor monitor in monitors.Values)
				if (monitor.DetectedFault())
					detectedFault = true;

			return detectedFault;
		}

		public Hashtable GetMonitorData()
		{
			logger.Trace("GetMonitorData");
			OnGetMonitorDataEvent();

			throw new NotImplementedException();
		}

		public bool MustStop()
		{
			logger.Trace("MustStop");
			OnMustStopEvent();

			foreach (Monitor monitor in monitors.Values)
				if (monitor.MustStop())
					return true;

			return false;
		}

		public Variant Message(string name, Variant data)
		{
			logger.Trace("Message: {0}", name);
			OnMessageEvent(name, data);

			Variant ret = null;
			Variant tmp = null;

			foreach (Monitor monitor in monitors.Values)
			{
				tmp = monitor.Message(name, data);
				if (tmp != null)
					ret = tmp;
			}

			return ret;
		}

		#endregion
	}

	public interface IAgent
	{
		void AgentConnect(string password);
		void AgentDisconnect();
		void StartMonitor(string name, string cls, Dictionary<string, Variant> args);
		void StopMonitor(string name);
		void StopAllMonitors();
		void SessionStarting();
		void SessionFinished();
		void IterationStarting(int iterationCount, bool isReproduction);
		bool IterationFinished();
		bool DetectedFault();
		Hashtable GetMonitorData();
		bool MustStop();
		Variant Message(string name, Variant data);
	}

}
