using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Encoding = Peach.Core.Encoding;

namespace Peach.Pro.OS.Linux.Agent.Monitors
{
	[Monitor("LinuxDebugger", true)]
	[Peach.Core.Description("Uses GDB to launch an executable, monitoring it for exceptions")]
	[Parameter("Executable", typeof(string), "Executable to launch")]
	[Parameter("Arguments", typeof(string), "Optional command line arguments", "")]
	[Parameter("GdbPath", typeof(string), "Path to gdb", "/usr/bin/gdb")]
	[Parameter("RestartOnEachTest", typeof(bool), "Restart process for each interation", "false")]
	[Parameter("FaultOnEarlyExit", typeof(bool), "Trigger fault if process exists", "false")]
	[Parameter("NoCpuKill", typeof(bool), "Disable process killing when CPU usage nears zero", "false")]
	[Parameter("StartOnCall", typeof(string), "Start command on state model call", "")]
	[Parameter("WaitForExitOnCall", typeof(string), "Wait for process to exit on state model call and fault if timeout is reached", "")]
	[Parameter("WaitForExitTimeout", typeof(int), "Wait for exit timeout value in milliseconds (-1 is infinite)", "10000")]
	public class LinuxDebugger : Peach.Core.Agent.Monitor
	{
		private class CaptureStream : IDisposable
		{
			private class BufferedReader : Stream
			{
				private const int BlockSize = 1024 * 1024;

				private readonly CaptureStream _parent;
				private readonly byte[] _buffer;
				private int _position;
				private int _length;
				private bool _closed;

				public BufferedReader(CaptureStream parent)
				{
					_parent = parent;
					_buffer = new byte[BlockSize];
					_position = 0;
					_length = 0;
					_closed = false;
				}

				public override int Read(byte[] buffer, int offset, int count)
				{
					if (_length == _position && !_closed)
					{
						try
						{
							// Read the next block of data from the process
							_length = _parent._stream.Read(_buffer, 0, _buffer.Length);

							if (_length == 0)
								Log("{0} Read Zero Bytes!", _parent._name);
						}
						catch (ObjectDisposedException)
						{
							// File was closed in Stop() before we read all of the data
							var msg = Encoding.UTF8.GetBytes("{0}{0}--- TRUNCATED ---{0}".Fmt(Environment.NewLine));
							Debug.Assert(msg.Length <= _buffer.Length);
							_length = Math.Min(msg.Length, _buffer.Length);
							Buffer.BlockCopy(msg, 0, _buffer, 0, _length);
							_closed = true;
						}

						_position = 0;


						// Log the data to disk
						_parent._file.Write(_buffer, 0, _length);
					}

					var ret = Math.Min(count, _length - _position);

					Buffer.BlockCopy(_buffer, _position, buffer, offset, ret);

					_position += ret;

					return ret;
				}

				public override void Flush()
				{
					throw new NotImplementedException();
				}

				public override long Seek(long offset, SeekOrigin origin)
				{
					throw new NotImplementedException();
				}

				public override void SetLength(long value)
				{
					throw new NotImplementedException();
				}

				public override void Write(byte[] buffer, int offset, int count)
				{
					throw new NotImplementedException();
				}

				public override bool CanRead
				{
					get { return true; }
				}

				public override bool CanSeek
				{
					get { return false; }
				}

				public override bool CanWrite
				{
					get { return false; }
				}

				public override long Length
				{
					get { throw new NotImplementedException(); }
				}

				public override long Position
				{
					get { throw new NotImplementedException(); }
					set { throw new NotImplementedException(); }
				}
			}

			private static readonly NLog.Logger Logger = logger;
			private readonly string _name;
			private readonly FileStream _file;
			private readonly Stream _stream;
			private readonly Thread _fileThread;

			public CaptureStream(LinuxDebugger owner, string fileName, Func<Process, StreamReader> strm)
			{
				var fullPath = Path.Combine(owner._tmpPath, fileName);

				_file = File.Open(fullPath, FileMode.Create, FileAccess.Write);
				_name = "[{0}:{1}]".Fmt(owner._procHandler.Id, Path.GetFileNameWithoutExtension(fileName));

				// Open up stdout/stderr from the gdb process
				_stream = strm(owner._procHandler).BaseStream;

				_fileThread = new Thread(LogAndSaveToFile) { Name = _name };

				_fileThread.Start();
			}

			public void Dispose()
			{
				Log("{0} >>> Dispose", _name);

				// Close the stdout/stderr stream 1st
				// This will cause _fileThread to exit if it already hasn't
				_stream.Close();

				Log("{0} >>> Joining File Thread", _name);

				_fileThread.Join();

				Log("{0} <<< Joining File Thread", _name);

				// Close the stdout/stderr log file last so _fileThread
				// can write out all of its data to it
				_file.Close();

				Log("{0} <<< Dispose", _name);
			}

			private void LogAndSaveToFile()
			{
				Log("{0} >>> LogAndSaveToFile", _name);

				// Build a stream reader on top of a buffered reader on top of stdout/stderr
				// When ReadLine() is called on the stream reader, it will in turn call
				// Read() on the BufferedReader.  This will cause a read of up to 1M
				// from stdout/stderr and log that data to a file.  The BufferedReader will
				// then return byte at a time to the StreamReader, which will translate the
				// bytes into individual lines of text for subsequent trace logging.

				// NOTE: We don't need to close the StreamReader or BufferedReader since
				// the actual underlying stdout/stderr handle will be closed in Dispose()

				var rdr = new StreamReader(new BufferedReader(this), new System.Text.UTF8Encoding());

				using (var queue = new BlockingCollection<string>())
				{
					var writer = new Thread(() =>
					{
						Log("{0} >>> Log Writer Thread", _name);

						foreach (var line in queue.GetConsumingEnumerable())
						{
							Logger.Debug("{0} {1}", _name, line);

							if (queue.IsAddingCompleted)
								break;
						}

						if (queue.Count > 0)
							Logger.Trace("{0} --- LOGGING TRUNCATED ---", _name);

						Log("{0} <<< Log Writer Thread", _name);
					})
					{
						Name = "{0} Logger".Fmt(_name),
					};

					writer.Start();

					while (!rdr.EndOfStream)
					{
						var line = rdr.ReadLine();
						if (!string.IsNullOrEmpty(line))
						{
							Log("{0} QUEUE {1}", _name, line);
							queue.Add(line);
						}
					}

					Log("{0} >>> Completing Queue", _name);

					queue.CompleteAdding();

					Log("{0} <<< Completing Queue", _name);
					Log("{0} >>> Join Log Writer", _name);

					writer.Join();

					Log("{0} <<< Join Log Writer", _name);
				}

				Log("{0} <<< LogAndSaveToFile", _name);
			}

			[Conditional("DEBUG")]
			private static void Log(string fmt, params object[] args)
			{
				Logger.Trace(fmt, args);
			}
		}

		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		static readonly string template = @"
define log_if_crash
 if ($_thread != 0x00)
  printf ""Crash detected, running exploitable.\n""
  source {3}
  set logging overwrite on
  set logging redirect on
  set logging on {0}
  exploitable -v
  set logging off
 end
end

handle all nostop noprint
handle SIGSEGV SIGFPE SIGABRT EXC_BAD_ACCESS EXC_BAD_INSTRUCTION EXC_ARITHMETIC stop print

file {1}
set args {2}

python
def on_start(evt):
    import tempfile, os
    h,tempfilename = tempfile.mkstemp()
    os.close(h)
    with open(tempfilename, 'w') as f:
        f.write(str(gdb.inferiors()[0].pid))
    os.renames(tempfilename,'{4}')
    gdb.events.cont.disconnect(on_start)
gdb.events.cont.connect(on_start)
end

run
log_if_crash
quit
";



		CaptureStream _stdout;
		CaptureStream _stderr;
		Process _procHandler;
		Process _procCommand;
		Fault _fault = null;
		bool _messageExit = false;
		string _exploitable = null;
		string _tmpPath = null;
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
		public bool FaultOnEarlyExit { get; private set; }
		public bool NoCpuKill { get; private set; }
		public string StartOnCall { get; private set; }
		public string WaitForExitOnCall { get; private set; }
		public int WaitForExitTimeout { get; private set; }

		public LinuxDebugger(IAgent agent, string name, Dictionary<string, Variant> args)
			: base(agent, name, args)
		{
			ParameterParser.Parse(this, args);

			_exploitable = FindExploitable();
		}

		string FindExploitable()
		{
			var target = "gdb/exploitable/exploitable.py";

			var dirs = new List<string> {
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
				Directory.GetCurrentDirectory(),
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

			throw new PeachException("Error, LinuxDebugger could not find '" + target + "' in search path.");
		}

		void _Start()
		{
			var si = new ProcessStartInfo();
			si.FileName = GdbPath;
			si.Arguments = "-batch -n -x " + _gdbCmd;
			si.UseShellExecute = false;
			si.RedirectStandardInput = true;
			si.RedirectStandardOutput = true;
			si.RedirectStandardError = true;

			_procHandler = new System.Diagnostics.Process();
			_procHandler.StartInfo = si;

			logger.Debug("_Start(): Starting gdb process");

			if (File.Exists(_gdbLog))
				File.Delete(_gdbLog);

			if (File.Exists(_gdbPid))
				File.Delete(_gdbPid);

			try
			{
				_procHandler.Start();
			}
			catch (Exception ex)
			{
				_procHandler = null;
				throw new PeachException("Could not start debugger '" + GdbPath + "'.  " + ex.Message + ".", ex);
			}

			logger.Debug("[{0}] gdb {1}{2}{3}",
				_procHandler.Id,
				Executable,
				string.IsNullOrEmpty(Arguments) ? "" : " ",
				Arguments);

			// Close stdin so all reads return zero
			_procHandler.StandardInput.Close();

			// Start capturing stdout and stderr
			_stdout = new CaptureStream(this, "stdout.log", p => p.StandardOutput);
			_stderr = new CaptureStream(this, "stderr.log", p => p.StandardError);

			// Wait for pid file to exist, open it up and read it
			while (!File.Exists(_gdbPid) && !_procHandler.HasExited)
				Thread.Sleep(10);

			if (!File.Exists(_gdbPid) && _procHandler.HasExited)
				throw new PeachException("GDB was unable to start '" + Executable + "'.");

			string strPid = File.ReadAllText(_gdbPid);
			int pid = Convert.ToInt32(strPid);

			try
			{
				_procCommand = Process.GetProcessById(pid);
			}
			catch (ArgumentException)
			{
				// Program ran to completion
				_procCommand = null;
			}
		}

		void _Stop()
		{
			if (_procHandler == null)
				return;

			// Stopping procCommand will cause procHandler to exit
			if (_procCommand != null)
			{
				if (!_procCommand.HasExited)
				{
					logger.Debug("_Stop(): Stopping process");
					Mono.Unix.Native.Syscall.kill(_procCommand.Id, Mono.Unix.Native.Signum.SIGTERM);

					if (!WaitForExit(_procCommand, 500))
					{
						logger.Debug("_Stop(): Killing process");
						Mono.Unix.Native.Syscall.kill(_procCommand.Id, Mono.Unix.Native.Signum.SIGKILL);

						WaitForExit(_procCommand, -1);
					}
				}

				logger.Debug("_Stop(): Closing process");
				_procCommand.Close();
				_procCommand = null;
			}

			if (!_procHandler.HasExited)
			{
				logger.Debug("_Stop(): Waiting for gdb to complete");
				_procHandler.WaitForExit();
			}

			logger.Debug("_Stop(): Closing gdb");
			_procHandler.Close();
			_procHandler = null;

			_stdout.Dispose();
			_stdout = null;

			_stderr.Dispose();
			_stderr = null;
		}

		void _WaitForExit(bool useCpuKill)
		{
			if (!_IsRunning())
				return;

			if (useCpuKill && !NoCpuKill)
			{
				const int pollInterval = 200;
				ulong lastTime = 0;
				int i = 0;

				try
				{
					for (i = 0; i < WaitForExitTimeout; i += pollInterval)
					{
						var pi = ProcessInfo.Instance.Snapshot(_procCommand);

						logger.Trace("CpuKill: OldTicks={0} NewTicks={1}", lastTime, pi.TotalProcessorTicks);

						if (i != 0 && lastTime == pi.TotalProcessorTicks)
						{
							logger.Debug("Cpu is idle, stopping process.");
							break;
						}

						lastTime = pi.TotalProcessorTicks;
						Thread.Sleep(pollInterval);
					}

					if (i >= WaitForExitTimeout)
						logger.Debug("Timed out waiting for cpu idle, stopping process.");
				}
				catch (Exception ex)
				{
					logger.Debug("Error querying cpu time: {0}", ex.Message);
				}

				_Stop();
			}
			else
			{
				logger.Debug("WaitForExit({0})", WaitForExitTimeout == -1 ? "INFINITE" : WaitForExitTimeout.ToString());

				if (!WaitForExit(_procCommand, WaitForExitTimeout))
				{
					logger.Trace("Process failed to exit before timeout expired");

					if (!useCpuKill)
					{
						logger.Debug("FAULT, WaitForExit ran out of time!");
						_fault = MakeFault("ProcessFailedToExit", "Process did not exit in " + WaitForExitTimeout + "ms");
						this.Agent.QueryMonitors("CanaKitRelay_Reset");
					}
				}
				else
				{
					logger.Trace("Finished waiting for process to exit");
				}
			}
		}

		static bool WaitForExit(Process p, int timeout)
		{
			// Process.WaitForExit doesn't work on processes
			// that were not started from within mono.
			// waitpid returns ECHILD

			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			while (!p.HasExited && timeout > 0 && sw.ElapsedMilliseconds < timeout)
				System.Threading.Thread.Sleep(10);

			return true;
		}

		bool _IsRunning()
		{
			return _procCommand != null && !_procCommand.HasExited;
		}

		Fault MakeFault(string folder, string reason)
		{
			var ret = new Fault
			{
				type = FaultType.Fault,
				detectionSource = "LinuxDebugger",
				title = reason,
				description = "{0}: {1} {2}".Fmt(reason, Executable, Arguments),
				folderName = folder,
			};

			ret.collectedData.Add(new Fault.Data("stdout.log", File.ReadAllBytes(Path.Combine(_tmpPath, "stdout.log"))));
			ret.collectedData.Add(new Fault.Data("stderr.log", File.ReadAllBytes(Path.Combine(_tmpPath, "stderr.log"))));

			return ret;
		}

		[DllImport("libc", CharSet = CharSet.Ansi, SetLastError = true)]
		static extern IntPtr mkdtemp(StringBuilder template);

		string MakeTempDir()
		{
			StringBuilder dir = new StringBuilder(Path.Combine(Path.GetTempPath(), "gdb.XXXXXX"));
			IntPtr ptr = mkdtemp(dir);
			if (ptr == IntPtr.Zero)
				throw new Win32Exception(Marshal.GetLastWin32Error());

			return dir.ToString();
		}

		public override void IterationStarting(uint iterationCount, bool isReproduction)
		{
			_fault = null;
			_messageExit = false;

			if (RestartOnEachTest)
				_Stop();

			if (!_IsRunning() && StartOnCall == null)
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

			_fault = new Fault();
			_fault.type = FaultType.Fault;
			_fault.detectionSource = "LinuxDebugger";

			var hash = reHash.Match(output);
			if (hash.Success)
			{
				_fault.majorHash = hash.Groups[1].Value;
				_fault.minorHash = hash.Groups[2].Value;
			}

			var exp = reClassification.Match(output);
			if (exp.Success)
				_fault.exploitability = exp.Groups[1].Value;

			var desc = reDescription.Match(output);
			if (desc.Success)
				_fault.title = desc.Groups[1].Value;

			var other = reOther.Match(output);
			if (other.Success)
				_fault.title += ", " + other.Groups[1].Value;

			_fault.collectedData.Add(new Fault.Data("StackTrace.txt", bytes));
			_fault.collectedData.Add(new Fault.Data("stdout.log", File.ReadAllBytes(Path.Combine(_tmpPath, "stdout.log"))));
			_fault.collectedData.Add(new Fault.Data("stderr.log", File.ReadAllBytes(Path.Combine(_tmpPath, "stderr.log"))));
			_fault.description = output;

			return true;
		}

		public override Fault GetMonitorData()
		{
			return _fault;
		}

		public override bool MustStop()
		{
			return false;
		}

		public override void StopMonitor()
		{
			_Stop();
		}

		public override void SessionStarting()
		{
			_tmpPath = MakeTempDir();
			_gdbCmd = Path.Combine(_tmpPath, "gdb.cmd");
			_gdbPid = Path.Combine(_tmpPath, "gdb.pid");
			_gdbLog = Path.Combine(_tmpPath, "gdb.log");

			string cmd = string.Format(template, _gdbLog, Executable, Arguments, _exploitable, _gdbPid);
			File.WriteAllText(_gdbCmd, cmd);

			logger.Debug("Wrote gdb commands to '{0}'", _gdbCmd);

			if (StartOnCall == null && !RestartOnEachTest)
				_Start();
		}

		public override void SessionFinished()
		{
			_Stop();

			Directory.Delete(_tmpPath, true);
		}

		public override bool IterationFinished()
		{
			if (!_messageExit && FaultOnEarlyExit && !_IsRunning())
			{
				_Stop(); // Stop 1st so stdout/stderr logs are closed
				_fault = MakeFault("ProcessExitedEarly", "Process exited early");
			}
			else if (StartOnCall != null)
			{
				_WaitForExit(true);
				_Stop();
			}
			else if (RestartOnEachTest)
			{
				_Stop();
			}

			return true;
		}

		public override Variant Message(string name, Variant data)
		{
			logger.Debug("Message(" + name + ", " + (string)data + ")");

			if (name == "Action.Call" && ((string)data) == StartOnCall)
			{
				_Stop();
				_Start();
			}
			else if (name == "Action.Call" && ((string)data) == WaitForExitOnCall)
			{
				_messageExit = true;
				_WaitForExit(false);
				_Stop();
			}
			else
			{
				logger.Debug("Unknown msg: " + name + " data: " + (string)data);
			}

			return null;
		}
	}
}
