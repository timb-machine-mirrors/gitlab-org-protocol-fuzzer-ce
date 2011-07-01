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

namespace Peach.Core.Agent
{
	/// <summary>
	/// </summary>
	public class Agent : IAgent
	{
		public object parent;
        Dictionary<string, Monitor> monitors = new Dictionary<string, Monitor>();
        string name;
        string url;
        string password;

        public Agent(string name, string url, string password)
        {
        }

        /// <summary>
        /// Agent will not return from this method.
        /// </summary>
        public void Run()
        {
        }

        #region IAgent Members

        public void AgentConnect(string password)
        {
            if (this.password == null)
            {
                if(password != null)
                    throw new Exception("Authentication failure");
            }
            else if (this.password == password)
            {
                // All good!
            }
        }

        public void AgentDisconnect()
        {
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
			Type tMonitor = GetMonitorByClass(cls);
			if (tMonitor == null)
				throw new PeachException("Error, unable to locate Monitor '" + cls + "'");

			ConstructorInfo co = tMonitor.GetConstructor(new Type[] { typeof(string), typeof(Dictionary<string,Variant>) });
			Monitor monitor = (Monitor)co.Invoke(new object[] {name, args});

			this.monitors.Add(name, monitor);
		}

        public void StopMonitor(string name)
        {
            monitors[name].StopMonitor();
            monitors.Remove(name);
        }

        public void StopAllMonitors()
        {
            foreach (Monitor monitor in monitors.Values)
                monitor.StopMonitor();

            monitors.Clear();
        }

        public void SessionStarting()
        {
			foreach (Monitor monitor in monitors.Values)
				monitor.SessionStarting();
        }

        public void SessionFinished()
        {
			foreach (Monitor monitor in monitors.Values)
				monitor.SessionFinished();
		}

        public void IterationStarting(int iterationCount, bool isReproduction)
        {
            foreach (Monitor monitor in monitors.Values)
                monitor.IterationStarting(iterationCount, isReproduction);
        }

        public bool IterationFinished()
        {
            bool replay = false;
            foreach (Monitor monitor in monitors.Values)
                if (monitor.IterationFinished())
                    replay = true;

            return replay;
        }

        public bool DetectedFault()
        {
            bool detectedFault = false;
            foreach (Monitor monitor in monitors.Values)
                if (monitor.DetectedFault())
                    detectedFault = true;

            return detectedFault;
        }

        public Hashtable GetMonitorData()
        {
            throw new NotImplementedException();
        }

        public bool MustStop()
        {
            foreach (Monitor monitor in monitors.Values)
                if (monitor.MustStop())
                    return true;
            
            return false;
        }

		public Variant Message(string name, Variant data)
		{
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
