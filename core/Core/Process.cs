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

			var prefix = "{0} ({1})".Fmt(Path.GetFileName(executable), _process.Id);

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
		}

		public void Stop()
		{
			if (!IsRunning)
				return;

			_logger.Debug("Stop(): SIGTERM", _process.Id);

			Terminate();

			_logger.Debug("Stop(): WaitForExit(1000) after SIGTERM");
			if (!_process.WaitForExit(1000))
			{
				_logger.Debug("Stop(): SIGKILL", _process.Id);

				Kill();

				_logger.Debug("Stop(): WaitForExit(1000) after SIGKILL");
				if (!_process.WaitForExit(1000))
					_logger.Warn("Stop(): WaitForExit timeout");
			}

			_logger.Debug("Stop(): Closing process");
			_process.Close();
			_process = null;

			_logger.Debug("Stop(): Wait for stdout to finish");
			try
			{
				_stdoutTask.Wait(5000);
			}
			catch (AggregateException ex)
			{
				_logger.Warn("Exception in stdout task: {0}", ex.InnerException.Message);
			}

			_logger.Debug("Stop(): Wait for stderr to finish");
			try
			{
				_stderrTask.Wait(5000);
			}
			catch (AggregateException ex)
			{
				_logger.Warn("Exception in stderr task: {0}", ex.InnerException.Message);
			}

			_logger.Debug("Stop(): Complete");
		}

		public bool WaitForExit(int timeout, bool useCpuKill)
		{
			if (!IsRunning)
				return true;

			if (useCpuKill)
			{
				const int pollInterval = 200;
				ulong lastTime = 0;

				try
				{
					int i;

					for (i = 0; i < timeout; i += pollInterval)
					{
						var pi = ProcessInfo.Instance.Snapshot(_process);

						_logger.Trace("WaitForExit: CpuKill: OldTicks={0} NewTicks={1}", lastTime, pi.TotalProcessorTicks);

						if (i != 0 && lastTime == pi.TotalProcessorTicks)
						{
							_logger.Debug("WaitForExit: Cpu is idle, stopping process.");
							break;
						}

						lastTime = pi.TotalProcessorTicks;
						Thread.Sleep(pollInterval);
					}

					if (i >= timeout)
						_logger.Debug("WaitForExit: Timed out waiting for cpu idle, stopping process.");
				}
				catch (Exception ex)
				{
					_logger.Debug("WaitForExit: Error querying cpu time: {0}", ex.Message);
				}

				Stop();
			}
			else
			{
				_logger.Debug("WaitForExit({0})", timeout);
				if (!_process.WaitForExit(timeout) && !useCpuKill)
				{
					_logger.Debug("FAULT, WaitForExit ran out of time!");
					return false;
				}
			}

			return true;
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
