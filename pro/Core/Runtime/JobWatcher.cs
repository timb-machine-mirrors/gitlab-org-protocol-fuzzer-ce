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

namespace Peach.Pro.Core.Runtime
{
	public enum Category
	{
		Faults,
		Reproducing,
		NonReproducable,
	}

	class JobWatcher : Watcher
	{
		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		readonly Job _job;
		readonly string _logPath;
		readonly MetricsCache _cache;
		readonly List<Fault.State> _states = new List<Fault.State>();
		readonly ManualResetEvent _pausedEvt = new ManualResetEvent(true);
		Fault _reproFault;
		bool _shouldStop;

		public JobWatcher(Job job, RunConfiguration config)
		{
			_job = job;
			_logPath = JobDatabase.GetStorageDirectory(_job.Guid);
			_cache = new MetricsCache(() => new JobDatabase(_job.Guid));
			config.shouldStop = ShouldStop;
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
		
		protected override void Engine_TestStarting(RunContext context)
		{
			using (var db = new JobDatabase(_job.Guid))
			{
				_job.Seed = context.config.randomSeed;
				_job.Mode = JobMode.Fuzzing;
				_job.Status = JobStatus.Running;

				db.UpdateJob(_job);
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
			StateModel stateModel, 
			Fault[] faults)
		{
			Debug.Assert(_reproFault == null);
			_reproFault = CombineFaults(currentIteration, stateModel, faults);
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
					var key = Path.Combine("Initial", kv.Key);
					fault.toSave.Add(key, kv.Value);
				}

				_reproFault = null;
			}

			SaveFault(context, fault);
		}

		protected override void Engine_ReproFailed(RunContext context, uint currentIteration)
		{
			Debug.Assert(_reproFault != null);

			// Update the searching ranges for the fault
			_reproFault.iterationStart = context.reproducingInitialIteration - context.reproducingIterationJumpCount;
			_reproFault.iterationStop = _reproFault.iteration;

			SaveFault(context, _reproFault);
			_reproFault = null;
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

		void SaveFault(RunContext context, Fault fault)
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
