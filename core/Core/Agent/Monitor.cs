
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
using Peach.Core.Dom;

namespace Peach.Core.Agent
{
	/// <summary>
	/// Monitors are hosted by agent processes and are
	/// able to report detected faults and gather information
	/// that is usefull when a fualt is detected.
	/// </summary>
	public abstract class Monitor : INamed
	{
		protected Monitor(string name)
		{
			Name = name;
			Class = GetType().GetAttributes<MonitorAttribute>(null).First().Name;
		}

		public enum MonitorWhen
		{
			DetectFault,
			OnCall,
			OnStart,
			OnEnd,
			OnIterationStart,
			OnIterationEnd,
			OnFault,
			OnIterationStartAfterFault
		};

		/// <summary>
		/// The name of this monitor.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The class of this monitor.
		/// </summary>
		public string Class { get; private set; }

		/// <summary>
		/// Start the monitor instance.
		/// If an exception is thrown, StopMonitor will not be called.
		/// </summary>
		public virtual void StartMonitor(Dictionary<string, string> args)
		{
			ParameterParser.Parse(this, args);
		}

		/// <summary>
		/// Stop the monitor instance.
		/// </summary>
		public virtual void StopMonitor()
		{
		}

		/// <summary>
		/// Starting a fuzzing session.  A session includes a number of test iterations.
		/// </summary>
		public virtual void SessionStarting()
		{
		}

		/// <summary>
		/// Finished a fuzzing session.
		/// </summary>
		public virtual void SessionFinished()
		{
		}

		/// <summary>
		/// Starting a new iteration
		/// </summary>
		/// <param name="iterationCount">Iteration count</param>
		/// <param name="isReproduction">Are we re-running an iteration</param>
		public virtual void IterationStarting(uint iterationCount, bool isReproduction)
		{
		}

		/// <summary>
		/// Iteration has completed.
		/// </summary>
		/// <returns>Returns true to indicate iteration should be re-run, else false.</returns>
		public virtual void IterationFinished()
		{
		}

		/// <summary>
		/// Was a fault detected during current iteration?
		/// </summary>
		/// <returns>True if a fault was detected, else false.</returns>
		public virtual bool DetectedFault()
		{
			return false;
		}

		/// <summary>
		/// Return a Fault instance
		/// </summary>
		/// <returns></returns>
		public virtual Fault GetMonitorData()
		{
			return null;
		}

		/// <summary>
		/// Can the fuzzing session continue, or must we stop?
		/// </summary>
		/// <returns>True if session must stop, else false.</returns>
		public virtual bool MustStop()
		{
			return false;
		}

		/// <summary>
		/// Send a message to the monitor and possibly get data back.
		/// </summary>
		/// <param name="msg">Message name</param>
		/// <returns>Returns data or null.</returns>
		public virtual void Message(string msg)
		{
		}

		/// <summary>
		/// An event handler that can be used by monitor implementations
		/// to alert others about interesting events occuring.
		/// The peach core does not make use of this event.
		/// </summary>
		/// <remarks>
		/// This event handler is completly ignroed by the peach core.
		/// It can be useful for writing tests against monitors so the
		/// testing framework can get notified when interesting things happen.
		/// </remarks>
		public event EventHandler InternalEvent;

		/// <summary>
		/// Raises the InternalEvent event.
		/// </summary>
		/// <param name="args">Arbitrary arguments to pass to event subscribers.</param>
		protected void OnInternalEvent(EventArgs args)
		{
			if (InternalEvent != null)
				InternalEvent(this, args);
		}
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class MonitorAttribute : PluginAttribute
	{
		public MonitorAttribute(string name, bool isDefault = false)
			: base(typeof(Monitor), name, isDefault)
		{
		}
	}
}

// end
