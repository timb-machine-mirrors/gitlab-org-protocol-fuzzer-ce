
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor;

namespace Peach.Pro.Core.Agent.Monitors
{
	/// <summary>
	/// Start a process
	/// </summary>
	[Monitor("Process", true)]
	[Monitor("process.Process")]
	[Description("Controls a process during a fuzzing run")]
	[Parameter("Executable", typeof(string), "Executable to launch")]
	[Parameter("Arguments", typeof(string), "Optional command line arguments", "")]
	[Parameter("RestartOnEachTest", typeof(bool), "Restart process for each interation", "false")]
	[Parameter("FaultOnEarlyExit", typeof(bool), "Trigger fault if process exists", "false")]
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
		public bool FaultOnEarlyExit { get; set; }
		public bool NoCpuKill { get; set; }
		public string StartOnCall { get; set; }
		public string WaitForExitOnCall { get; set; }
		public int WaitForExitTimeout { get; set; }

		public ProcessMonitor(string name)
			: base(name)
		{
		}

		private static void _LogOutput(string prefix, Func<StreamReader> stream)
		{
			var t = new Thread(new ThreadStart(delegate
			{
				try
				{
					using (var sr = stream())
					{
						while (!sr.EndOfStream)
						{
							var line = sr.ReadLine();

							if (!string.IsNullOrEmpty(line) && Logger.IsDebugEnabled)
								Logger.Debug("{0}: {1}", prefix, line);
						}
					}
				}
				// ReSharper disable once EmptyGeneralCatchClause
				catch
				{
				}
			}));

			t.Start();
		}

		void _Start()
		{
			if (_process == null || _process.HasExited)
			{
				if (_process != null)
					_process.Close();

				_process = new Process
				{
					StartInfo =
					{
						FileName = Executable,
						UseShellExecute = false,
						RedirectStandardError = true,
						RedirectStandardOutput = true
					}
				};

				if (!string.IsNullOrEmpty(Arguments))
					_process.StartInfo.Arguments = Arguments;

				Logger.Debug("_Start(): Starting process");

				try
				{
					_process.Start();

					var prefix = "{0} (0x{1:X})".Fmt(Path.GetFileName(Executable), _process.Id);

					_LogOutput(prefix, () => _process.StandardError);
					_LogOutput(prefix, () => _process.StandardOutput);
				}
				catch (Exception ex)
				{
					_process = null;
					throw new PeachException("Could not start process '" + Executable + "'.  " + ex.Message + ".", ex);
				}
			}
			else
			{
				Logger.Trace("_Start(): Process already running, ignore");
			}
		}

		void _Stop()
		{
			Logger.Trace("_Stop()");

			for (var i = 0; i < 100 && _IsRunning(); i++)
			{
				Logger.Debug("_Stop(): Killing process");

				Debug.Assert(_process != null);

				try
				{
					_process.Kill();
					_process.WaitForExit();
					_process.Close();
					_process = null;
				}
				catch (Exception ex)
				{
					Logger.Error("_Stop(): {0}", ex.Message);
				}
			}

			if (_process != null)
			{
				Logger.Debug("_Stop(): Closing process handle");
				_process.Close();
				_process = null;
			}
			else
			{
				Logger.Trace("_Stop(): _process == null, done!");
			}
		}

		void _WaitForExit(bool useCpuKill)
		{
			if (!_IsRunning())
				return;

			if (useCpuKill && !NoCpuKill)
			{
				const int pollInterval = 200;
				ulong lastTime = 0;

				try
				{
					int i;

					for (i = 0; i < WaitForExitTimeout; i += pollInterval)
					{
						var pi = ProcessInfo.Instance.Snapshot(_process);

						Logger.Trace("CpuKill: OldTicks={0} NewTicks={1}", lastTime, pi.TotalProcessorTicks);

						if (i != 0 && lastTime == pi.TotalProcessorTicks)
						{
							Logger.Debug("Cpu is idle, stopping process.");
							break;
						}

						lastTime = pi.TotalProcessorTicks;
						Thread.Sleep(pollInterval);
					}

					if (i >= WaitForExitTimeout)
						Logger.Debug("Timed out waiting for cpu idle, stopping process.");
				}
				catch (Exception ex)
				{
					Logger.Debug("Error querying cpu time: {0}", ex.Message);
				}

				_Stop();
			}
			else
			{
				Logger.Debug("WaitForExit({0})", WaitForExitTimeout == -1
					? "INFINITE" : WaitForExitTimeout.ToString(CultureInfo.InvariantCulture));

				if (!_process.WaitForExit(WaitForExitTimeout))
				{
					if (!useCpuKill)
					{
						Logger.Debug("FAULT, WaitForExit ran out of time!");
						_data = MakeFault("FailedToExit", "Process {1} did not exit in {1}ms.".Fmt(Executable, WaitForExitTimeout));
					}
				}
			}
		}

		bool _IsRunning()
		{
			return _process != null && !_process.HasExited;
		}

		MonitorData MakeFault(string majorHash, string title)
		{
			return new MonitorData
			{
				Title = title,
				Data = new Dictionary<string, byte[]>(),
				Fault = new MonitorData.Info
				{
					MajorHash = majorHash,
					MinorHash = null,
					Risk = null,
				}
			};
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			_data = null;
			_messageExit = false;

			if (RestartOnEachTest)
				_Stop();

			if (StartOnCall == null)
				_Start();
		}

		public override bool DetectedFault()
		{
			return _data != null;
		}

		public override MonitorData GetNewMonitorData()
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
			_Stop();
		}

		public override void IterationFinished()
		{
			if (!_messageExit && FaultOnEarlyExit && !_IsRunning())
			{
				_data = MakeFault("ExitedEarly", "Process '{0}' exited early".Fmt(Executable));
				_Stop();
			}
			else  if (StartOnCall != null)
			{
				_WaitForExit(true);
				_Stop();
			}
			else if (RestartOnEachTest)
			{
				_Stop();
			}
		}

		public override void Message(string msg)
		{
			if (msg == StartOnCall)
			{
				_Stop();
				_Start();
			}
			else if (msg == WaitForExitOnCall)
			{
				_messageExit = true; 
				_WaitForExit(false);
				_Stop();
			}
		}
	}
}
