using System;
using System.Collections.Generic;
using System.IO;
using SysProcess = System.Diagnostics.Process;
using System.Threading.Tasks;
using System.Threading;
using System.Text;

namespace Peach.Core
{
	public struct ProcessRunResult
	{
		public bool Timeout;
		public int ExitCode;
		public StringBuilder StdErr;
		public StringBuilder StdOut;
	}
	
	public abstract class Process : PlatformFactory<Process>
	{
		protected NLog.Logger _logger;
		protected SysProcess _process;
		protected int _pid = -1;
		protected bool _inferior;
		private Task _stdoutTask;
		private Task _stderrTask;

		protected Process(NLog.Logger logger)
		{
			_logger = logger;
		}

		protected abstract void WaitForProcessGroup(SysProcess process);

		protected abstract void Terminate(SysProcess process);

		protected abstract void Kill(SysProcess process);

		protected virtual SysProcess CreateProcess(
			string executable, 
			string arguments,
			string workingDirectory,
			Dictionary<string, string> environment)
		{
			var si = new System.Diagnostics.ProcessStartInfo
			{
				FileName = executable,
				Arguments = arguments,
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
				CreateNoWindow = true,
				WorkingDirectory = workingDirectory ?? "",
			};

			if (environment != null)
				environment.ForEach(x => si.EnvironmentVariables[x.Key] = x.Value);

			_logger.Debug("Start(): \"{0} {1}\"", executable, arguments);
			return SysProcess.Start(si);
		}

		public bool IsRunning
		{ 
			get { return _process != null && !_process.HasExited; } 
		}

		public void Attach(int pid)
		{
			if (IsRunning)
				throw new Exception("Process already started");

			_process = SysProcess.GetProcessById(pid);
			_pid = pid;
			_inferior = true;
		}

		public static ProcessRunResult Run(
			NLog.Logger logger,
			string executable, 
			string arguments, 
			Dictionary<string, string> environment, 
			string workingDirectory,
			int timeout)
		{
			var process = CreateInstance(logger);
			return process._Run(executable, arguments, environment, workingDirectory, timeout);
		}

		ProcessRunResult _Run(
			string executable, 
			string arguments, 
			Dictionary<string, string> environment, 
			string workingDirectory,
			int timeout)
		{
			_logger.Debug("Run(): \"{0} {1}\"", executable, arguments);
			using (var process = CreateProcess(executable, arguments, workingDirectory, environment))
			{
				var prefix = "[{0}] {1}".Fmt(process.Id, Path.GetFileName(executable));
				var stdout = new StringWriter();
				var stderr = new StringWriter();

				// Close stdin so all reads return zero
				process.StandardInput.Close();

				_logger.Trace("[{0}] Run(): start stdout task".Fmt(process.Id));
				var stdoutTask = Task.Factory.StartNew(LoggerTask, new LoggerArgs { 
					Prefix = prefix + " out",
					Source = process.StandardOutput,
					Sink = stdout,
				}, TaskCreationOptions.LongRunning);

				_logger.Trace("[{0}] Run(): start stderr task".Fmt(process.Id));
				var stderrTask = Task.Factory.StartNew(LoggerTask, new LoggerArgs { 
					Prefix = prefix + " err",
					Source = process.StandardError,
					Sink = stderr,
				}, TaskCreationOptions.LongRunning);

				bool clean = false;
				try
				{
					clean = process.WaitForExit(timeout);
				}
				catch (Exception ex)
				{
					_logger.Warn("[{0}] Run(): Exception in WaitForExit(): {1}", process.Id, ex.Message);
				}

				try
				{
					if (!clean)
					{
						// we could get here too fast, before the trampoline has finished
						// so wait until the pgid matches the pid
						WaitForProcessGroup(process);
						Kill(process);
						process.WaitForExit(timeout);
					}
				}
				catch (Exception ex)
				{
					_logger.Warn("[{0}] Run(): Exception in Kill(): {1}", process.Id, ex.Message);
				}

				try
				{
					if (!Task.WaitAll(new Task[] { stdoutTask, stderrTask }, timeout))
						clean = false;
				}
				catch (AggregateException ex)
				{
					clean = false;
					_logger.Warn("[{0}] Run(): Exception in stdout/stderr task: {1}", process.Id, ex.InnerException.Message);
				}
				catch (Exception ex)
				{
					clean = false;
					_logger.Warn("[{0}] Run(): Exception in stdout/stderr task: {1}", process.Id, ex.Message);
				}

				var result = new ProcessRunResult {
					Timeout = !clean,
					ExitCode = clean ? process.ExitCode : -1,
					StdOut = stdout.GetStringBuilder(),
					StdErr = stderr.GetStringBuilder(),
				};

				process.Close();

				return result;
			}
		}

		public void Start(
			string executable, 
			string arguments, 
			Dictionary<string, string> environment,
			string logDir)
		{
			if (IsRunning)
				throw new Exception("Process already started");

			_process = CreateProcess(executable, arguments, null, environment);

			var prefix = "[{0}] {1}".Fmt(_process.Id, Path.GetFileName(executable));

			TextWriter stdout = null;
			TextWriter stderr = null;
			if (!string.IsNullOrEmpty(logDir) && Directory.Exists(logDir))
			{
				stdout = new StreamWriter(Path.Combine(logDir, "stdout.log"));
				stderr = new StreamWriter(Path.Combine(logDir, "stderr.log"));
			}

			// Close stdin so all reads return zero
			_process.StandardInput.Close();

			_logger.Trace("[{0}] Start(): start stdout task".Fmt(_process.Id));
			_stdoutTask = Task.Factory.StartNew(LoggerTask, new LoggerArgs { 
				Prefix = prefix + " out",
				Source = _process.StandardOutput,
				Sink = stdout,
			}, TaskCreationOptions.LongRunning);

			_logger.Trace("[{0}] Start(): start stderr task".Fmt(_process.Id));
			_stderrTask = Task.Factory.StartNew(LoggerTask, new LoggerArgs { 
				Prefix = prefix + " err",
				Source = _process.StandardError,
				Sink = stderr,
			}, TaskCreationOptions.LongRunning);

			Thread.Sleep(100);
			if (!IsRunning)
			{
				_logger.Debug("Process exited early with ExitCode: {0}", _process.ExitCode);
				if (_process.ExitCode != 0)
					throw new Exception("Process failed to start. ExitCode: {0}".Fmt(_process.ExitCode));
			}

			_pid = _process.Id;
			_inferior = false;
		}

		public void Stop(int timeout)
		{
			if (IsRunning)
			{
				_logger.Debug("[{0}] Stop(): SIGTERM", _pid);
				try
				{
					Terminate(_process);
				}
				catch (Exception ex)
				{
					_logger.Warn("[{0}] Stop(): Exception sending SIGTERM: {1}", _pid, ex.InnerException.Message);
				}
			}

			if (_stdoutTask != null)
			{
				_logger.Debug("[{0}] Stop(): Wait for stdout/stderr to finish", _pid);
				bool clean = false;
				try
				{
					clean = Task.WaitAll(new Task[] { _stdoutTask, _stderrTask }, timeout);
				}
				catch (AggregateException ex)
				{
					_logger.Warn("[{0}] Stop(): Exception in stdout/stderr task: {1}", _pid, ex.InnerException.Message);
				}

				if (!clean)
				{
					_logger.Debug("[{0}] Stop(): SIGKILL", _pid);
					try
					{
						Kill(_process);
					}
					catch (Exception ex)
					{
						_logger.Warn("[{0}] Stop(): Exception sending SIGKILL: {1}", _pid, ex.InnerException.Message);
					}
				}

				_stdoutTask = null;
				_stderrTask = null;
			}

			if (_process != null)
			{
				_logger.Debug("[{0}] Stop(): WaitForExit({1})", _pid, timeout);
				_process.WaitForExit(timeout);
			}

			Close();
		}

		public void Close()
		{
			if (_process != null)
			{
				_logger.Debug("[{0}] Close(): Closing process", _pid);
				_process.Close();
				_process = null;
				_logger.Debug("[{0}] Close(): Complete", _pid);
			}

			_pid = -1;
		}

		public void Shutdown(bool force)
		{
			if (!IsRunning)
				return;

			if (force)
				Kill(_process);
			else
				Terminate(_process);
		}

		public bool WaitForExit(int timeout)
		{
			var exited = true;

			if (IsRunning)
			{
				_logger.Debug("[{0}] WaitForExit({1})", _pid, timeout);
				if (_inferior)
					exited = InferiorWaitForExit(timeout);
				else
					exited = _process.WaitForExit(timeout);
			}

			Stop(timeout);

			return exited;
		}

		bool InferiorWaitForExit(int timeout)
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();

			while (!_process.HasExited && timeout > 0 && sw.ElapsedMilliseconds < timeout)
				Thread.Sleep(10);

			return _process.HasExited;
		}

		public void WaitForIdle(int timeout)
		{
			if (IsRunning)
			{
				const int pollInterval = 200;
				ulong lastTime = 0;

				try
				{
					int i;

					for (i = 0; i < timeout; i += pollInterval)
					{
						var pi = ProcessInfo.Instance.Snapshot(_process);

						_logger.Trace("[{0}] WaitForIdle(): OldTicks={1} NewTicks={2}", _pid, lastTime, pi.TotalProcessorTicks);

						if (i != 0 && lastTime == pi.TotalProcessorTicks)
						{
							_logger.Debug("[{0}] WaitForIdle(): Cpu is idle, stopping process.", _pid);
							break;
						}

						lastTime = pi.TotalProcessorTicks;
						Thread.Sleep(pollInterval);
					}

					if (i >= timeout)
						_logger.Debug("[{0}] WaitForIdle(): Timed out waiting for cpu idle, stopping process.", _pid);
				}
				catch (Exception ex)
				{
					if (IsRunning)
						_logger.Debug("[{0}] WaitForIdle(): Error querying cpu time: {1}", _pid, ex.Message);
				}
			}

			Stop(timeout);
		}

		struct LoggerArgs
		{
			public string Prefix;
			public StreamReader Source;
			public TextWriter Sink;
		}

		private void LoggerTask(object obj)
		{
			var args = (LoggerArgs)obj;

			try
			{
				using (var reader = args.Source)
				{
					while (!reader.EndOfStream)
					{
						var line = reader.ReadLine();
						if (!string.IsNullOrEmpty(line) && _logger.IsDebugEnabled)
							_logger.Debug("{0}: {1}", args.Prefix, line);
						if (args.Sink != null)
							args.Sink.WriteLine(line);
					}
					_logger.Debug("{0}: EOF", args.Prefix);
				}
			}
			finally
			{
				if (args.Sink != null)
				{
					args.Sink.Close();
					args.Sink.Dispose();
				}
			}
		}
	}
}
