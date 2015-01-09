using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Peach.Core;
using Peach.Pro.Core.Loggers;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core.WebServices
{
	public class JobRunner
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		Stopwatch stopwatch;
		ManualResetEvent pauseEvent;
		Thread thread;

		private JobRunner()
		{
		}

		public string PitUrl { get; private set; }
		public string Guid { get; private set; }
		public string Name { get; private set; }
		public uint Seed { get; private set; }
		public DateTime StartDate { get; private set; }
		public DateTime StopDate { get; private set; }
		public JobStatus Status { get; private set; }
		public bool Range { get; private set; }
		public uint RangeStart { get; private set; }
		public uint RangeStop { get; private set; }
		public bool HasMetrics { get; private set; }
		public string Result { get; private set; }

		public TimeSpan Runtime
		{
			get
			{
				if (stopwatch != null)
					return stopwatch.Elapsed;

				return DateTime.UtcNow - StartDate;
			}
		}

		public bool Pause()
		{
			lock (this)
			{
				if (thread != null && Status == JobStatus.Running)
				{
					Status = JobStatus.PausePending;
					pauseEvent.Reset();
					return true;
				}

				return false;
			}
		}

		public bool Continue()
		{
			lock (this)
			{
				if (thread != null && Status == JobStatus.Paused)
				{
					Status = JobStatus.ContinuePending;
					pauseEvent.Set();
					return true;
				}

				return false;
			}
		}

		public bool Stop()
		{
			lock (this)
			{
				if (thread != null && Status != JobStatus.StopPending && Status != JobStatus.Stopped)
				{
					Status = JobStatus.StopPending;
					pauseEvent.Set();
					return true;
				}

				return false;
			}
		}

		public bool Kill()
		{
			Thread th;

			lock (this)
			{
				if (thread == null)
					return false;

				th = thread;
				th.Abort();
			}

			th.Join();

			return true;
		}

		public static JobRunner Run(
			WebLogger webLogger,
			string pitLibraryPath,
			string pitFile,
			string pitUrl,
			uint? seed,
			uint rangeStart,
			uint rangeStop)
		{
			var config = new RunConfiguration()
			{
				pitFile = pitFile,
			};

			if (seed.HasValue)
				config.randomSeed = seed.Value;

			if (rangeStop > 0)
			{
				config.range = true;
				config.rangeStart = rangeStart;
				config.rangeStop = rangeStop;
			}
			else if (rangeStart > 0)
			{
				config.skipToIteration = rangeStart;
			}

			var ret = new JobRunner()
			{
				Guid = System.Guid.NewGuid().ToString().ToLower(),
				Name = Path.GetFileNameWithoutExtension(pitFile),
				Seed = config.randomSeed,
				StartDate = config.runDateTime.ToUniversalTime(),
				Status = JobStatus.StartPending,
				PitUrl = pitUrl,
				Range = config.range,
				RangeStart = config.rangeStart,
				RangeStop = config.rangeStop,
				HasMetrics = true
			};

			ret.stopwatch = new Stopwatch();
			ret.pauseEvent = new ManualResetEvent(true);
			ret.thread = new Thread(delegate() { ret.ThreadProc(webLogger, pitLibraryPath, config); });

			ret.stopwatch.Start();
			ret.thread.Start();

			return ret;
		}

		public static JobRunner Attach(Peach.Core.Dom.Dom dom, RunConfiguration config)
		{
			var ret = new JobRunner()
			{
				Guid = System.Guid.NewGuid().ToString().ToLower(),
				Name = Path.GetFileNameWithoutExtension(config.pitFile),
				Seed = config.randomSeed,
				StartDate = config.runDateTime.ToUniversalTime(),
				Status = JobStatus.Running,
				PitUrl = string.Empty,
				HasMetrics = dom.tests
					.Where(t => t.name == config.runName)
					.SelectMany(t => t.loggers)
					.Where(l => l is MetricsLogger)
					.Any(),
			};

			return ret;
		}

		bool shouldStop()
		{
			// Called once per iteration.
			if (Status != JobStatus.Running)
			{
				try
				{
					stopwatch.Stop();

					lock (this)
					{
						if (Status == JobStatus.StopPending)
							return true;

						Status = JobStatus.Paused;
					}

					// Will block the engine until the event is set by ResumeJob()
					pauseEvent.WaitOne();

					lock (this)
					{
						if (Status == JobStatus.StopPending)
							return true;

						Status = JobStatus.Running;
					}
				}
				finally
				{
					stopwatch.Start();
				}
			}

			return false;
		}

		void ThreadProc(WebLogger webLogger, string pitLibraryPath, RunConfiguration config)
		{
			try
			{
				var pitConfig = config.pitFile + ".config";
				var defs = PitDatabase.ParseConfig(pitLibraryPath, pitConfig);
				var args = new Dictionary<string, object>();

				args[Peach.Core.Analyzers.PitParser.DEFINED_VALUES] = defs;

				var parser = new Godel.Core.GodelPitParser();
				var dom = parser.asParser(args, config.pitFile);

				foreach (var test in dom.tests)
				{
					// If test has metrics logger, do nothing
					var metricsLogger = test.loggers.OfType<MetricsLogger>().FirstOrDefault();
					if (metricsLogger != null)
						continue;

					// If test does not have a file logger, do nothing
					var fileLogger = test.loggers.OfType<FileLogger>().FirstOrDefault();
					if (fileLogger == null)
						continue;

					// Add metrics logger with same path as file logger
					metricsLogger = new MetricsLogger(new Dictionary<string, Variant>
					{
						{ "Path", new Variant(fileLogger.Path) }
					});

					test.loggers.Add(metricsLogger);
				}

				var engine = new Engine(webLogger);


				// hook up the stop event
				config.shouldStop = shouldStop;

				lock (this)
				{
					// If we are still start pending, then go to running
					// We could be in StopPending, in which case we should just exit
					if (Status == JobStatus.StopPending)
						return;

					Status = JobStatus.Running;
				}

				engine.startFuzzing(dom, config);
			}
			catch (Exception ex)
			{
				logger.Debug("Unhandled exception when running job:\n{0}", ex);
				Result = ex.Message;
			}
			finally
			{
				lock (this)
				{
					stopwatch.Stop();

					pauseEvent.Dispose();
					pauseEvent = null;
					thread = null;

					Status = JobStatus.Stopped;
					StopDate = DateTime.UtcNow;
				}
			}
		}
	}
}
