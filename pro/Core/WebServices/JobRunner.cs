using Peach.Core;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Peach.Enterprise.WebServices
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

		public static JobRunner Run(WebLogger webLogger, string pitLibraryPath, string pitFile, string pitUrl, uint seed, uint rangeStart, uint rangeStop)
		{
			var config = new RunConfiguration()
			{ 
				pitFile = pitFile,
			};

			if (seed > 0)
				config.randomSeed = seed;

			if (rangeStart > 0)
			{
				config.range = true;
				config.rangeStart = rangeStart;
			}

			if(rangeStop > 0)
			{
				config.range = true;
				config.rangeStop = rangeStop;
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

		public static JobRunner Attach(RunConfiguration config)
		{
			//TODO: fix HasMetrics
			var ret = new JobRunner()
			{
				Guid = System.Guid.NewGuid().ToString().ToLower(),
				Name = Path.GetFileNameWithoutExtension(config.pitFile),
				Seed = config.randomSeed,
				StartDate = config.runDateTime.ToUniversalTime(),
				Status = JobStatus.Running,
				PitUrl = string.Empty,
				HasMetrics = false
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


				#region add MetricsLogger to all tests if it doesn't exist
				var metricsargs = new Dictionary<string, Peach.Core.Variant>();
				metricsargs.Add("Path", new Peach.Core.Variant("jobtmp/" + this.Guid));
				var metricslogger = new Loggers.MetricsLogger(metricsargs);

				foreach (var test in dom.tests)
				{
					bool found = false;
					foreach (var logger in test.loggers)
					{
						if (logger.GetType() == typeof(Loggers.MetricsLogger))
						{
							found = true;
							break;
						}
					}

					if (!found)
					{
						test.loggers.Add(metricslogger);
					}
				}
				#endregion


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
