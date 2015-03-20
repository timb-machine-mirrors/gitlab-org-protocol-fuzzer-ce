using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Pro.Core.WebServices.Models;
using Encoding = System.Text.Encoding;
using Fault = Peach.Core.Fault;
using Peach.Pro.Core.Storage;
using Peach.Core.IO;

namespace Peach.Pro.Core.Runtime.Enterprise
{
	public enum Category
	{
		Faults,
		Reproducing,
		NonReproducable,
	}

	class JobWatcher : Watcher
	{
		readonly Guid _guid;
		readonly string _dbPath;
		readonly string _logPath;
		readonly List<Fault.State> _states = new List<Fault.State>();
		readonly List<Mutation> _mutations = new List<Mutation>();
		Mutation _mutation = new Mutation();
		Fault _reproFault;
		Job _job;
		bool _isReproducing;

		readonly MetricsCache _cache;

		public JobWatcher(Guid guid)
		{
			_guid = guid;

			var config = Utilities.GetUserConfig();
			var logRoot = config.AppSettings.Settings.Get("LogPath");
			if (string.IsNullOrEmpty(logRoot))
				logRoot = Utilities.GetAppResourcePath("db");

			_logPath = Path.Combine(logRoot, _guid.ToString());
			Directory.CreateDirectory(_logPath);

			_dbPath = Path.Combine(_logPath, "peach.db");

			using (var db = new JobContext(_dbPath))
			{
				_cache = new MetricsCache(db);
			}
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			using (var db = new JobContext(_dbPath))
			{
				bool isNew = false;

				_job = db.GetJob(_guid.ToString());
				if (_job == null)
				{
					isNew = true;
					_job = new Job
					{
						Id = _guid.ToString(),
						Name = Path.GetFileNameWithoutExtension(context.config.pitFile),
						RangeStart = context.config.rangeStart,
						RangeStop = context.config.rangeStart,
						IterationCount = 0,
						Seed = context.config.randomSeed,
						StartDate = DateTime.UtcNow,
						HasMetrics = true,
					};
				}

				_job.StartIteration = context.currentIteration;
				_job.Mode = JobMode.Fuzzing;
				_job.Status = JobStatus.Running;

				db.UpsertJob(_job, isNew);
			}
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			using (var db = new JobContext(_dbPath))
			{
				_job.Mode = JobMode.Fuzzing;
				_job.StopDate = DateTime.UtcNow;
				_job.Status = JobStatus.Stopped;

				db.UpdateJob(_job);
			}
		}

		protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			_states.Clear();
			_mutations.Clear();
			_isReproducing = context.reproducingFault;

			// TODO: rate throttle
			using (var db = new JobContext(_dbPath))
			{
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
					_job.CurrentIteration = context.reproducingInitialIteration;
				}
				else
				{
					_job.Mode = JobMode.Fuzzing;
					_job.CurrentIteration = currentIteration;
				}

				db.UpdateJob(_job);
			}
		}

		protected override void Engine_Fault(RunContext context, uint currentIteration, StateModel stateModel, Fault[] faults)
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

		protected override void Engine_ReproFault(RunContext context, uint currentIteration, StateModel stateModel, Fault[] faults)
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

		protected override void StateStarting(RunContext context, Peach.Core.Dom.State state)
		{
			_states.Add(new Fault.State
			{
				name = state.Name,
				actions = new List<Fault.Action>()
			});

			var dom = state.parent.parent;

			if (!_isReproducing &&
				!dom.context.controlIteration &&
				!dom.context.controlRecordingIteration)
			{
				using (var db = new JobContext(_dbPath))
				{
					_mutation.StateId = _cache.Add<Storage.State>(db, state.Name);
					db.InsertStateInstance(new StateInstance { StateId = _mutation.StateId });
				}
			}
		}

		protected override void ActionStarting(RunContext context, Peach.Core.Dom.Action action)
		{
			using (var db = new JobContext(_dbPath))
			{
				_mutation.ActionId = _cache.Add<Storage.Action>(db, action.Name);
			}

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
			Peach.Core.Mutator mutator)
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

			using (var db = new JobContext(_dbPath))
			{
				_mutation.Iteration = context.currentIteration;
				_mutation.ParameterId = _cache.Add<Storage.Parameter>(db, data.Name ?? "");
				_mutation.ElementId = _cache.Add<Storage.Element>(db, element.fullName);
				_mutation.MutatorId = _cache.Add<Storage.Mutator>(db, mutator.Name);
				_mutation.DatasetId = _cache.Add<Storage.Dataset>(db,
					data.selectedData != null ? data.selectedData.Name : "");

				_mutations.Add(_mutation);

				if (!_isReproducing)
					db.InsertMutation(_mutation);

				_mutation = new Mutation
				{
					Id = 0,
					Iteration = _mutation.Iteration,
					StateId = _mutation.StateId,
					ActionId = _mutation.ActionId,
					ParameterId = _mutation.ParameterId,
					ElementId = _mutation.ElementId,
					MutatorId = _mutation.MutatorId,
					DatasetId = _mutation.DatasetId,
				};
			}
		}

		private Fault CombineFaults(uint currentIteration, StateModel stateModel, Fault[] faults)
		{
			// The combined fault will use toSave and not collectedData
			var ret = new Fault() { collectedData = null };

			Fault coreFault = null;
			var dataFaults = new List<Fault>();

			// First find the core fault.
			foreach (var fault in faults)
			{
				if (fault.type == FaultType.Fault)
				{
					coreFault = fault;
					//logger.Debug("Found core fault [" + coreFault.title + "]");
				}
				else
					dataFaults.Add(fault);
			}

			if (coreFault == null)
				throw new PeachException("Error, we should always have a fault with type = Fault!");

			// Gather up data from the state model
			foreach (var item in stateModel.dataActions)
			{
				//logger.Debug("Saving action: " + item.Key);
				ret.toSave.Add(item.Key, item.Value);
			}

			// Write out all collected data information
			foreach (var fault in faults)
			{
				//logger.Debug("Saving fault: " + fault.title);

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
			else if (coreFault.majorHash == null && coreFault.minorHash == null && coreFault.exploitability == null)
				ret.folderName = "Unknown";
			else
				ret.folderName = string.Format("{0}_{1}_{2}", coreFault.exploitability, coreFault.majorHash, coreFault.minorHash);

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

		void SaveFault(RunContext context, Category category, Fault fault)
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

			var entity = new FaultDetail
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

			// root/category/bucket/iteration
			var subDir = Path.Combine(
				category.ToString(),
				fault.folderName,
				fault.iteration.ToString());

			foreach (var item in fault.toSave)
			{
				var fullName = Path.Combine(subDir, item.Key);
				var path = Path.Combine(_logPath, fullName);
				var size = SaveFile(path, item.Value);

				entity.Files.Add(new FaultFile
				{
					Name = Path.GetFileName(fullName),
					FullName = fullName,
					Size = size,
				});
			}

			if (category == Category.Reproducing)
				return;

			// Ensure any past saving of this fault as Reproducing has been cleaned up
			var reproDir = Path.Combine(
				_logPath,
				Category.Reproducing.ToString());

			if (Directory.Exists(reproDir))
			{
				try { Directory.Delete(reproDir, true); }
				// Can happen if a process has a file/subdirectory open...
				catch (IOException) { }
			}

			using (var db = new JobContext(_dbPath))
			{
				db.InsertFault(entity);
				SaveFaultMetrics(db, fault, now);
			}
		}

		private void SaveFaultMetrics(JobContext db, Fault fault, DateTime now)
		{
			db.InsertFaultMetric(new FaultMetric
			{
				Iteration = fault.iteration,
				MajorHash = fault.majorHash ?? "UNKNOWN",
				MinorHash = fault.minorHash ?? "UNKNOWN",
				Timestamp = now,
				Hour = now.Hour,
			}, _mutations);
		}

		long SaveFile(string fullPath, Stream contents)
		{
			try
			{
				string dir = Path.GetDirectoryName(fullPath);
				Directory.CreateDirectory(dir);

				contents.Seek(0, SeekOrigin.Begin);

				using (var fs = new FileStream(fullPath, FileMode.CreateNew))
				{
					contents.CopyTo(fs, BitStream.BlockCopySize);
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
