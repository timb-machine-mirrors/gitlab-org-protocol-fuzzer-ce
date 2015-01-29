
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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Peach.Core;
using Peach.Core.Agent;
using Encoding = Peach.Core.Encoding;
using Monitor = Peach.Core.Agent.Monitor;
using NLog;

namespace Peach.Pro.OS.OSX.Agent.Monitors
{
	/// <summary>
	/// Monitor will use OS X's built in CrashReporter (similar to watson)
	/// to detect and report crashes.
	/// </summary>
	[Monitor("CrashWrangler", true)]
	[Monitor("osx.CrashWrangler")]
	[Core.Description("Launch a process and monitor it for crashes")]
	[Parameter("Executable", typeof(string), "Executable to launch")]
	[Parameter("Arguments", typeof(string), "Optional command line arguments", "")]
	[Parameter("StartOnCall", typeof(string), "Start command on state model call", "")]
	[Parameter("UseDebugMalloc", typeof(bool), "Use OS X Debug Malloc (slower) (defaults to false)", "false")]
	[Parameter("ExecHandler", typeof(string), "Crash Wrangler Execution Handler program.", "exc_handler")]
	[Parameter("ExploitableReads", typeof(bool), "Are read a/v's considered exploitable? (defaults to false)", "false")]
	[Parameter("NoCpuKill", typeof(bool), "Disable process killing by CPU usage? (defaults to false)", "false")]
	[Parameter("CwLogFile", typeof(string), "CrashWrangler Log file (defaults to cw.log)", "cw.log")]
	[Parameter("CwLockFile", typeof(string), "CrashWRangler Lock file (defaults to cw.lock)", "cw.lock")]
	[Parameter("CwPidFile", typeof(string), "CrashWrangler PID file (defaults to cw.pid)", "cw.pid")]
	[Parameter("FaultOnEarlyExit", typeof(bool), "Trigger fault if process exists (defaults to false)", "false")]
	[Parameter("WaitForExitOnCall", typeof(string), "Wait for process to exit on state model call and fault if timeout is reached", "")]
	[Parameter("WaitForExitTimeout", typeof(int), "Wait for exit timeout value in milliseconds (-1 is infinite)", "10000")]
	[Parameter("RestartOnEachTest", typeof(bool), "Restart process for each interation", "false")]
	public class CrashWrangler : Monitor
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public string Executable { get; set; }
		public string Arguments { get; set; }
		public string StartOnCall { get; set; }
		public bool UseDebugMalloc { get; set; }
		public string ExecHandler { get; set; }
		public bool ExploitableReads { get; set; }
		public bool NoCpuKill { get; set; }
		public string CwLogFile { get; set; }
		public string CwLockFile { get; set; }
		public string CwPidFile { get; set; }
		public bool FaultOnEarlyExit { get; set; }
		public string WaitForExitOnCall { get; set; }
		public int WaitForExitTimeout { get; set; }
		public bool RestartOnEachTest { get; set; }

		private Process _procHandler; // Handle to exec_handler process
		private Process _procCommand; // Handle to inferrior process
		private bool? _detectedFault; // Was a fault detected
		private bool _faultExitFail;  // Failed to exit within WaitForExitTimeout
		private bool _faultExitEarly; // Process exited early
		private bool _messageExit;    // Process exited due to WaitForExitOnCall

		public CrashWrangler(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			string val;
			if (args.TryGetValue("Command", out val) && !args.ContainsKey("Executable"))
			{
				Logger.Info("The parameter 'Command' on the monitor 'CrashWrangler' is deprecated.  Use the parameter 'Executable' instead.");
				args["Executable"] = val;
				args.Remove("Command");
			}

			base.StartMonitor(args);
		}

		public override void IterationStarting(uint iterationCount, bool isReproduction)
		{
			_detectedFault = null;
			_faultExitFail = false;
			_faultExitEarly = false;
			_messageExit = false;

			if (!_IsProcessRunning() && StartOnCall == null)
				_StartProcess();
		}

		public override bool DetectedFault()
		{
			if (_detectedFault == null)
			{
				// Give CrashWrangler a chance to write the log
				Thread.Sleep(500);
				_detectedFault = File.Exists(CwLogFile);

				if (!_detectedFault.Value)
				{
					if (FaultOnEarlyExit && _faultExitEarly)
						_detectedFault = true;
					else if (_faultExitFail)
						_detectedFault = true;
				}
			}

			return _detectedFault.Value;
		}

		public override Fault GetMonitorData()
		{
			if (!DetectedFault())
				return null;

			Fault fault = new Fault();
			fault.detectionSource = "CrashWrangler";
			fault.type = FaultType.Fault;

			if (File.Exists(CwLogFile))
			{
				fault.description = File.ReadAllText(CwLogFile);
				fault.collectedData.Add(new Fault.Data("StackTrace.txt", File.ReadAllBytes(CwLogFile)));

				var s = new Summary(fault.description);

				fault.majorHash = s.majorHash;
				fault.minorHash = s.minorHash;
				fault.title = s.title;
				fault.exploitability = s.exploitable;

			}
			else if (!_faultExitFail)
			{
				fault.title = "Process exited early";
				fault.description = "Process exited early: " + Executable + " " + Arguments;
				fault.folderName = "ProcessExitedEarly";
			}
			else
			{
				fault.title = "Process did not exit in " + WaitForExitTimeout + "ms";
				fault.description = fault.title + ": " + Executable + " " + Arguments;
				fault.folderName = "ProcessFailedToExit";
			}
			return fault;
		}

		public override void SessionStarting()
		{
			ExecHandler = Utilities.FindProgram(
				Path.GetDirectoryName(ExecHandler),
				Path.GetFileName(ExecHandler),
				"ExecHandler"
			);

			if (StartOnCall == null)
				_StartProcess();
		}

		public override void SessionFinished()
		{
			_StopProcess();
		}

		public override void IterationFinished()
		{
			if (!_messageExit && FaultOnEarlyExit && !_IsProcessRunning())
			{
				_faultExitEarly = true;
				_StopProcess();
			}
			else if (StartOnCall != null)
			{
				_WaitForExit(true);
				_StopProcess();
			}
			else if (RestartOnEachTest)
			{
				_StopProcess();
			}
		}

		public override void Message(string msg)
		{
			if (msg == StartOnCall)
			{
				_StopProcess();
				_StartProcess();
			}
			else if (msg == WaitForExitOnCall)
			{
				_messageExit = true;
				_WaitForExit(false);
				_StopProcess();
			}
		}

		private ulong _GetTotalCputime(Process p)
		{
			try
			{
				return ProcessInfo.Instance.Snapshot(p).TotalProcessorTicks;
			}
			catch
			{
				return 0;
			}
		}

		private bool _IsProcessRunning()
		{
			return _procCommand != null && !_procCommand.HasExited && !_IsZombie(_procCommand);
		}

		private bool _IsZombie(Process p)
		{
			try
			{
				return !ProcessInfo.Instance.Snapshot(p).Responding;
			}
			catch
			{
				return false;
			}
		}

		private bool _CommandExists()
		{
			using (var p = new Process())
			{
				p.StartInfo = new ProcessStartInfo("which", "-s \"" + Executable + "\"");
				p.Start();
				p.WaitForExit();
				return p.ExitCode == 0;
			}
		}

		private void _StartProcess()
		{
			if (!_CommandExists())
				throw new PeachException("CrashWrangler: Could not find command \"" + Executable + "\"");

			if (File.Exists(CwPidFile))
				File.Delete(CwPidFile);

			if (File.Exists(CwLogFile))
				File.Delete(CwLogFile);

			if (File.Exists(CwLockFile))
				File.Delete(CwLockFile);

			var si = new ProcessStartInfo();
			si.FileName = ExecHandler;
			si.Arguments = "\"" + Executable + "\"" + (Arguments.Length == 0 ? "" : " ") + Arguments;
			si.UseShellExecute = false;

			foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
				si.EnvironmentVariables[de.Key.ToString()] = de.Value.ToString();

			si.EnvironmentVariables["CW_NO_CRASH_REPORTER"] = "1";
			si.EnvironmentVariables["CW_QUIET"] = "1";
			si.EnvironmentVariables["CW_LOG_PATH"] = CwLogFile;
			si.EnvironmentVariables["CW_PID_FILE"] = CwPidFile;
			si.EnvironmentVariables["CW_LOCK_FILE"] = CwLockFile;

			if (UseDebugMalloc)
				si.EnvironmentVariables["CW_USE_GMAL"] = "1";

			if (ExploitableReads)
				si.EnvironmentVariables["CW_EXPLOITABLE_READS"] = "1";

			_procHandler = new Process();
			_procHandler.StartInfo = si;

			try
			{
				_procHandler.Start();
			}
			catch (Win32Exception ex)
			{

				string err = GetLastError(ex.NativeErrorCode);
				throw new PeachException(string.Format("CrashWrangler: Could not start handler \"{0}\" - {1}", ExecHandler, err), ex);
			}

			// Wait for pid file to exist, open it up and read it
			while (!File.Exists(CwPidFile) && !_procHandler.HasExited)
				Thread.Sleep(250);

			string strPid = File.ReadAllText(CwPidFile);
			int pid = Convert.ToInt32(strPid);

			try
			{
				_procCommand = Process.GetProcessById(pid);
			}
			catch (ArgumentException ex)
			{
				if (!_procHandler.HasExited)
					throw new PeachException("CrashWrangler: Could not open handle to command \"" + Executable + "\" with pid \"" + pid + "\"", ex);

				var ret = _procHandler.ExitCode;
				var log = File.Exists(CwLogFile);

				// If the exit code non-zero and no log means it was unable to run the command
				if (ret != 0 && !log)
					throw new PeachException("CrashWrangler: Handler could not run command \"" + Executable + "\"", ex);

				// If the exit code is 0 or there is a log, the program ran to completion
				if (_procCommand != null)
				{
					_procCommand.Close();
					_procCommand = null;
				}
			}
		}

		private void _StopProcess()
		{
			if (_procHandler == null)
				return;

			// Ensure a crash report is not being generated
			while (File.Exists(CwLockFile))
				Thread.Sleep(250);

			// Killing _procCommand will cause _procHandler to exit
			// _procCommand might not exist if the program ran to completion
			// prior to opening a handle to the pid
			if (_procCommand != null)
			{
				if (!_procCommand.HasExited)
				{
					_procCommand.CloseMainWindow();
					_procCommand.WaitForExit(500);

					if (!_procCommand.HasExited)
					{
						try
						{
							_procCommand.Kill();
						}
						catch (InvalidOperationException)
						{
							// Already exited between HasEcited and Kill()
						}
						_procCommand.WaitForExit();
					}
				}

				_procCommand.Close();
				_procCommand = null;
			}

			if (!_procHandler.HasExited)
			{
				_procHandler.WaitForExit();
			}

			_procHandler.Close();
			_procHandler = null;
		}

		private void _WaitForExit(bool useCpuKill)
		{
			const int pollInterval = 200;
			int i = 0;

			if (!_IsProcessRunning())
				return;

			if (useCpuKill && !NoCpuKill)
			{
				ulong lastTime = 0;

				for (i = 0; i < WaitForExitTimeout; i += pollInterval)
				{
					var currTime = _GetTotalCputime(_procCommand);

					if (i != 0 && lastTime == currTime)
						break;

					lastTime = currTime;
					Thread.Sleep(pollInterval);
				}

				_StopProcess();
			}
			else
			{
				// For some reason, Process.WaitForExit is causing a SIGTERM
				// to be delivered to the process. So we poll instead.

				if (WaitForExitTimeout >= 0)
				{
					for (i = 0; i < WaitForExitTimeout; i += pollInterval)
					{
						if (!_IsProcessRunning())
							break;

						Thread.Sleep(pollInterval);
					}

					if (i >= WaitForExitTimeout && !useCpuKill)
					{
						_detectedFault = true;
						_faultExitFail = true;
					}
				}
				else
				{
					while (_IsProcessRunning())
						Thread.Sleep(pollInterval);
				}
			}
		}

		private static string GetLastError(int err)
		{
			IntPtr ptr = strerror(err);
			string ret = Marshal.PtrToStringAnsi(ptr);
			return ret;
		}

		[DllImport("libc")]
		private static extern IntPtr strerror(int err);

		class Summary
		{
			public string majorHash { get; private set; }
			public string minorHash { get; private set; }
			public string title { get; private set; }
			public string exploitable { get; private set; }

			private static readonly string[] system_modules =
			{
				"libSystem.B.dylib",
				"libsystem_kernel.dylib",
				"libsystem_c.dylib",
				"com.apple.CoreFoundation",
				"libstdc++.6.dylib",
				"libobjc.A.dylib",
				"libgcc_s.1.dylib",
				"libgmalloc.dylib",
				"libc++abi.dylib",
				"modified_gmalloc.dylib", // Apple internal dylib
				"???",                    // For when it doesn't exist in a known module
			};

			private static readonly string[] offset_functions = 
			{
				"__memcpy",
				"__longcopy",
				"__memmove",
				"__bcopy",
				"__memset_pattern",
				"__bzero",
				"memcpy",
				"longcopy",
				"memmove",
				"bcopy",
				"bzero",
				"memset_pattern",
			};

			private const int major_depth = 5;

			public Summary(string log)
			{
				string is_exploitable = null;
				string access_type = null;
				string exception = null;

				exploitable = "UNKNOWN";

				var reProp = new Regex(@"^(((?<key>\w+)=(?<value>[^:]+):)+)$", RegexOptions.Multiline);
				var mProp = reProp.Match(log);
				if (mProp.Success)
				{
					var ti = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo;
					var keys = mProp.Groups["key"].Captures;
					var vals = mProp.Groups["value"].Captures;

					System.Diagnostics.Debug.Assert(keys.Count == vals.Count);

					for (int i = 0; i < keys.Count; ++i)
					{
						var key = keys[i].Value;
						var val = vals[i].Value;

						switch (key)
						{
							case "is_exploitable":
								is_exploitable = val.ToLower();
								break;
							case "exception":
								exception = string.Join("", val.ToLower().Split('_').Where(a => a != "exc").Select(a => ti.ToTitleCase(a)).ToArray());
								break;
							case "access_type":
								access_type = ti.ToTitleCase(val.ToLower());
								break;
						}
					}
				}

				title = string.Format("{0}{1}", access_type, exception);

				if (is_exploitable == null)
					exploitable = "UNKNOWN";
				else if (is_exploitable == "yes")
					exploitable = "EXPLOITABLE";
				else
					exploitable = "NOT_EXPLOITABLE";

				Regex reTid = new Regex(@"^Crashed Thread:\s+(\d+)", RegexOptions.Multiline);
				Match mTid = reTid.Match(log);
				if (!mTid.Success)
					return;

				string tid = mTid.Groups[1].Value;
				string strReAddr = @"^Thread " + tid + @" Crashed:.*\n((\d+\s+(?<file>\S*)\s+(?<addr>0x[0-9,a-f,A-F]+)\s(?<func>.+)\n)+)";
				Regex reAddr = new Regex(strReAddr, RegexOptions.Multiline);
				Match mAddr = reAddr.Match(log);
				if (!mAddr.Success)
					return;

				var files = mAddr.Groups["file"].Captures;
				var addrs = mAddr.Groups["addr"].Captures;
				var names = mAddr.Groups["func"].Captures;

				string maj = "";
				string min = "";
				int cnt = 0;

				for (int i = 0; i < files.Count; ++i)
				{
					var file = files[i].Value;
					var addr = addrs[i].Value;
					var name = names[i].Value;

					// Ignore certian system modules
					if (system_modules.Contains(file))
						continue;

					// When generating a signature, remove offsets for common functions
					string other = offset_functions.Where(a => name.StartsWith(a)).FirstOrDefault();
					if (other != null)
						addr = other;

					string sig = (cnt == 0 ? "" : ",") + addr;
					min += sig;

					if (++cnt <= major_depth)
						maj += sig;
				}

				// If we have no usable backtrace info, hash on the reProp line
				if (cnt == 0)
				{
					maj = mProp.Value;
					min = mProp.Value;
				}

				majorHash = Md5(maj);
				minorHash = Md5(min);
			}

			private static string Md5(string input)
			{
				using (var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider())
				{
					byte[] buf = Encoding.UTF8.GetBytes(input);
					byte[] final = md5.ComputeHash(buf);
					var sb = new StringBuilder();
					foreach (byte b in final)
						sb.Append(b.ToString("X2"));
					return sb.ToString();
				}
			}
		}
	}
}

// end
