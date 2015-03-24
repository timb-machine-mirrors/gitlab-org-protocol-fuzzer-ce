using System;
using System.Diagnostics;
using System.IO;
using Peach.Core;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Core.Storage;
using System.Threading.Tasks;
using System.Threading;

namespace Peach.Pro.Core.WebServices
{
	public class JobRunner : IDisposable
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
		public readonly static JobRunner Instance = new JobRunner();

		internal JobRunner()
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
				if (_job == null)
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

		public bool Start(
			string pitLibraryPath,
			string pitFile,
			Job jobRequest)
		{
			lock (this)
			{
				if (_job != null)
					return false;

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
					Seed = jobRequest.Seed,
					RangeStart = jobRequest.RangeStart,
					RangeStop = jobRequest.RangeStop,
				};

				using (var db = new JobDatabase(_job.Guid))
				{
					db.InsertJob(_job);
				}

				StartProcess();

				return true;
			}
		}

		void StartProcess()
		{
			Logger.Trace("StartProcess");

			var fileName = Utilities.GetAppResourcePath("PeachWorker.exe");

			var args = new[]
			{
				"--pits", _pitLibraryPath,
				"--guid", _job.Id,
				_pitFile,
				//"-vv",
			};

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

			_taskStderr = new Task(LoggingTask, _process.StandardError);
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

		void LoggingTask(object obj)
		{
			var stream = (StreamReader)obj;
			try
			{
				while (!_process.HasExited)
				{
					var line = stream.ReadLine();
					if (line == null)
						return;

					// TODO: do something better with this
					Console.WriteLine(line);
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
