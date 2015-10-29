using System;
using System.Collections.Generic;
using System.IO;
using SysProcess = System.Diagnostics.Process;
using System.Threading.Tasks;
using System.Threading;

namespace Peach.Core
{
	public abstract class Process : PlatformFactory<Process>
	{
		protected NLog.Logger _logger;
		protected SysProcess _process;
		protected int _pid = -1;
		protected bool _inferior = false;
		private Task _stdoutTask;
		private Task _stderrTask;

		protected Process(NLog.Logger logger)
		{
			_logger = logger;
		}

		protected abstract void Terminate();

		protected abstract void Kill();

		protected abstract string FileName(string executable);

		protected abstract string Arguments(string executable, string arguments);

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

		public void Start(
			string executable, 
			string arguments, 
			Dictionary<string, string> environment,
			string logDir)
		{
			if (IsRunning)
				throw new Exception("Process already started");

			var si = new System.Diagnostics.ProcessStartInfo {
				FileName = FileName(executable),
				Arguments = Arguments(executable, arguments),
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};

			if (environment != null)
				environment.ForEach(x => si.EnvironmentVariables[x.Key] = x.Value);

			_logger.Debug("Start(): start process");
			_process = SysProcess.Start(si);

			var prefix = "[{0}] {1}".Fmt(_process.Id, Path.GetFileName(executable));

			// Close stdin so all reads return zero
			_process.StandardInput.Close();

			_logger.Debug("Start(): start stdout task ({0})".Fmt(_process.Id));
			_stdoutTask = Task.Factory.StartNew(LoggerTask, new LoggerArgs { 
				Prefix = prefix + " out",
				Source = _process.StandardOutput,
				LogName = "stdout.log",
				LogDir = logDir,
			});

			_logger.Debug("Start(): start stderr task ({0})".Fmt(_process.Id));
			_stderrTask = Task.Factory.StartNew(LoggerTask, new LoggerArgs { 
				Prefix = prefix + " err",
				Source = _process.StandardError,
				LogName = "stderr.log",
				LogDir = logDir,
			});

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
					Terminate();
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
						Kill();
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
				Kill();
			else
				Terminate();
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

		class LoggerArgs
		{
			public string Prefix { get; set; }
			public StreamReader Source { get; set; }
			public string LogName { get; set; }
			public string LogDir { get; set; }
		}

		private void LoggerTask(object obj)
		{
			var args = (LoggerArgs)obj;

			StreamWriter writer = null;
			if (!string.IsNullOrEmpty(args.LogDir) && Directory.Exists(args.LogDir))
			{
				writer = new StreamWriter(Path.Combine(args.LogDir, args.LogName));
			}

			try
			{
				using (var reader = args.Source)
				{
					while (!reader.EndOfStream)
					{
						var line = reader.ReadLine();
						if (!string.IsNullOrEmpty(line) && _logger.IsDebugEnabled)
							_logger.Debug("{0}: {1}", args.Prefix, line);
						if (writer != null)
							writer.WriteLine(line);
					}
					_logger.Debug("{0}: EOF", args.Prefix);
				}
			}
			finally
			{
				if (writer != null)
				{
					writer.Close();
					writer.Dispose();
				}
			}
		}
	}
}
