using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Encoding = Peach.Core.Encoding;
using Monitor = Peach.Core.Agent.Monitor2;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.OS.Linux.Agent.Monitors
{
	[Monitor("Gdb")]
	[Alias("LinuxDebugger")]
	[Description("Uses GDB to launch an executable, monitoring it for exceptions")]
	[Parameter("Executable", typeof(string), "Executable to launch")]
	[Parameter("Arguments", typeof(string), "Optional command line arguments", "")]
	[Parameter("GdbPath", typeof(string), "Path to gdb", "/usr/bin/gdb")]
	[Parameter("RestartOnEachTest", typeof(bool), "Restart process for each interation", "false")]
	[Parameter("RestartAfterFault", typeof(bool), "Restart process after any fault occurs", "false")]
	[Parameter("FaultOnEarlyExit", typeof(bool), "Trigger fault if process exists", "false")]
	[Parameter("NoCpuKill", typeof(bool), "Disable process killing when CPU usage nears zero", "false")]
	[Parameter("StartOnCall", typeof(string), "Start command on state model call", "")]
	[Parameter("WaitForExitOnCall", typeof(string), "Wait for process to exit on state model call and fault if timeout is reached", "")]
	[Parameter("WaitForExitTimeout", typeof(int), "Wait for exit timeout value in milliseconds (-1 is infinite)", "10000")]
	public class GdbDebugger : Monitor
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		static readonly string template = @"
define log_if_crash
 if ($_thread != 0x00)
  printf ""Crash detected, running exploitable.\n""
  set logging overwrite on
  set logging redirect on
  set logging on {0}
  exploitable -v
  set logging off
 end
end

handle all nostop noprint
handle SIGSEGV SIGFPE SIGABRT SIGILL SIGPIPE SIGBUS SIGSYS SIGXCPU SIGXFSZ EXC_BAD_ACCESS EXC_BAD_INSTRUCTION EXC_ARITHMETIC stop print

file {1}
set args {2}
source {3}

python
def on_start(evt):
    import tempfile, os
    h,tmp = tempfile.mkstemp()
    os.close(h)
    with open(tmp, 'w') as f:
        f.write(str(gdb.inferiors()[0].pid))
    os.renames(tmp, '{4}')
    gdb.events.cont.disconnect(on_start)
gdb.events.cont.connect(on_start)
end

printf ""starting inferior: '{1} {2}'\n""

run
log_if_crash
quit
";

		Process _gdb;
		Process _inferior;
		MonitorData _fault;
		bool _messageExit = false;
		bool _secondStart = false;
		string _exploitable = null;
		TempDirectory _tmpDir = null;
		string _gdbCmd = null;
		string _gdbPid = null;
		string _gdbLog = null;

		Regex reHash = new Regex(@"^Hash: (\w+)\.(\w+)$", RegexOptions.Multiline);
		Regex reClassification = new Regex(@"^Exploitability Classification: (.*)$", RegexOptions.Multiline);
		Regex reDescription = new Regex(@"^Short description: (.*)$", RegexOptions.Multiline);
		Regex reOther = new Regex(@"^Other tags: (.*)$", RegexOptions.Multiline);

		public string GdbPath { get; private set; }
		public string Executable { get; private set; }
		public string Arguments { get; private set; }
		public bool RestartOnEachTest { get; private set; }
		public bool RestartAfterFault { get; set; }
		public bool FaultOnEarlyExit { get; private set; }
		public bool NoCpuKill { get; private set; }
		public string StartOnCall { get; private set; }
		public string WaitForExitOnCall { get; private set; }
		public int WaitForExitTimeout { get; private set; }

		public GdbDebugger(string name)
			: base(name)
		{
			_gdb = PlatformFactory<Process>.CreateInstance(logger);
			_inferior = PlatformFactory<Process>.CreateInstance(logger);
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			base.StartMonitor(args);

			_exploitable = FindExploitable();
		}

		string FindExploitable()
		{
			var target = "gdb/exploitable/exploitable.py";

			var dirs = new List<string> {
				Utilities.ExecutionDirectory,
				Environment.CurrentDirectory,
			};

			string path = Environment.GetEnvironmentVariable("PATH");
			if (!string.IsNullOrEmpty(path))
				dirs.AddRange(path.Split(Path.PathSeparator));

			foreach (var dir in dirs)
			{
				string full = Path.Combine(dir, target);
				if (File.Exists(full))
					return full;
			};

			throw new PeachException("Error, Gdb could not find '" + target + "' in search path.");
		}

		void _Start()
		{
			try
			{
				_gdb.Start(GdbPath, "-batch -n -x {0}".Fmt(_gdbCmd), null, _tmpDir.Path);
			}
			catch (Exception ex)
			{
				throw new PeachException("Could not start debugger '{0}'. {1}.".Fmt(GdbPath, ex.Message), ex);
			}

			// Wait for pid file to exist, open it up and read it
			while (!File.Exists(_gdbPid) && _gdb.IsRunning)
				Thread.Sleep(10);

			if (!File.Exists(_gdbPid) && !_gdb.IsRunning)
				throw new PeachException("GDB was unable to start '{0}'.".Fmt(Executable));

			try
			{
				var pid = Convert.ToInt32(File.ReadAllText(_gdbPid));
				_inferior.Attach(pid);
			}
			catch (ArgumentException)
			{
				// inferior ran to completion
			}

			// Notify event handler the process started
			OnInternalEvent(EventArgs.Empty);
		}

		void _Stop()
		{
			_inferior.Shutdown();
			_gdb.WaitForIdle(WaitForExitTimeout);
			_inferior.Dispose();
		}

		MonitorData MakeFault(string type, string reason)
		{
			var ret = new MonitorData
			{
				Title = reason,
				Data = new Dictionary<string, Stream>
				{
					{ "stdout.log", new MemoryStream(File.ReadAllBytes(Path.Combine(_tmpDir.Path, "stdout.log"))) },
					{ "stderr.log", new MemoryStream(File.ReadAllBytes(Path.Combine(_tmpDir.Path, "stderr.log"))) }
				},
				Fault = new MonitorData.Info
				{
					Description = "{0} {1} {2}".Fmt(reason, Executable, Arguments),
					MajorHash = Hash(Class + Executable),
					MinorHash = Hash(type),
				}
			};

			return ret;
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			var firstStart = !_secondStart;

			_fault = null;
			_messageExit = false;
			_secondStart = true;

			if ((RestartAfterFault && args.LastWasFault) || RestartOnEachTest || !_gdb.IsRunning)
				_Stop();
			else if (firstStart)
				return;

			if (!_gdb.IsRunning && StartOnCall == null)
				_Start();
		}

		public override bool DetectedFault()
		{
			if (!File.Exists(_gdbLog))
				return _fault != null;

			logger.Info("DetectedFault - Caught fault with gdb");

			_Stop();

			byte[] bytes = File.ReadAllBytes(_gdbLog);
			string output = Encoding.UTF8.GetString(bytes);

			_fault = new MonitorData
			{
				Data = new Dictionary<string, Stream>(),
				Fault = new MonitorData.Info()
			};

			var hash = reHash.Match(output);
			if (hash.Success)
			{
				_fault.Fault.MajorHash = hash.Groups[1].Value.Substring(0, 8).ToUpper();
				_fault.Fault.MinorHash = hash.Groups[2].Value.Substring(0, 8).ToUpper();
			}

			var exp = reClassification.Match(output);
			if (exp.Success)
				_fault.Fault.Risk = exp.Groups[1].Value;

			var desc = reDescription.Match(output);
			if (desc.Success)
				_fault.Title = desc.Groups[1].Value;

			var other = reOther.Match(output);
			if (other.Success)
				_fault.Title += ", " + other.Groups[1].Value;

			_fault.Data.Add("StackTrace.txt", new MemoryStream(bytes));
			_fault.Data.Add("stdout.log", new MemoryStream(File.ReadAllBytes(Path.Combine(_tmpDir.Path, "stdout.log"))));
			_fault.Data.Add("stderr.log", new MemoryStream(File.ReadAllBytes(Path.Combine(_tmpDir.Path, "stderr.log"))));
			_fault.Fault.Description = output;

			return true;
		}

		public override MonitorData GetMonitorData()
		{
			return _fault;
		}

		public override void SessionStarting()
		{
			_tmpDir = new TempDirectory();
			_gdbCmd = Path.Combine(_tmpDir.Path, "gdb.cmd");
			_gdbPid = Path.Combine(_tmpDir.Path, "gdb.pid");
			_gdbLog = Path.Combine(_tmpDir.Path, "gdb.log");

			string cmd = string.Format(template, _gdbLog, Executable, Arguments, _exploitable, _gdbPid);
			File.WriteAllText(_gdbCmd, cmd);

			logger.Debug("Wrote gdb commands to '{0}'", _gdbCmd);

			if (StartOnCall == null && !RestartOnEachTest)
				_Start();
		}

		public override void SessionFinished()
		{
			_Stop();
			_tmpDir.Dispose();
		}

		public override void IterationFinished()
		{
			if (!_messageExit && FaultOnEarlyExit && !_gdb.IsRunning)
			{
				_Stop(); // Stop 1st so stdout/stderr logs are closed
				_fault = MakeFault("ExitedEarly", "Process exited early.");
			}
			else if (StartOnCall != null)
			{
				if (!NoCpuKill)
					_inferior.WaitForIdle(WaitForExitTimeout);
				else
					_inferior.WaitForExit(WaitForExitTimeout);
				_gdb.Stop(WaitForExitTimeout);
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

				if (!_gdb.WaitForExit(WaitForExitTimeout))
					_fault = MakeFault("FailedToExit", "Process did not exit in " + WaitForExitTimeout + "ms.");
			}
		}
	}
}
