using System;
using System.Diagnostics;
using System.IO;
using Peach.Core;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Core.Storage;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices
{
	public class JobMonitor : IDisposable
	{
		public Job Job
		{
			get
			{
				lock (this)
				{
					if (_job != null)
					{
						// refresh job status from JobDatabase
						// but only if we're currently running a job
						using (var db = new JobDatabase(_job.Guid))
						{
							_job = db.GetJob(_job.Guid);
						}
					}
					return _job;
				}
			}
		}

		Job _job;
		Process _process;
		Task _taskMonitor;
		Task _taskStderr;
		string _pitFile;
		string _pitLibraryPath;
		volatile bool _pendingKill;

		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
		public readonly static JobMonitor Instance = new JobMonitor();

		internal JobMonitor()
		{
		}

		enum Command
		{
			Stop,
			Pause,
			Continue,
		}

		public bool Pause()
		{
			return SendCommand(Command.Pause);
		}

		public bool Continue()
		{
			return SendCommand(Command.Continue);
		}

		public bool Stop()
		{
			return SendCommand(Command.Stop);
		}

		public bool Kill()
		{
			Logger.Trace("Kill");

			lock (this)
			{
				if (_job == null ||
					_process == null)
					return false;

				try
				{
					_pendingKill = true;
					_process.Kill();
					return true;
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
					return false;
				}
			}
		}

		// used by unit tests
		// kill the worker without setting _pendingKill
		// this should test Restarts
		internal void Terminate()
		{
			_process.Kill();
		}

		public Job Start(
			string pitLibraryPath,
			string pitFile,
			JobRequest jobRequest)
		{
			lock (this)
			{
				if (_job != null)
					return null;

				_pitFile = pitFile;
				_pitLibraryPath = pitLibraryPath;

				_job = new Job
				{
					Guid = Guid.NewGuid(),
					Name = Path.GetFileNameWithoutExtension(_pitFile),
					StartDate = DateTime.UtcNow,
					Status = JobStatus.StartPending,
					Mode = JobMode.Starting,

					// select only params that we need to start a job
					PitUrl = jobRequest.PitUrl,
					IsTest = jobRequest.IsTest,
					Seed = jobRequest.Seed,
					RangeStart = jobRequest.RangeStart,
					RangeStop = jobRequest.RangeStop,
				};

				using (var db = new JobDatabase(_job.Guid))
				{
					db.InsertJob(_job);
				}

				StartProcess();

				return _job;
			}
		}

		void StartProcess()
		{
			Logger.Trace("StartProcess");

			var fileName = Utilities.GetAppResourcePath("PeachWorker.exe");

			var args = new List<string>
			{
				"--pits", _pitLibraryPath,
				"--guid", _job.Id,
				_pitFile,
				//"-vv",
			};

			if (!string.IsNullOrEmpty(Configuration.LogRoot))
			{
				args.Add("--logRoot");
				args.Add(Configuration.LogRoot);
			}

			_process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = fileName,
					Arguments = string.Join(" ", args),
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					WorkingDirectory = Utilities.ExecutionDirectory,
				}
			};
			_process.Start();

			var path = Path.Combine(Configuration.LogRoot, _job.Guid.ToString(), "error.log");
			_taskStderr = new Task(LoggingTask, new LoggingTaskArgs
			{
				source = _process.StandardError,
				sink = new StreamWriter(path),
			});
			_taskStderr.Start();

			_taskMonitor = new Task(MonitorTask);
			_taskMonitor.Start();

			// wait for prompt
			_process.StandardOutput.ReadLine();
		}

		bool SendCommand(Command cmd)
		{
			Logger.Trace("SendCommand: {0}", cmd);

			lock (this)
			{
				if (_job == null)
					return false;

				try
				{
					_process.StandardInput.WriteLine(cmd.ToString().ToLower());
					_process.StandardOutput.ReadLine();
					return true;
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
					return false;
				}
			}
		}

		struct LoggingTaskArgs
		{
			public StreamReader source;
			public StreamWriter sink;
		}

		void LoggingTask(object obj)
		{
			var args = (LoggingTaskArgs)obj;
			try
			{
				using (args.source)
				using (args.sink)
				{
					while (!_process.HasExited)
					{
						var line = args.source.ReadLine();
						if (line == null)
							return;

						args.sink.WriteLine(line);
						args.sink.Flush();
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
		}

		void MonitorTask()
		{
			Logger.Trace("WaitForExit");

			_process.WaitForExit();
			var exitCode = _process.ExitCode;

			Logger.Trace("WaitForExit: {0}", exitCode);

			// this shouldn't throw, LoggingTask should catch it
			_taskStderr.Wait();

			Logger.Trace("LoggingTask done");

			_process.Dispose();
			_process = null;

			if (exitCode == 0 || _pendingKill)
			{
				FinishJob();
			}
			else
			{
				Thread.Sleep(TimeSpan.FromSeconds(1));

				StartProcess();
			}
		}

		void FinishJob()
		{
			lock (this)
			{
				// copy job from JobDatabase into NodeDatabase
				using (var db = new JobDatabase(_job.Guid))
				{
					_job = db.GetJob(_job.Guid);
				}

				using (var db = new NodeDatabase())
				{
					db.InsertJob(_job);
				}

				_job = null;
				_taskStderr = null;
				_pitFile = null;
				_pitLibraryPath = null;
				_pendingKill = false;
			}
		}

		// used by unit tests
		public void Dispose()
		{
			if (Kill())
			{
				Logger.Trace("Waiting for process to die");
				_taskMonitor.Wait(TimeSpan.FromSeconds(5));
			}
		}
	}
}
