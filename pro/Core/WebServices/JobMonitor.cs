using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Peach.Core;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Core.Storage;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices
{
	public interface IJobMonitor : IDisposable
	{
		Job GetJob();

		Job Start(string pitLibraryPath, string pitFile, JobRequest jobRequest);

		bool Pause();
		bool Continue();
		bool Stop();
		bool Kill();

		EventHandler InternalEvent { set; }
	}

	public abstract class BaseJobMonitor
	{
		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
		protected Guid? _guid;
		protected string _pitFile;
		protected string _pitLibraryPath;

		public EventHandler InternalEvent { get; set; }

		public Job GetJob()
		{
			lock (this)
			{
				if (!_guid.HasValue)
					return null;
			}

			return JobResolver.GetJob(_guid.Value);
		}

		public Job Start(string pitLibraryPath, string pitFile, JobRequest jobRequest)
		{
			lock (this)
			{
				if (IsRunning)
					return null;

				_pitFile = Path.GetFullPath(pitFile);
				_pitLibraryPath = pitLibraryPath;

				var job = new Job(jobRequest, _pitFile);
				_guid = job.Guid;

				OnStart(job);

				return job;
			}
		}

		protected abstract void OnStart(Job job);
		protected abstract bool IsRunning { get; }

		protected void JobFail(Exception ex)
		{
			Logger.Trace(">>> JobFail");

			var job = GetJob();
			if (job == null)
			{
				Logger.Trace("<<< JobFail (job == null)");
				return;
			}

			job.Status = JobStatus.Stopped;
			job.Result = ex.Message;

			using (var db = new NodeDatabase())
			{
				var events = db.GetTestEventsByJob(job.Guid).ToList();
				foreach (var testEvent in events)
				{
					if (testEvent.Status == TestStatus.Active)
					{
						testEvent.Status = TestStatus.Fail;
						testEvent.Resolve = ex.Message;
					}
				}

				db.UpdateTestEvents(events);
				db.UpdateJob(job);
			}

			if (File.Exists(job.DatabasePath))
			{
				using (var db = new JobDatabase(job.DatabasePath))
				{
					db.UpdateJob(job);
				}
			}

			Logger.Trace("<<< JobFail");
		}
	}

	public class InternalJobMonitor : BaseJobMonitor, IJobMonitor
	{
		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
		volatile JobRunner _runner;
		Thread _thread;

		protected override bool IsRunning { get { return _runner != null; } }

		public bool Pause()
		{
			lock (this)
			{
				if (!IsRunning)
					return false;
				_runner.Pause();
				return true;
			}
		}

		public bool Continue()
		{
			lock (this)
			{
				if (!IsRunning)
					return false;
				_runner.Continue();
				return true;
			}
		}

		public bool Stop()
		{
			lock (this)
			{
				if (!IsRunning)
					return false;
				_runner.Stop();
				return true;
			}
		}

		public bool Kill()
		{
			Logger.Trace(">>> Kill");

			lock (this)
			{
				if (!IsRunning)
				{
					Logger.Trace("<<< Kill (!IsRunning)");
					return false;
				}

				Logger.Trace("Abort");
				_thread.Abort();

				Logger.Trace("<<< Kill");
				return true;
			}
		}

		public void Dispose()
		{
			Logger.Trace(">>> Dispose");

			if (Kill())
			{
				Logger.Trace("Join");
				_thread.Join(TimeSpan.FromSeconds(5));
			}

			Logger.Trace("<<< Dispose");
		}

		protected override void OnStart(Job job)
		{
			_runner = new JobRunner(job, _pitLibraryPath, _pitFile);
			_thread = new Thread(() =>
			{
				try
				{
					_runner.Run();
				}
				catch (Exception ex)
				{
					JobFail(ex);
				}
				finally
				{
					lock (this)
					{
						_runner = null;
					}

					if (InternalEvent != null)
						InternalEvent(this, EventArgs.Empty);
				}
			});
			_thread.Start();
		}
	}

	public class ExternalJobMonitor : BaseJobMonitor, IJobMonitor
	{
		Process _process;
		Task _taskMonitor;
		Task _taskStderr;
		volatile bool _pendingKill;

		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		protected override bool IsRunning { get { return _process != null; } }

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
				if (!IsRunning)
					return true;

				try
				{
					_pendingKill = true;
					_process.Kill();
					return true;
				}
				catch (InvalidOperationException)
				{
					// The process has already been killed
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

		protected override void OnStart(Job job)
		{
			Logger.Trace("StartProcess");

			var fileName = Utilities.GetAppResourcePath("PeachWorker.exe");

			var args = new List<string>
			{
				"--pits", _pitLibraryPath,
				"--guid", job.Id,
				_pitFile,
			};

			if (!string.IsNullOrEmpty(Configuration.LogRoot))
			{
				args.Add("--logRoot");
				args.Add(Configuration.LogRoot);
			}

			//if (Configuration.LogLevel == NLog.LogLevel.Debug)
			//	args.Add("-v");
			//if (Configuration.LogLevel == NLog.LogLevel.Trace)
			//	args.Add("-vv");

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

			var path = Path.Combine(Configuration.LogRoot, job.Id, "error.log");
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			_taskStderr = Task.Factory.StartNew(LoggingTask, new LoggingTaskArgs
			{
				Source = _process.StandardError,
				Sink = new StreamWriter(path),
			});

			_taskMonitor = Task.Factory.StartNew(MonitorTask, job);

			// wait for prompt
			_process.StandardOutput.ReadLine();
		}

		bool SendCommand(Command cmd)
		{
			Logger.Trace("SendCommand: {0}", cmd);

			lock (this)
			{
				if (!IsRunning)
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
			public StreamReader Source;
			public StreamWriter Sink;
		}

		void LoggingTask(object obj)
		{
			var args = (LoggingTaskArgs)obj;
			try
			{
				using (args.Source)
				using (args.Sink)
				{
					while (!_process.HasExited)
					{
						var line = args.Source.ReadLine();
						if (line == null)
							return;

						Logger.Error(line);
						args.Sink.WriteLine(line);
						args.Sink.Flush();
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
		}

		void MonitorTask(object obj)
		{
			var job = (Job)obj;

			Logger.Trace("WaitForExit> pid: {0}", _process.Id);

			_process.WaitForExit();
			var exitCode = _process.ExitCode;

			Logger.Trace("WaitForExit> ExitCode: {0}", exitCode);

			// this shouldn't throw, LoggingTask should catch it
			_taskStderr.Wait();

			Logger.Trace("LoggingTask done");

			if (exitCode == 0 || exitCode == 2 || _pendingKill)
			{
				if (exitCode != 0 && _pendingKill)
					JobFail(new Exception("Job has been aborted."));
				FinishJob();
			}
			else
			{
				Thread.Sleep(TimeSpan.FromSeconds(1));

				lock (this)
				{
					_process.Dispose();
					_process = null;

					OnStart(job);
				}
			}
		}

		void FinishJob()
		{
			Logger.Trace(">>> FinishJob");

			lock (this)
			{
				try 
				{ 
					_process.Dispose(); 
				}
				catch(Exception ex) 
				{
 					Logger.Trace("Exception during _process.Dispose: {0}", ex.Message);
				}

				_process = null;
				_taskStderr = null;
				_pendingKill = false;
			}

			if (InternalEvent != null)
				InternalEvent(this, EventArgs.Empty);

			Logger.Trace("<<< FinishJob");
		}

		// used by unit tests
		public void Dispose()
		{
			Logger.Trace(">>> Dispose");

			if (Kill())
			{
				Logger.Trace("Waiting for process to die");
				_taskMonitor.Wait(TimeSpan.FromSeconds(5));
			}

			Logger.Trace("<<< Dispose");
		}
	}
}
