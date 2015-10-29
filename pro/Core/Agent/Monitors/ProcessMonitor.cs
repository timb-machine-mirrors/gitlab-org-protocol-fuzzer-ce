
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
using System.Globalization;
using System.IO;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor2;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Agent.Monitors
{
	/// <summary>
	/// Start a process
	/// </summary>
	[Monitor("Process")]
	[Alias("process.Process")]
	[Description("Controls a process during a fuzzing run")]
	[Parameter("Executable", typeof(string), "Executable to launch")]
	[Parameter("Arguments", typeof(string), "Optional command line arguments", "")]
	[Parameter("RestartOnEachTest", typeof(bool), "Restart process for each interation", "false")]
	[Parameter("RestartAfterFault", typeof(bool), "Restart process after any fault occurs", "false")]
	[Parameter("FaultOnEarlyExit", typeof(bool), "Trigger fault if process exits", "false")]
	[Parameter("NoCpuKill", typeof(bool), "Disable process killing when CPU usage nears zero", "false")]
	[Parameter("StartOnCall", typeof(string), "Start command on state model call", "")]
	[Parameter("WaitForExitOnCall", typeof(string), "Wait for process to exit on state model call and fault if timeout is reached", "")]
	[Parameter("WaitForExitTimeout", typeof(int), "Wait for exit timeout value in milliseconds (-1 is infinite)", "10000")]
	public class ProcessMonitor : Monitor
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		Process _process;
		MonitorData _data;
		bool _messageExit;

		public string Executable { get; set; }
		public string Arguments { get; set; }
		public bool RestartOnEachTest { get; set; }
		public bool RestartAfterFault { get; set; }
		public bool FaultOnEarlyExit { get; set; }
		public bool NoCpuKill { get; set; }
		public string StartOnCall { get; set; }
		public string WaitForExitOnCall { get; set; }
		public int WaitForExitTimeout { get; set; }

		public ProcessMonitor(string name)
			: base(name)
		{
			_process = PlatformFactory<Process>.CreateInstance(Logger);
		}

		void _Start()
		{
			if (_process.IsRunning)
				return;
			
			try
			{
				_process.Start(Executable, Arguments, null, null);
				OnInternalEvent(EventArgs.Empty);
			}
			catch (Exception ex)
			{
				throw new PeachException("Could not start process '{0}'. {1}.".Fmt(Executable, ex.Message), ex);
			}
		}

		MonitorData MakeFault(string reason, string title)
		{
			return new MonitorData
			{
				Title = title,
				Data = new Dictionary<string, Stream>(),
				Fault = new MonitorData.Info
				{
					MajorHash = Hash(Class + Executable),
					MinorHash = Hash(reason),
				}
			};
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			_data = null;
			_messageExit = false;

			if ((RestartAfterFault && args.LastWasFault) || RestartOnEachTest)
				_process.Stop();

			if (StartOnCall == null)
				_Start();
		}

		public override bool DetectedFault()
		{
			return _data != null;
		}

		public override MonitorData GetMonitorData()
		{
			return _data;
		}

		public override void SessionStarting()
		{
			if (StartOnCall == null && !RestartOnEachTest)
				_Start();
		}

		public override void SessionFinished()
		{
			_process.Stop();
		}

		public override void IterationFinished()
		{
			if (!_messageExit && FaultOnEarlyExit && !_process.IsRunning)
			{
				_data = MakeFault("ExitedEarly", "Process '{0}' exited early.".Fmt(Executable));
				_process.Stop();
			}
			else  if (StartOnCall != null)
			{
				Logger.Debug("IterationFinished");
				_process.WaitForExit(WaitForExitTimeout, !NoCpuKill);
				_process.Stop();
			}
			else if (RestartOnEachTest)
			{
				_process.Stop();
			}
		}

		public override void Message(string msg)
		{
			if (msg == StartOnCall)
			{
				_process.Stop();
				_Start();
			}
			else if (msg == WaitForExitOnCall)
			{
				_messageExit = true; 
				if (!_process.WaitForExit(WaitForExitTimeout, false))
					_data = MakeFault("FailedToExit", "Process '{0}' did not exit in {1}ms.".Fmt(Executable, WaitForExitTimeout));
				_process.Stop();
			}
		}
	}
}
