using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;
using Encoding = System.Text.Encoding;
using Peach.Core.Agent;
using Godel.Core;
using Peach.Pro.Core.WebServices;
using System.Threading.Tasks;
using NLog.Config;
using NLog.Targets;
using NLog;
using NLog.Targets.Wrappers;

namespace Peach.Pro.Core.Runtime
{
	class JobRunner : Watcher
	{
		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		readonly Job _job;
		readonly string _logPath;
		readonly MetricsCache _cache;
		readonly List<TestEvent> _events = new List<TestEvent>();
		readonly List<Fault.State> _states = new List<Fault.State>();
		readonly ManualResetEvent _pausedEvt = new ManualResetEvent(true);
		readonly RunConfiguration _config;
		readonly string _pitLibraryPath;
		Fault _reproFault;
		bool _shouldStop;
		int _agentConnect;

		static string DEFINED_VALUES = "DefinedValues";

		public JobRunner(Job job, RunConfiguration config, string pitLibraryPath)
		{
			_job = job;
			_logPath = JobDatabase.GetStorageDirectory(_job.Guid);
			_cache = new MetricsCache(() => new JobDatabase(_job.Guid));
			_config = config;
			_config.shouldStop = ShouldStop;
			_pitLibraryPath = pitLibraryPath;
		}

		// Filter these loggers to the info level since they are spammy at debug
		private static string[] FilteredLoggers =
		{
			"Peach.Core.Dom.Array",
			"Peach.Core.Dom.Choice",
			"Peach.Core.Dom.DataElement",
			"Peach.Core.Cracker.DataCracker",
		};

		public void Run()
		{
			try
			{
				var target = new AsyncTargetWrapper(new FileTarget
				{
					Layout = _job.IsTest ? 
						"${logger} ${message}" :
						"${longdate} ${logger} ${message}",
					FileName = Path.Combine(_logPath, "debug.log"),
					ConcurrentWrites = false,
					KeepFileOpen = !_job.IsTest,
					ArchiveAboveSize = 10 * 1024 * 1024,
					ArchiveNumbering = ArchiveNumberingMode.Sequence,
				});

				var nconfig = new LoggingConfiguration();
				nconfig.AddTarget("FileTarget", target);

				foreach (var logger in FilteredLoggers)
				{
					var rule = new LoggingRule(logger, LogLevel.Info, target) { Final = true };
					nconfig.LoggingRules.Add(rule);
				}

				var defaultRule = new LoggingRule("*", LogLevel.Debug, target);
				nconfig.LoggingRules.Add(defaultRule);

				LogManager.Configuration = nconfig;

				var dom = ParsePit();

				// the JobWatcher integrates all the previous loggers into one:
				// File, Metrics, Web
				foreach (var test in dom.tests)
				{
					test.loggers.Clear();
				}

				var engine = new Engine(this);
				var engineTask = Task.Factory.StartNew(() => engine.startFuzzing(dom, _config));

				Loop(engineTask);

				if (_job.IsTest)
				{
					using (var db = new JobDatabase(_job.Guid))
					{
						EventSuccess(db);
					}
				}
			}
			catch (ApplicationException ex)
			{
				JobFail(ex);
				if (Configuration.LogLevel == LogLevel.Trace)
					Logger.Error("Exception: {0}", ex);
				else
					Logger.Error("Exception: {0}", ex.Message);
			}
			catch (Exception ex)
			{
				Logger.Error("Unhandled Exception: {0}", ex);
				throw;
			}
			finally
			{
				NLog.LogManager.Flush();
			}
		}

		private void Loop(Task engineTask)
		{
			Console.WriteLine("OK");

			while (true)
			{
				Console.Write("> ");
				var readerTask = Task.Factory.StartNew<string>(Console.ReadLine);
				var index = Task.WaitAny(engineTask, readerTask);
				if (index == 0)
				{
					// this causes any unhandled exceptions to be thrown
					try { engineTask.Wait(); }
					catch (AggregateException ex) { throw ex.InnerException; }
					return;
				}

				switch (readerTask.Result)
				{
					case "help":
						ShowHelp();
						break;
					case "stop":
						Console.WriteLine("OK");
						Stop();
						engineTask.Wait();
						return;
					case "pause":
						Console.WriteLine("OK");
						Pause();
						break;
					case "continue":
						Console.WriteLine("OK");
						Continue();
						break;
					default:
						Console.WriteLine("Invalid command");
						break;
				}
			}
		}

		private void ShowHelp()
		{
			Console.WriteLine("Available commands:");
			Console.WriteLine("    help");
			Console.WriteLine("    stop");
			Console.WriteLine("    pause");
			Console.WriteLine("    continue");
		}

		private bool ShouldStop()
		{
			if (!_pausedEvt.WaitOne(0))
			{
				using (var db = new JobDatabase(_job.Guid))
				{
					_job.Status = JobStatus.Paused;
					db.UpdateJob(_job);
				}

				_pausedEvt.WaitOne();

				using (var db = new JobDatabase(_job.Guid))
				{
					_job.Status = JobStatus.Running;
					db.UpdateJob(_job);
				}
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

		void AddEvent(JobDatabase db, string name, string description)
		{
			Debug.Assert(_job.IsTest);

			var testEvent = new TestEvent
			{
				Status = TestStatus.Active,
				Short = name,
				Description = description,
			};
			db.InsertTestEvent(testEvent);
			_events.Add(testEvent);
		}

		void EventSuccess(JobDatabase db)
		{
			Debug.Assert(_job.IsTest);

			var last = _events.Last();
			last.Status = TestStatus.Pass;
			db.UpdateTestEvents(new[] { last });
		}

		void EventFail(JobDatabase db, string resolve)
		{
			Debug.Assert(_job.IsTest);

			var last = _events.Last();
			last.Status = TestStatus.Fail;
			last.Resolve = resolve;
			db.UpdateTestEvents(new[] { last });
		}

		void JobFail(Exception ex)
		{
			if (_job.IsTest)
			{
				foreach (var testEvent in _events)
				{
					if (testEvent.Status == TestStatus.Active)
					{
						testEvent.Status = TestStatus.Fail;
						testEvent.Resolve = ex.Message;
					}
				}

				using (var db = new JobDatabase(_job.Guid))
				{
					db.UpdateTestEvents(_events);
				}
			}
		}

		Dictionary<string, object> ParseConfig()
		{
			var args = new Dictionary<string, object>();
			var pitConfig = _config.pitFile + ".config";

			// It is ok if a .config doesn't exist
			if (_job.IsTest && File.Exists(pitConfig))
			{
				using (var db = new JobDatabase(_job.Guid))
				{
					try
					{
						AddEvent(db, "Loading pit config", "Loading configuration file '{0}'".Fmt(pitConfig));

						var defs = PitDatabase.ParseConfig(_pitLibraryPath, pitConfig);
						args[DEFINED_VALUES] = defs;

						EventSuccess(db);
					}
					catch (Exception ex)
					{
						EventFail(db, ex.Message);
						throw;
					}
				}
			}
			else
			{
				// ParseConfig allows non-existant config files
				var defs = PitDatabase.ParseConfig(_pitLibraryPath, pitConfig);
				args[DEFINED_VALUES] = defs;
			}

			return args;
		}

		public Peach.Core.Dom.Dom ParsePit()
		{
			var args = ParseConfig();
			var parser = new GodelPitParser();

			if (_job.IsTest)
			{
				using (var db = new JobDatabase(_job.Guid))
				{
					AddEvent(db, "Loading pit file", "Loading pit file '{0}'".Fmt(_config.pitFile));

					try
					{
						var dom = parser.asParser(args, _config.pitFile);
						EventSuccess(db);
						return dom;
					}
					catch (Exception ex)
					{
						EventFail(db, ex.Message);
						throw;
					}
				}
			}

			return parser.asParser(args, _config.pitFile);
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			using (var db = new JobDatabase(_job.Guid))
			{
				_job.Seed = context.config.randomSeed;
				_job.Mode = JobMode.Fuzzing;
				_job.Status = JobStatus.Running;
				_job.StartDate = DateTime.UtcNow;

				db.UpdateJob(_job);

				if (_job.IsTest)
				{
					AddEvent(db, "Starting fuzzing engine", "Starting fuzzing engine");

					// Before we get iteration start, we will get AgentConnect & SessionStart
					_agentConnect = 0;
				}
			}
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			using (var db = new JobDatabase(_job.Guid))
			{
				_job.StopDate = DateTime.UtcNow;
				_job.Mode = JobMode.Fuzzing;
				_job.Status = JobStatus.Stopped;

				db.UpdateJob(_job);
			}
		}

		protected override void Agent_AgentConnect(RunContext context, AgentClient agent)
		{
			if (_job.IsTest)
			{
				using (var db = new JobDatabase(_job.Guid))
				{
					if (_agentConnect++ > 0)
						EventSuccess(db);

					AddEvent(db, "Connecting to agent", "Connecting to agent '{0}'".Fmt(agent.Url));
				}
			}
		}

		protected override void Agent_StartMonitor(RunContext context, AgentClient agent, string name, string cls)
		{
			if (_job.IsTest)
			{
				using (var db = new JobDatabase(_job.Guid))
				{
					EventSuccess(db);
					AddEvent(db, "Starting monitor", "Starting monitor '{0}'".Fmt(cls));
				}
			}
		}

		protected override void Agent_SessionStarting(RunContext context, AgentClient agent)
		{
			if (_job.IsTest)
			{
				using (var db = new JobDatabase(_job.Guid))
				{
					EventSuccess(db);
					AddEvent(db,
						"Starting fuzzing session",
						"Notifying agent '{0}' that the fuzzing session is starting".Fmt(agent.Url));
				}
			}
		}

		protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			_states.Clear();
			_cache.IterationStarting(context.currentIteration);

			if (context.reproducingFault)
			{
				if (context.reproducingIterationJumpCount == 0)
				{
					Debug.Assert(context.reproducingInitialIteration == currentIteration);
					_job.Mode = JobMode.Reproducing;
				}
				else
				{
					_job.Mode = JobMode.Searching;
				}
			}
			else
			{
				_job.Mode = JobMode.Fuzzing;
			}

			// TODO: rate throttle
			using (var db = new JobDatabase(_job.Guid))
			{
				db.UpdateJob(_job);

				if (_job.IsTest)
				{
					foreach (var testEvent in _events)
						testEvent.Status = TestStatus.Pass;
					db.UpdateTestEvents(_events);

					var desc = context.reproducingFault
						? "Reproducing fault found on the initial control record iteration"
						: "Running the initial control record iteration";

					// Add event for the iteration running
					AddEvent(db, "Running iteration", desc);
				}
			}
		}

		protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
		{
			if (!context.reproducingFault &&
				!context.controlIteration &&
				!context.controlRecordingIteration)
			{
				using (var db = new JobDatabase(_job.Guid))
				{
					_job.IterationCount++;
					db.UpdateJob(_job);
				}

				_cache.IterationFinished();
			}
		}

		protected override void StateStarting(RunContext context, Peach.Core.Dom.State state)
		{
			_states.Add(new Fault.State
			{
				name = state.Name,
				actions = new List<Fault.Action>()
			});

			_cache.StateStarting(state.Name, state.runCount);
		}

		protected override void ActionStarting(RunContext context, Peach.Core.Dom.Action action)
		{
			_cache.ActionStarting(action.Name);

			var rec = new Fault.Action
			{
				name = action.Name,
				type = action.type,
				models = new List<Fault.Model>()
			};

			foreach (var data in action.allData)
			{
				rec.models.Add(new Fault.Model
				{
					name = data.dataModel.Name,
					parameter = data.Name ?? "",
					dataSet = data.selectedData != null ? data.selectedData.Name : "",
					mutations = new List<Fault.Mutation>(),
				});
			}

			if (rec.models.Count == 0)
				rec.models = null;

			_states.Last().actions.Add(rec);
		}

		protected override void ActionFinished(RunContext context, Peach.Core.Dom.Action action)
		{
			var rec = _states.Last().actions.Last();
			if (rec.models == null)
				return;

			foreach (var model in rec.models)
			{
				if (model.mutations.Count == 0)
					model.mutations = null;
			}
		}

		protected override void DataMutating(
			RunContext context,
			ActionData data,
			DataElement element,
			Mutator mutator)
		{
			var rec = _states.Last().actions.Last();

			var tgtName = data.dataModel.Name;
			var tgtParam = data.Name ?? "";
			var tgtDataSet = data.selectedData != null ? data.selectedData.Name : "";
			var model = rec.models.FirstOrDefault(m =>
				m.name == tgtName &&
				m.parameter == tgtParam &&
				m.dataSet == tgtDataSet);
			Debug.Assert(model != null);

			model.mutations.Add(new Fault.Mutation()
			{
				element = element.fullName,
				mutator = mutator.Name
			});

			_cache.DataMutating(
				data.Name ?? "",
				element.fullName,
				mutator.Name,
				data.selectedData != null ? data.selectedData.Name : "");
		}

		protected override void Engine_ReproFault(
			RunContext context,
			uint currentIteration,
			Peach.Core.Dom.StateModel stateModel,
			Fault[] faults)
		{
			Debug.Assert(_reproFault == null);
			_reproFault = CombineFaults(currentIteration, stateModel, faults);
		}

		protected override void Engine_Fault(
			RunContext context,
			uint currentIteration,
			Peach.Core.Dom.StateModel stateModel,
			Fault[] faults)
		{
			var fault = CombineFaults(currentIteration, stateModel, faults);

			if (_reproFault != null)
			{
				// Save reproFault toSave in fault
				foreach (var kv in _reproFault.toSave)
				{
					var key = Path.Combine("Initial", kv.Key);
					fault.toSave.Add(key, kv.Value);
				}

				_reproFault = null;
			}

			SaveFault(context, fault, true);
		}

		protected override void Engine_ReproFailed(RunContext context, uint currentIteration)
		{
			Debug.Assert(_reproFault != null);

			// Update the searching ranges for the fault
			_reproFault.iterationStart = context.reproducingInitialIteration - context.reproducingIterationJumpCount;
			_reproFault.iterationStop = _reproFault.iteration;

			SaveFault(context, _reproFault, false);
			_reproFault = null;
		}

		private Fault CombineFaults(
			uint currentIteration,
			Peach.Core.Dom.StateModel stateModel,
			Fault[] faults)
		{
			// The combined fault will use toSave and not collectedData
			var ret = new Fault { collectedData = null };

			Fault coreFault = null;
			var dataFaults = new List<Fault>();

			// First find the core fault.
			foreach (var fault in faults)
			{
				if (fault.type == FaultType.Fault)
					coreFault = fault;
				else
					dataFaults.Add(fault);
			}

			if (coreFault == null)
				throw new PeachException("Error, we should always have a fault with type = Fault!");

			// Gather up data from the state model
			foreach (var item in stateModel.dataActions)
			{
				ret.toSave.Add(item.Key, item.Value);
			}

			// Write out all collected data information
			foreach (var fault in faults)
			{
				foreach (var kv in fault.collectedData)
				{
					var fileName = string.Join(".", new[]
					{
						fault.agentName, 
						fault.monitorName, 
						fault.detectionSource, 
						kv.Key
					}.Where(a => !string.IsNullOrEmpty(a)));
					ret.toSave.Add(fileName, new MemoryStream(kv.Value));
				}

				if (!string.IsNullOrEmpty(fault.description))
				{
					var fileName = string.Join(".", new[]
					{
						fault.agentName, 
						fault.monitorName, 
						fault.detectionSource, 
						"description.txt"
					}.Where(a => !string.IsNullOrEmpty(a)));
					ret.toSave.Add(fileName, new MemoryStream(Encoding.UTF8.GetBytes(fault.description)));
				}
			}

			// Copy over information from the core fault
			if (coreFault.folderName != null)
				ret.folderName = coreFault.folderName;
			else if (coreFault.majorHash == null &&
				coreFault.minorHash == null &&
				coreFault.exploitability == null)
				ret.folderName = "Unknown";
			else
				ret.folderName = "{0}_{1}_{2}".Fmt(
					coreFault.exploitability,
					coreFault.majorHash,
					coreFault.minorHash);

			// Save all states, actions, data sets, mutations
			var settings = new JsonSerializerSettings()
			{
				DefaultValueHandling = DefaultValueHandling.Ignore,
			};
			var json = JsonConvert.SerializeObject(
				new { States = _states },
				Formatting.Indented,
				settings);
			ret.toSave.Add("fault.json", new MemoryStream(Encoding.UTF8.GetBytes(json)));

			ret.controlIteration = coreFault.controlIteration;
			ret.controlRecordingIteration = coreFault.controlRecordingIteration;
			ret.description = coreFault.description;
			ret.detectionSource = coreFault.detectionSource;
			ret.monitorName = coreFault.monitorName;
			ret.agentName = coreFault.agentName;
			ret.exploitability = coreFault.exploitability;
			ret.iteration = currentIteration;
			ret.iterationStart = coreFault.iterationStart;
			ret.iterationStop = coreFault.iterationStop;
			ret.majorHash = coreFault.majorHash;
			ret.minorHash = coreFault.minorHash;
			ret.title = coreFault.title;
			ret.type = coreFault.type;
			ret.states = _states;

			return ret;
		}

		void SaveFault(RunContext context, Fault fault, bool isRepro)
		{
			var now = DateTime.UtcNow;

			var bucket = string.Join("_", new[] { 
					fault.majorHash, 
					fault.minorHash, 
					fault.exploitability 
				}.Where(s => !string.IsNullOrEmpty(s)));

			if (fault.folderName != null)
				bucket = fault.folderName;
			else if (string.IsNullOrEmpty(bucket))
				bucket = "Unknown";

			var faultDetail = new FaultDetail
			{
				Files = new List<FaultFile>(),
				Reproducable = isRepro,
				Iteration = fault.iteration,
				TimeStamp = now,
				BucketName = bucket,
				Source = fault.detectionSource,
				Exploitability = fault.exploitability,
				MajorHash = fault.majorHash,
				MinorHash = fault.minorHash,

				Title = fault.title,
				Description = fault.description,
				Seed = context.config.randomSeed,
				IterationStart = fault.iterationStart,
				IterationStop = fault.iterationStop,
			};

			var dir = fault.iteration.ToString("X8");

			foreach (var item in fault.toSave)
			{
				var fullName = Path.Combine(dir, item.Key);
				var path = Path.Combine(_logPath, fullName);
				var size = SaveFile(path, item.Value);

				faultDetail.Files.Add(new FaultFile
				{
					Name = Path.GetFileName(fullName),
					FullName = fullName,
					Size = size,
				});
			}

			using (var db = new JobDatabase(_job.Guid))
			{
				db.InsertFault(faultDetail);
				_cache.OnFault(new FaultMetric
				{
					Iteration = fault.iteration,
					MajorHash = fault.majorHash ?? "UNKNOWN",
					MinorHash = fault.minorHash ?? "UNKNOWN",
					Timestamp = now,
					Hour = now.Hour,
				});

				_job.FaultCount++;
				db.UpdateJob(_job);
			}
		}

		long SaveFile(string fullPath, Stream contents)
		{
			try
			{
				var dir = Path.GetDirectoryName(fullPath);
				Directory.CreateDirectory(dir);

				contents.Seek(0, SeekOrigin.Begin);

				using (var fs = new FileStream(fullPath, FileMode.CreateNew))
				{
					contents.CopyTo(fs, BitwiseStream.BlockCopySize);
					return fs.Position;
				}
			}
			catch (Exception e)
			{
				throw new PeachException(e.Message, e);
			}
		}
	}
}
