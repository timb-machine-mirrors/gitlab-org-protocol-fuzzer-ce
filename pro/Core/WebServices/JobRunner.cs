using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Peach.Core;
using Peach.Pro.Core.Loggers;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Core.Storage;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Peach.Pro.Core.WebServices
{
	public class JobRunner
	{
		public Job Job { get; private set; }

		enum Command
		{
			Stop,
			Pause,
			Continue,
			Kill,
		}

		static object _mutex = new object();
		static JobRunner _instance;

		//static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		Process _process;
		Task _stderr;
		readonly string _pitFile;
		readonly string _pitLibraryPath;
		readonly BlockingCollection<Command> _requestQueue = new BlockingCollection<Command>();

		public JobRunner(Job job, string pitFile, string pitLibraryPath)
		{
			Job = job;
			_pitFile = pitFile;
			_pitLibraryPath = pitLibraryPath;
		}

		public void Pause()
		{
			_requestQueue.Add(Command.Pause);
		}

		public void Continue()
		{
			_requestQueue.Add(Command.Continue);
		}

		public void Stop()
		{
			_requestQueue.Add(Command.Stop);
		}

		public void Kill()
		{
			_requestQueue.Add(Command.Kill);
		}

		public static JobRunner Run(
			string prefix, 
			string pitLibraryPath, 
			Pit pit, 
			Job jobRequest)
		{
			var pitFile = pit.Versions[0].Files[0].Name;

			var job = new Job
			{
				Id = Guid.NewGuid(),
				Name = Path.GetFileNameWithoutExtension(pitFile),
				Status = JobStatus.StartPending,
				StartDate = DateTime.UtcNow,
				Mode = JobMode.Fuzzing,

				// select only params that we need to start a job
				Seed = jobRequest.Seed,
				RangeStart = jobRequest.RangeStart,
				RangeStop = jobRequest.RangeStop,
			};

			var runner = new JobRunner(job, pitFile, pitLibraryPath);
			runner.Start();
			return runner;
		}

		public void Start()
		{
			using (var db = new NodeDatabase())
			{
				db.InsertJob(Job);
			}

			lock (_mutex)
			{
				_instance = this;
			}
		}

		void StartProcess()
		{
			var fileName = Utilities.GetAppResourcePath("PeachWorker.exe");

			var args = new[]
			{
				"--pits", _pitLibraryPath,
				"--guid", Job.Id.ToString(),
				_pitFile,
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

			_stderr = new Task(ProcessStdErr);
			_stderr.Start();

			// wait for prompt
			_process.StandardOutput.ReadLine();
		}

		void ProcessStdIn()
		{
			_process.StandardInput.WriteLine();
		}

		void ProcessStdOut()
		{
			var prompt = _process.StandardOutput.ReadLine();
		}

		void ProcessStdErr()
		{
			while (!_process.HasExited)
			{
				var line = _process.StandardError.ReadLine();
				Console.WriteLine(line);
			}
		}

		public static JobRunner Get(Guid id)
		{
			lock (_mutex)
			{
				//JobRunner ret;
				//_jobs.TryGetValue(id, out ret);
				//return ret;
				return null;
			}
		}

		//public static JobRunner Attach(Peach.Core.Dom.Dom dom, RunConfiguration config)
		//{
		//	var ret = new JobRunner()
		//	{
		//		Guid = System.Guid.NewGuid().ToString().ToLower(),
		//		Name = Path.GetFileNameWithoutExtension(config.pitFile),
		//		Seed = config.randomSeed,
		//		StartDate = config.runDateTime.ToUniversalTime(),
		//		Status = JobStatus.Running,
		//		PitUrl = string.Empty,
		//		HasMetrics = dom.tests
		//			.Where(t => t.Name == config.runName)
		//			.SelectMany(t => t.loggers).Any(l => l is MetricsLogger),
		//	};

		//	return ret;
		//}

		//bool shouldStop()
		//{
		//	// Called once per iteration.
		//	if (Status != JobStatus.Running)
		//	{
		//		try
		//		{
		//			_stopwatch.Stop();

		//			lock (this)
		//			{
		//				if (Status == JobStatus.StopPending)
		//					return true;

		//				Status = JobStatus.Paused;
		//			}

		//			// Will block the engine until the event is set by ResumeJob()
		//			pauseEvent.WaitOne();

		//			lock (this)
		//			{
		//				if (Status == JobStatus.StopPending)
		//					return true;

		//				Status = JobStatus.Running;
		//			}
		//		}
		//		finally
		//		{
		//			_stopwatch.Start();
		//		}
		//	}

		//	return false;
		//}

		//void ThreadProc(WebLogger webLogger, string pitLibraryPath, RunConfiguration config)
		//{
		//	try
		//	{
		//		var pitConfig = config.pitFile + ".config";
		//		var defs = PitDatabase.ParseConfig(pitLibraryPath, pitConfig);
		//		var args = new Dictionary<string, object>();

		//		args[Peach.Core.Analyzers.PitParser.DEFINED_VALUES] = defs;

		//		var parser = new Godel.Core.GodelPitParser();
		//		var dom = parser.asParser(args, config.pitFile);

		//		foreach (var test in dom.tests)
		//		{
		//			// If test has metrics logger, do nothing
		//			var metricsLogger = test.loggers.OfType<MetricsLogger>().FirstOrDefault();
		//			if (metricsLogger != null)
		//				continue;

		//			// If test does not have a file logger, do nothing
		//			var fileLogger = test.loggers.OfType<FileLogger>().FirstOrDefault();
		//			if (fileLogger == null)
		//				continue;

		//			// Add metrics logger with same path as file logger
		//			metricsLogger = new MetricsLogger(new Dictionary<string, Variant>
		//			{
		//				{ "Path", new Variant(fileLogger.Path) }
		//			});

		//			test.loggers.Add(metricsLogger);
		//		}

		//		var engine = new Engine(webLogger);


		//		// hook up the stop event
		//		config.shouldStop = shouldStop;

		//		lock (this)
		//		{
		//			// If we are still start pending, then go to running
		//			// We could be in StopPending, in which case we should just exit
		//			if (Status == JobStatus.StopPending)
		//				return;

		//			Status = JobStatus.Running;
		//		}

		//		engine.startFuzzing(dom, config);
		//	}
		//	catch (Exception ex)
		//	{
		//		logger.Debug("Unhandled exception when running job:\n{0}", ex);
		//		Result = ex.Message;
		//	}
		//	finally
		//	{
		//		lock (this)
		//		{
		//			_stopwatch.Stop();

		//			pauseEvent.Dispose();
		//			pauseEvent = null;
		//			thread = null;

		//			Status = JobStatus.Stopped;
		//			StopDate = DateTime.UtcNow;
		//		}
		//	}
		//}
	}
}
