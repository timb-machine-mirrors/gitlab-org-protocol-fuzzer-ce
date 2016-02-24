using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Pro.Core.Godel;
using Peach.Pro.Core.Loggers;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using Logger = NLog.Logger;

namespace Peach.Pro.Core.Runtime
{
	class JobRunner : Watcher
	{
		static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		readonly JobLogger _jobLogger = new JobLogger();
		readonly ManualResetEvent _pausedEvt = new ManualResetEvent(true);
		readonly RunConfiguration _config;
		readonly string _pitLibraryPath;
		bool _shouldStop;
		Engine _engine;
		Thread _currentThread;

		public JobRunner(Job job, string pitLibraryPath, string pitFile)
		{
			_pitLibraryPath = pitLibraryPath;

			_config = new RunConfiguration
			{
				id = job.Guid,
				pitFile = pitFile,
				shouldStop = ShouldStop,
			};

			if (job.Seed.HasValue)
				_config.randomSeed = (uint)job.Seed.Value;

			if (job.Duration.HasValue)
			{
				// This duration needs to take into account total duration and runtime
				_config.Duration = job.Duration.Value - job.Runtime;

				if (_config.Duration < TimeSpan.Zero)
					_config.Duration = TimeSpan.Zero;
			}

			if (job.DryRun)
			{
				_config.singleIteration = true;
			}
			else if (job.RangeStop.HasValue)
			{
				_config.range = true;
				_config.rangeStart = (uint)job.RangeStart + (uint)job.IterationCount;
				_config.rangeStop = (uint)job.RangeStop.Value;
			}
			else
			{
				_config.skipToIteration = (uint)job.RangeStart + (uint)job.IterationCount;
			}
		}

		public void Run(EventWaitHandle evtReady)
		{
			try
			{
				_currentThread = Thread.CurrentThread;

				_jobLogger.Initialize(_config);

				evtReady.Set();

				var dom = ParsePit();

				Test test;

				if (!dom.tests.TryGetValue(_config.runName, out test))
					throw new PeachException("Unable to locate test named '{0}'.".Fmt(_config.runName));

				var userLogger = test.loggers.OfType<JobLogger>().FirstOrDefault();
				if (userLogger != null)
				{
					_jobLogger.BasePath = userLogger.BasePath;
					test.loggers.Remove(userLogger);
				}

				_engine = new Engine(_jobLogger);
				_engine.startFuzzing(dom, _config);
			}
			catch (ApplicationException ex) // PeachException or SoftException
			{
				if (Configuration.LogLevel == LogLevel.Trace)
					Logger.Error("Exception: {0}".Fmt(ex));
				else
					Logger.Error("Exception: {0}", ex.Message);
				_jobLogger.JobFail(_config.id, ex.Message);
			}
			catch (Exception ex)
			{
				if (ex.GetBaseException() is ThreadAbortException)
				{
					Thread.ResetAbort();
					Logger.Trace("Thread aborted");
					_jobLogger.JobFail(_config.id, "Job killed.");
				}
				else
				{
					Logger.Error("Unhandled Exception: {0}".Fmt(ex));
					_jobLogger.JobFail(_config.id, ex.Message);
					throw;
				}
			}
			finally
			{
				Logger.Debug("Flushing Logs");
				LogManager.Flush();
				_jobLogger.RestoreLogging(_config.id);
			}
		}

		private bool ShouldStop()
		{
			if (!_pausedEvt.WaitOne(0))
			{
				_jobLogger.Pause();

				_pausedEvt.WaitOne();

				_jobLogger.Continue();
			}
			return _shouldStop;
		}

		public void Pause()
		{
			_pausedEvt.Reset();
		}

		public void Continue()
		{
			_pausedEvt.Set();
		}

		public void Stop()
		{
			_shouldStop = true;
			_pausedEvt.Set();
		}

		public void Abort()
		{
			Logger.Trace(">>> Abort");
			if (_engine != null)
			{
				_engine.Abort();
			}
			else
			{
				// this happens if pit parsing hangs...
				_currentThread.Abort();
				_currentThread.Join();
			}
			Logger.Trace("<<< Abort");
		}

		Dictionary<string, object> ParseConfig()
		{
			var args = new Dictionary<string, object>();
			var pitConfig = _config.pitFile + ".config";

			// It is ok if a .config doesn't exist
			if (File.Exists(pitConfig))
			{
				using (var db = new NodeDatabase())
				{
					try
					{
						_jobLogger.AddEvent(db,
							_config.id,
							"Loading pit config", "Loading configuration file '{0}'".Fmt(pitConfig),
							CompleteTestEvents.Last);

						var defs = PitDefines.ParseFile(pitConfig, _pitLibraryPath).Evaluate();
						args[PitParser.DEFINED_VALUES] = defs;
					}
					catch (Exception ex)
					{
						_jobLogger.EventFail(db, ex.Message);
						throw;
					}
				}
			}
			else
			{
				// ParseConfig allows non-existant config files
				var defs = PitDefines.ParseFile(pitConfig, _pitLibraryPath).Evaluate();
				args[PitParser.DEFINED_VALUES] = defs;
			}

			return args;
		}

		public Peach.Core.Dom.Dom ParsePit()
		{
			var args = ParseConfig();
			var parser = new ProPitParser();

			using (var db = new NodeDatabase())
			{
				_jobLogger.AddEvent(db,
					_config.id,
					"Loading pit file", "Loading pit file '{0}'".Fmt(_config.pitFile),
					CompleteTestEvents.Last);

				try
				{
					return parser.asParser(args, _config.pitFile);
				}
				catch (Exception ex)
				{
					_jobLogger.EventFail(db, ex.Message);
					throw;
				}
			}
		}
	}
}
