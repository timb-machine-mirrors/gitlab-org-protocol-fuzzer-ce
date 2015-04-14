
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;
using Encoding = Peach.Core.Encoding;
using Logger = Peach.Core.Logger;
using System.Diagnostics;
using Action = Peach.Core.Dom.Action;
using State = Peach.Core.Dom.State;
using Peach.Pro.Core.Runtime;

namespace Peach.Pro.Core.Loggers
{
	/// <summary>
	/// Standard file system Logger.
	/// </summary>
	[Logger("File")]
	[Logger("Filesystem", true)]
	[Logger("Logger.Filesystem")]
	[Parameter("Path", typeof(string), "Log folder")]
	public class JobLogger : Logger
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		const string DebugLogLayout = "${longdate} ${logger} ${message}";

		// Filter these loggers to the info level since they are spammy at debug
		static readonly string[] FilteredLoggers =
		{
			"Peach.Core.Dom.Array",
			"Peach.Core.Dom.Choice",
			"Peach.Core.Dom.DataElement",
			"Peach.Core.Cracker.DataCracker",
		};

		readonly List<Fault.State> _states = new List<Fault.State>();
		readonly List<TestEvent> _events = new List<TestEvent>();
		readonly LoggingConfiguration _loggingConfig = LogManager.Configuration;
		Fault _reproFault;
		TextWriter _log;
		MetricsCache _cache;
		Job _job;
		int _agentConnect;
		Exception _caught;
		DatabaseTarget _tempTarget;

		enum Category { Faults, Reproducing, NonReproducable }

		/// <summary>
		/// The user configured base path for all the logs
		/// </summary>
		public string BasePath { get; set; }

		public JobLogger()
		{
			BasePath = Configuration.LogRoot;
		}

		public JobLogger(Dictionary<string, Variant> args)
		{
			Variant path;
			if (!args.TryGetValue("Path", out path))
				path = new Variant(Configuration.LogRoot);
			BasePath = Path.GetFullPath((string)path);
		}

		public void Initialize(RunConfiguration config)
		{
			_tempTarget = new DatabaseTarget(config.id);
			ConfigureLogging(null, _tempTarget.Name, _tempTarget);
		}

		protected override void Agent_AgentConnect(RunContext context, AgentClient agent)
		{
			if (context.controlRecordingIteration)
			{
				using (var db = new NodeDatabase())
				{
					if (_agentConnect++ > 0)
						EventSuccess(db);

					AddEvent(db,
						context.config.id,
						"Connecting to agent",
						"Connecting to agent '{0}'".Fmt(agent.Url));
				}
			}
		}

		protected override void Agent_StartMonitor(RunContext context, AgentClient agent, string name, string cls)
		{
			if (context.controlRecordingIteration)
			{
				using (var db = new NodeDatabase())
				{
					EventSuccess(db);
					AddEvent(db,
						context.config.id,
						"Starting monitor",
						"Starting monitor '{0}'".Fmt(cls));
				}
			}
		}

		protected override void Agent_SessionStarting(RunContext context, AgentClient agent)
		{
			if (context.controlRecordingIteration)
			{
				using (var db = new NodeDatabase())
				{
					EventSuccess(db);
					AddEvent(db,
						context.config.id,
						"Starting fuzzing session",
						"Notifying agent '{0}' that the fuzzing session is starting".Fmt(agent.Url));
				}
			}
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			Debug.Assert(_log == null);

			using (var db = new NodeDatabase())
			{
				_job = db.GetJob(context.config.id);
				if (_job == null)
					_job = new Job(context.config);
			}

			if (_job.DatabasePath == null)
			{
				_job.LogPath = GetLogPath(context, BasePath);
				if (!Directory.Exists(_job.LogPath))
					Directory.CreateDirectory(_job.LogPath);
			}

			ConfigureDebugLogging(context.config);

			using (var db = new JobDatabase(_job.DatabasePath))
			{
				var job = db.GetJob(_job.Guid);
				if (job == null)
				{
					_job.Mode = JobMode.Fuzzing;
					_job.Status = JobStatus.Running;
					_job.StartDate = DateTime.UtcNow;

					db.InsertJob(_job);
				}
				else
				{
					_job = job;
				}
			}

			_cache = new MetricsCache(_job.DatabasePath);

			using (var db = new NodeDatabase())
			{
				AddEvent(db,
					context.config.id,
					"Starting fuzzing engine",
					"Starting fuzzing engine");

				// Before we get iteration start, we will get AgentConnect & SessionStart
				_agentConnect = 0;

				db.UpdateJob(_job);
			}

			_log = File.CreateText(Path.Combine(_job.LogPath, "status.txt"));

			_log.WriteLine("Peach Fuzzing Run");
			_log.WriteLine("=================");
			_log.WriteLine("");
			_log.WriteLine("Date of run: " + context.config.runDateTime);
			_log.WriteLine("Peach Version: " + context.config.version);

			_log.WriteLine("Seed: " + context.config.randomSeed);

			_log.WriteLine("Command line: " + string.Join(" ", context.config.commandLine));
			_log.WriteLine("Pit File: " + context.config.pitFile);
			_log.WriteLine(". Test starting: " + context.test.Name);
			_log.WriteLine("");

			_log.Flush();
		}

		protected override void Engine_TestError(RunContext context, Exception ex)
		{
			Logger.Trace("Engine_TestError");

			_caught = ex;

			// Happens if we can't open the log during TestStarting()
			// because of permission issues
			if (_log != null)
			{
				_log.WriteLine("! Test error:");
				_log.WriteLine(ex);
				_log.Flush();
			}
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			Logger.Trace("Engine_TestFinished");

			if (_job != null)
			{
				Debug.Assert(_job.DatabasePath != null);
				using (var db = new JobDatabase(_job.DatabasePath))
				{
					_job.StopDate = DateTime.UtcNow;
					_job.Mode = JobMode.Fuzzing;
					_job.Status = JobStatus.Stopped;

					db.UpdateJob(_job);
				}

				using (var db = new NodeDatabase())
				{
					if (_caught == null)
						EventSuccess(db);
					db.UpdateJob(_job);
				}
			}

			// it's possible we reach here before Engine_TestStarting has had a chance to finish
			if (_log != null)
			{
				_log.WriteLine(". Test finished: " + context.test.Name);
				_log.Flush();
				_log.Close();
				_log.Dispose();
				_log = null;
			}

			RestoreLogging();
		}

		protected override void Engine_IterationStarting(
			RunContext context,
			uint currentIteration,
			uint? totalIterations)
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

			using (var db = new JobDatabase(_job.DatabasePath))
			{
				db.UpdateJob(_job);
			}

			if (context.controlRecordingIteration)
			{
				using (var db = new NodeDatabase())
				{
					foreach (var testEvent in _events)
						testEvent.Status = TestStatus.Pass;
					db.UpdateTestEvents(_events);

					var desc = context.reproducingFault
						? "Reproducing fault found on the initial control record iteration"
						: "Running the initial control record iteration";

					// Add event for the iteration running
					AddEvent(db, context.config.id, "Running iteration", desc);
				}
			}

			if (currentIteration == 1 || currentIteration % 100 == 0)
			{
				if (totalIterations.HasValue && totalIterations.Value < uint.MaxValue)
				{
					_log.WriteLine(". Iteration {0} of {1} : {2}",
						currentIteration, (uint)totalIterations, DateTime.Now);
					_log.Flush();
				}
				else
				{
					_log.WriteLine(". Iteration {0} : {1}", currentIteration, DateTime.Now);
					_log.Flush();
				}
			}
		}

		protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
		{
			if (!context.reproducingFault &&
				!context.controlIteration &&
				!context.controlRecordingIteration)
			{
				using (var db = new JobDatabase(_job.DatabasePath))
				{
					_job.IterationCount++;
					db.UpdateJob(_job);
				}

				_cache.IterationFinished();
			}
		}

		protected override void StateStarting(RunContext context, State state)
		{
			_states.Add(new Fault.State
			{
				name = state.Name,
				actions = new List<Fault.Action>()
			});

			_cache.StateStarting(state.Name, state.runCount);
		}

		protected override void ActionStarting(RunContext context, Action action)
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
				rec.models.Add(new Fault.Model()
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

		protected override void ActionFinished(RunContext context, Action action)
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

			model.mutations.Add(new Fault.Mutation
			{
				element = element.fullName,
				mutator = mutator.Name
			});

			_cache.DataMutating(tgtParam, element.fullName, mutator.Name, tgtDataSet);
		}

		protected override void Engine_ReproFault(
			RunContext context,
			uint currentIteration,
			StateModel stateModel,
			Fault[] faults)
		{
			Debug.Assert(_reproFault == null);

			_reproFault = CombineFaults(currentIteration, stateModel, faults);
			SaveFault(context, Category.Reproducing, _reproFault);
		}

		protected override void Engine_ReproFailed(RunContext context, uint currentIteration)
		{
			Debug.Assert(_reproFault != null);

			// Update the searching ranges for the fault
			_reproFault.iterationStart = context.reproducingInitialIteration - context.reproducingIterationJumpCount;
			_reproFault.iterationStop = _reproFault.iteration;

			SaveFault(context, Category.NonReproducable, _reproFault);
			_reproFault = null;
		}

		protected override void Engine_Fault(
			RunContext context,
			uint currentIteration,
			StateModel stateModel,
			Fault[] faults)
		{
			var fault = CombineFaults(currentIteration, stateModel, faults);

			if (_reproFault != null)
			{
				// Save reproFault toSave in fault
				foreach (var kv in _reproFault.toSave)
				{
					var key = Path.Combine("Initial", _reproFault.iteration.ToString(), kv.Key);
					fault.toSave.Add(key, kv.Value);
				}

				_reproFault = null;
			}

			SaveFault(context, Category.Faults, fault);
		}

		long SaveFile(string fullPath, Stream contents)
		{
			try
			{
				var dir = Path.GetDirectoryName(fullPath);
				if (!Directory.Exists(dir))
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

		private Fault CombineFaults(uint currentIteration, StateModel stateModel, Fault[] faults)
		{
			// The combined fault will use toSave and not collectedData
			var ret = new Fault { collectedData = null };

			Fault coreFault = null;
			var dataFaults = new List<Fault>();

			// First find the core fault.
			foreach (var fault in faults)
			{
				if (fault.type == FaultType.Fault)
				{
					coreFault = fault;
					Logger.Debug("Found core fault [" + coreFault.title + "]");
				}
				else
					dataFaults.Add(fault);
			}

			if (coreFault == null)
				throw new PeachException("Error, we should always have a fault with type = Fault!");

			// Gather up data from the state model
			foreach (var item in stateModel.dataActions)
			{
				Logger.Debug("Saving action: " + item.Key);
				ret.toSave.Add(item.Key, item.Value);
			}

			// Write out all collected data information
			foreach (var fault in faults)
			{
				Logger.Debug("Saving fault: " + fault.title);

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
				ret.folderName = string.Format("{0}_{1}_{2}",
					coreFault.exploitability,
					coreFault.majorHash,
					coreFault.minorHash);

			// Save all states, actions, data sets, mutations
			var settings = new JsonSerializerSettings()
			{
				DefaultValueHandling = DefaultValueHandling.Ignore
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

		void SaveFault(RunContext context, Category category, Fault fault)
		{
			switch (category)
			{
				case Category.Faults:
					_log.WriteLine("! Reproduced fault at iteration {0} : {1}",
						fault.iteration, DateTime.Now);
					break;
				case Category.NonReproducable:
					_log.WriteLine("! Non-reproducable fault detected at iteration {0} : {1}",
						fault.iteration, DateTime.Now);
					break;
				case Category.Reproducing:
					_log.WriteLine("! Fault detected at iteration {0}, trying to reproduce : {1}",
						fault.iteration, DateTime.Now);
					break;
			}

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

			// root/category/bucket/iteration
			var subDir = Path.Combine(
				_job.LogPath,
				category.ToString(),
				fault.folderName,
				fault.iteration.ToString());

			var faultDetail = new FaultDetail
			{
				Files = new List<FaultFile>(),
				Reproducable = category == Category.Reproducing,
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

				FaultPath = subDir,
			};

			foreach (var kv in fault.toSave)
			{
				var fileName = Path.Combine(subDir, kv.Key);
				var size = SaveFile(fileName, kv.Value);
				faultDetail.Files.Add(new FaultFile
				{
					Name = Path.GetFileName(fileName),
					FullName = kv.Key,
					Size = size,
				});
			}

			if (category != Category.Reproducing)
			{
				// Ensure any past saving of this fault as Reproducing has been cleaned up
				var reproDir = Path.Combine(_job.LogPath, Category.Reproducing.ToString());

				if (Directory.Exists(reproDir))
				{
					try
					{
						Directory.Delete(reproDir, true);
					}
					catch (IOException)
					{
						// Can happen if a process has a file/subdirectory open...
					}
				}

				using (var db = new JobDatabase(_job.DatabasePath))
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

			_log.Flush();
		}

		public void AddEvent(NodeDatabase db, Guid jobId, string name, string description)
		{
			var testEvent = new TestEvent
			{
				JobId = jobId.ToString(),
				Status = TestStatus.Active,
				Short = name,
				Description = description,
			};
			db.InsertTestEvent(testEvent);
			_events.Add(testEvent);
		}

		public void EventSuccess(NodeDatabase db)
		{
			var last = _events.Last();
			last.Status = TestStatus.Pass;
			db.UpdateTestEvents(new[] { last });
		}

		public void EventFail(NodeDatabase db, string resolve)
		{
			var last = _events.Last();
			last.Status = TestStatus.Fail;
			last.Resolve = resolve;
			db.UpdateTestEvents(new[] { last });
		}

		public void JobFail(Guid id, Exception ex)
		{
			foreach (var testEvent in _events)
			{
				if (testEvent.Status == TestStatus.Active)
				{
					testEvent.Status = TestStatus.Fail;
					testEvent.Resolve = ex.Message;
				}
			}

			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(id);
				Debug.Assert(job != null);

				job.StopDate = DateTime.UtcNow;
				job.Mode = JobMode.Fuzzing;
				job.Status = JobStatus.Stopped;
				job.Result = ex.Message;
				db.UpdateJob(job);

				db.UpdateTestEvents(_events);
			}
		}

		public void UpdateStatus(JobStatus status)
		{
			Debug.Assert(_job.DatabasePath != null);

			_job.Status = status;

			using (var db = new JobDatabase(_job.DatabasePath))
			{
				db.UpdateJob(_job);
			}
		}

		void RestoreLogging()
		{
			Debug.Assert(_loggingConfig != null);

			LogManager.Configuration = _loggingConfig;
		}

		void ConfigureDebugLogging(RunConfiguration config)
		{
			var target = new FileTarget
			{
				Name = "FileTarget",
				Layout = DebugLogLayout,
				FileName = _job.DebugLogPath,
				ConcurrentWrites = false,
				KeepFileOpen = !config.singleIteration,
				ArchiveAboveSize = 10 * 1024 * 1024,
				ArchiveNumbering = ArchiveNumberingMode.Sequence,
			};

			var wrapper = new AsyncTargetWrapper(target);

			string oldName = null;
			if (_tempTarget != null)
				oldName = _tempTarget.Name;
			ConfigureLogging(oldName, target.Name, wrapper);

			_tempTarget = null;
		}

		void ConfigureLogging(string oldName, string name, Target target)
		{
			var nconfig = LogManager.Configuration;
			if (oldName != null)
				nconfig.RemoveTarget(oldName);
			nconfig.AddTarget(name, target);

			foreach (var logger in FilteredLoggers)
			{
				var rule = new LoggingRule(logger, LogLevel.Info, target) { Final = true };
				nconfig.LoggingRules.Add(rule);
			}

			var defaultRule = new LoggingRule("*", LogLevel.Debug, target);
			nconfig.LoggingRules.Add(defaultRule);

			LogManager.Configuration = nconfig;
		}
	}

	class DatabaseTarget : Target
	{
		NodeDatabase _db = new NodeDatabase();
		readonly string _jobId;

		public DatabaseTarget(Guid jobId)
		{
			Name = "DatabaseTarget";
			_jobId = jobId.ToString();
		}

		protected override void Write(LogEventInfo logEvent)
		{
			_db.InsertJobLog(new JobLog
			{
				JobId = _jobId,
				Logger = logEvent.LoggerName,
				Message = logEvent.Message.Fmt(logEvent.Parameters),
				TimeStamp = logEvent.TimeStamp,
			});
		}

		protected override void Dispose(bool disposing)
		{
			Console.WriteLine("DatabaseTarget.Dispose>");

			base.Dispose(disposing);

			if (disposing && _db != null)
			{
				_db.Dispose();
				_db = null;
			}
		}
	}
}
