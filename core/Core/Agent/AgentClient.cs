
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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Peach.Core.Dom;
using NLog;
using Peach.Core.IO;

namespace Peach.Core.Agent
{
	/// <summary>
	/// Abstract base class for all Agent servers.
	/// </summary>
	public abstract class AgentClient : INamed, IOwned<AgentManager>
	{
		protected abstract NLog.Logger Logger { get; }

		/// <summary>
		/// Construct an agent client
		/// </summary>
		/// <param name="name">Name of agent</param>
		/// <param name="url">Agent URL</param>
		/// <param name="password">Agent Password</param>
		public AgentClient(string name, string url, string password)
		{
			this.name = name;
			this.url = url;
			this.password = password;
		}

		#region Public Agent Functions

		public string name { get; private set; }
		public string url { get; private set; }
		public string password { get; private set; }

		public AgentManager parent { get; set; }

		/// <summary>
		/// Connect to agent
		/// </summary>
		public void AgentConnect()
		{
			Logger.Trace("AgentConnect: {0}", name);
			parent.Context.OnAgentConnect(this);

			OnAgentConnect();
		}

		/// <summary>
		/// Disconnect from agent
		/// </summary>
		public void AgentDisconnect()
		{
			Logger.Trace("AgentDisconnect: {0}", name);
			parent.Context.OnAgentDisconnect(this);

			OnAgentDisconnect();
		}

		/// <summary>
		/// Creates a publisher on the remote agent
		/// </summary>
		/// <param name="cls">Class of publisher to create</param>
		/// <param name="args">Arguments for publisher</param>
		/// <returns>Instance of remote publisher</returns>
		public IPublisher CreatePublisher(string cls, Dictionary<string, Variant> args)
		{
			Logger.Trace("CreatePublisher: {0} {1}", name, cls);
			parent.Context.OnCreatePublisher(this, cls, args);

			return OnCreatePublisher(cls, args);
		}

		/// <summary>
		/// Start a specific monitor
		/// </summary>
		/// <param name="name">Name for monitor instance</param>
		/// <param name="cls">Class of monitor to start</param>
		/// <param name="args">Arguments</param>
		public void StartMonitor(string name, string cls, Dictionary<string, Variant> args)
		{
			Logger.Trace("StartMonitor: {0} {1} {2}", this.name, name, cls);
			parent.Context.OnStartMonitor(this, name, cls, args);

			OnStartMonitor(name, cls, args);
		}

		/// <summary>
		/// Stop all monitors currently running
		/// </summary>
		public void StopAllMonitors()
		{
			Logger.Trace("StopAllMonitors: {0}", name);
			parent.Context.OnStopAllMonitors(this);

			OnStopAllMonitors();
		}

		/// <summary>
		/// Starting a fuzzing session.  A session includes a number of test iterations.
		/// </summary>
		public void SessionStarting()
		{
			Logger.Trace("SessionStarting: {0}", name);
			parent.Context.OnSessionStarting(this);

			OnSessionStarting();
		}

		/// <summary>
		/// Finished a fuzzing session.
		/// </summary>
		public void SessionFinished()
		{
			Logger.Trace("SessionFinished: {0}", name);
			parent.Context.OnSessionFinished(this);

			OnSessionFinished();
		}

		/// <summary>
		/// Starting a new iteration
		/// </summary>
		/// <param name="iterationCount">Iteration count</param>
		/// <param name="isReproduction">Are we re-running an iteration</param>
		public void IterationStarting(uint iterationCount, bool isReproduction)
		{
			Logger.Trace("IterationStarting: {0} {1} {2}", name, iterationCount, isReproduction);
			parent.Context.OnIterationStarting(this);

			OnIterationStarting(iterationCount, isReproduction);
		}

		/// <summary>
		/// Iteration has completed.
		/// </summary>
		/// <returns>Returns true to indicate iteration should be re-run, else false.</returns>
		public bool IterationFinished()
		{
			Logger.Trace("IterationFinished: {0}", name);
			parent.Context.OnIterationFinished(this);

			return OnIterationFinished();
		}

		/// <summary>
		/// Was a fault detected during current iteration?
		/// </summary>
		/// <returns>True if a fault was detected, else false.</returns>
		public bool DetectedFault()
		{
			Logger.Trace("DetectedFault: {0}", name);
			parent.Context.OnDetectedFault(this);

			return OnDetectedFault();
		}

		/// <summary>
		/// Get the fault information
		/// </summary>
		/// <returns>Returns array of Fault instances</returns>
		public Fault[] GetMonitorData()
		{
			Logger.Trace("GetMonitorData: {0}", name);
			parent.Context.OnGetMonitorData(this);

			var ret = OnGetMonitorData();

			foreach (var item in ret)
				item.agentName = name;

			return ret;
		}

		/// <summary>
		/// Can the fuzzing session continue, or must we stop?
		/// </summary>
		/// <returns>True if session must stop, else false.</returns>
		public bool MustStop()
		{
			Logger.Trace("MustStop: {0}", name);
			parent.Context.OnMustStop(this);

			return OnMustStop();
		}

		/// <summary>
		/// Send a message to all monitors.
		/// </summary>
		/// <param name="name">Message Name</param>
		/// <param name="data">Message data</param>
		/// <returns>Returns data as Variant or null.</returns>
		public Variant Message(string name, Variant data)
		{
			Logger.Trace("Message: {0} {1}", this.name, name);
			parent.Context.OnMessage(this, name, data);

			return OnMessage(name, data);
		}

		#endregion

		#region Abstract Interface

		#region Agent Control

		/// <summary>
		/// Connect to agent
		/// </summary>
		protected abstract void OnAgentConnect();

		/// <summary>
		/// Disconnect from agent
		/// </summary>
		protected abstract void OnAgentDisconnect();

		#endregion

		#region Publisher Functions

		/// <summary>
		/// Creates a publisher on the remote agent
		/// </summary>
		/// <param name="cls">Class of publisher to create</param>
		/// <param name="args">Arguments for publisher</param>
		/// <returns>Instance of remote publisher</returns>
		protected abstract IPublisher OnCreatePublisher(string cls, Dictionary<string, Variant> args);

		#endregion

		#region Monitor Functions

		/// <summary>
		/// Start a specific monitor
		/// </summary>
		/// <param name="name">Name for monitor instance</param>
		/// <param name="cls">Class of monitor to start</param>
		/// <param name="args">Arguments</param>
		protected abstract void OnStartMonitor(string name, string cls, Dictionary<string, Variant> args);

		/// <summary>
		/// Stop all monitors currently running
		/// </summary>
		protected abstract void OnStopAllMonitors();

		/// <summary>
		/// Starting a fuzzing session.  A session includes a number of test iterations.
		/// </summary>
		protected abstract void OnSessionStarting();

		/// <summary>
		/// Finished a fuzzing session.
		/// </summary>
		protected abstract void OnSessionFinished();

		/// <summary>
		/// Starting a new iteration
		/// </summary>
		/// <param name="iterationCount">Iteration count</param>
		/// <param name="isReproduction">Are we re-running an iteration</param>
		protected abstract void OnIterationStarting(uint iterationCount, bool isReproduction);
		/// <summary>
		/// Iteration has completed.
		/// </summary>
		/// <returns>Returns true to indicate iteration should be re-run, else false.</returns>
		protected abstract bool OnIterationFinished();

		/// <summary>
		/// Was a fault detected during current iteration?
		/// </summary>
		/// <returns>True if a fault was detected, else false.</returns>
		protected abstract bool OnDetectedFault();

		/// <summary>
		/// Get the fault information
		/// </summary>
		/// <returns>Returns array of Fault instances</returns>
		protected abstract Fault[] OnGetMonitorData();

		/// <summary>
		/// Can the fuzzing session continue, or must we stop?
		/// </summary>
		/// <returns>True if session must stop, else false.</returns>
		protected abstract bool OnMustStop();

		/// <summary>
		/// Send a message to all monitors.
		/// </summary>
		/// <param name="name">Message Name</param>
		/// <param name="data">Message data</param>
		/// <returns>Returns data as Variant or null.</returns>
		protected abstract Variant OnMessage(string name, Variant data);

		#endregion

		#endregion
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class AgentAttribute : Attribute
	{
		public string protocol;
		public bool isDefault;

		public AgentAttribute(string protocol, bool isDefault = false)
		{
			this.protocol = protocol;
			this.isDefault = isDefault;
		}
	}


}
