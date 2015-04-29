using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Pro.Core.Loggers;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;
using Godel.Core;
using Peach.Pro.Core.WebServices;
using NLog;

namespace Peach.Pro.Core.Runtime
{
	class JobRunner : Watcher
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		readonly JobLogger _jobLogger = new JobLogger();
		readonly ManualResetEvent _pausedEvt = new ManualResetEvent(true);
		readonly RunConfiguration _config;
		readonly string _pitLibraryPath;
		bool _shouldStop;

		const string DefinedValues = "DefinedValues";

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

			if (job.IsControlIteration)
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

		public void Run()
		{
			try
			{
				_jobLogger.Initialize(_config);

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

				var engine = new Engine(_jobLogger);

				engine.startFuzzing(dom, _config);
			}
			catch (ApplicationException ex) // PeachException or SoftException
			{
				if (Configuration.LogLevel == LogLevel.Trace)
					Logger.ErrorException("Exception: {0}", ex);
				else
					Logger.Error("Exception: {0}", ex.Message);
				_jobLogger.JobFail(_config.id, ex);
			}
			catch (Exception ex)
			{
				Logger.Error("Unhandled Exception: {0}", ex);
				_jobLogger.JobFail(_config.id, ex);
				throw;
			}
			finally
			{
				LogManager.Flush();
			}
		}

		private bool ShouldStop()
		{
			if (!_pausedEvt.WaitOne(0))
			{
				_jobLogger.UpdateStatus(JobStatus.Paused);

				_pausedEvt.WaitOne();

				_jobLogger.UpdateStatus(JobStatus.Running);
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
							"Loading pit config", "Loading configuration file '{0}'".Fmt(pitConfig));

						var defs = PitDatabase.ParseConfig(_pitLibraryPath, pitConfig);
						args[DefinedValues] = defs;

						_jobLogger.EventSuccess(db);
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
				var defs = PitDatabase.ParseConfig(_pitLibraryPath, pitConfig);
				args[DefinedValues] = defs;
			}

			return args;
		}

		public Peach.Core.Dom.Dom ParsePit()
		{
			var args = ParseConfig();
			var parser = new GodelPitParser();

			using (var db = new NodeDatabase())
			{
				_jobLogger.AddEvent(db, 
					_config.id,
					"Loading pit file", "Loading pit file '{0}'".Fmt(_config.pitFile));

				try
				{
					var dom = parser.asParser(args, _config.pitFile);
					_jobLogger.EventSuccess(db);
					return dom;
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
