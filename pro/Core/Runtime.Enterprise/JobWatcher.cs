using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Pro.Core.WebServices.Models;
using File = System.IO.File;
using Encoding = System.Text.Encoding;
using Fault = Peach.Core.Fault;
using Peach.Pro.Core.Storage;
using Peach.Core.IO;
using System.Data.Entity;

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
		readonly string _logPath;
		readonly List<Fault.State> _states = new List<Fault.State>();
		readonly List<Sample> _samples = new List<Sample>();
		Sample _sample = new Sample();
		Fault _reproFault;
		Guid _guid;
		string _dbPath;
		Job _job;
		bool _isReproducing;

		class MetricEntity<T> where T : class, IMetric, new()
		{
			readonly Dictionary<string, T> _map = new Dictionary<string, T>();

			public void Load(DbSet<T> dbSet)
			{
				dbSet.ForEach(item => _map.Add(item.Name, item));
			}

			public T Add(JobContext db, DbSet<T> dbSet, string name)
			{
				T entity;
				if (!_map.TryGetValue(name, out entity))
				{
					entity = dbSet.Add(new T { Name = name });
					_map.Add(name, entity);
				}
				else
				{
					dbSet.Attach(entity);
				}
				return entity;
			}
		}

		readonly MetricEntity<Storage.State> _stateMetrics = new MetricEntity<Storage.State>();
		readonly MetricEntity<Storage.Action> _actionMetrics = new MetricEntity<Storage.Action>();
		readonly MetricEntity<Storage.Parameter> _parameterMetrics = new MetricEntity<Storage.Parameter>();
		readonly MetricEntity<Storage.Element> _elementMetrics = new MetricEntity<Storage.Element>();
		readonly MetricEntity<Storage.Mutator> _mutatorMetrics = new MetricEntity<Storage.Mutator>();
		readonly MetricEntity<Storage.Dataset> _datasetMetrics = new MetricEntity<Storage.Dataset>();
		readonly Dictionary<string, Storage.Bucket> _bucketsMap = new Dictionary<string, Storage.Bucket>();

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
				db.Database.Initialize(true);

				_stateMetrics.Load(db.States);
				_actionMetrics.Load(db.Actions);
				_parameterMetrics.Load(db.Parameters);
				_elementMetrics.Load(db.Elements);
				_mutatorMetrics.Load(db.Mutators);
				_datasetMetrics.Load(db.Datasets);
			}
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			using (var db = new JobContext(_dbPath))
			{
				_job = db.Jobs.Find(_guid);
				if (_job == null)
				{
					_job = db.Jobs.Add(new Job
					{
						Id = _guid,
						Name = Path.GetFileNameWithoutExtension(context.config.pitFile),
						RangeStart = context.config.rangeStart,
						RangeStop = context.config.rangeStart,
						IterationCount = 0,
						Seed = context.config.randomSeed,
						StartDate = DateTime.UtcNow,
						HasMetrics = true,
					});
				}

				_job.StartIteration = context.currentIteration;
				_job.Mode = JobMode.Fuzzing;
				_job.Status = JobStatus.Running;

				db.SaveChanges();
			}
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			using (var db = new JobContext(_dbPath))
			{
				db.Jobs.Attach(_job);

				_job.Mode = JobMode.Fuzzing;
				_job.StopDate = DateTime.UtcNow;
				_job.Status = JobStatus.Stopped;

				db.SaveChanges();
			}
		}

		protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			_states.Clear();
			_samples.Clear();
			_isReproducing = context.reproducingFault;

			// TODO: rate throttle
			using (var db = new JobContext(_dbPath))
			{
				db.Jobs.Attach(_job);

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

				db.SaveChanges();
			}
		}

		protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
		{
		}

		protected override void Engine_Fault(RunContext context, uint currentIteration, StateModel stateModel, Fault[] faults)
		{
			var fault = CombineFaults(context, currentIteration, stateModel, faults);

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

			_reproFault = CombineFaults(context, currentIteration, stateModel, faults);
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
					_sample.State = _stateMetrics.Add(db, db.States, state.Name);
					db.StateInstances.Add(new StateInstance { State = _sample.State });
					db.SaveChanges();
				}
			}
		}

		protected override void ActionStarting(RunContext context, Peach.Core.Dom.Action action)
		{
			using (var db = new JobContext(_dbPath))
			{
				_sample.Action = _actionMetrics.Add(db, db.Actions, action.Name);
				db.SaveChanges();
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
				db.States.Attach(_sample.State);
				db.Actions.Attach(_sample.Action);

				_sample.Parameter = _parameterMetrics.Add(db, db.Parameters, data.Name ?? "");
				_sample.Element = _elementMetrics.Add(db, db.Elements, element.fullName);
				_sample.Mutator = _mutatorMetrics.Add(db, db.Mutators, mutator.Name);
				_sample.Dataset = _datasetMetrics.Add(db, db.Datasets,
					data.selectedData != null ? data.selectedData.Name : "");

				_samples.Add(_sample);

				if (!_isReproducing)
					db.Samples.Add(_sample);

				_sample = new Sample
				{
					State = _sample.State,
					Action = _sample.Action,
					Parameter = _sample.Parameter,
					Element = _sample.Element,
					Mutator = _sample.Mutator,
					Dataset = _sample.Dataset,
				};

				db.SaveChanges();
			}
		}

		private Fault CombineFaults(RunContext context, uint currentIteration, StateModel stateModel, Fault[] faults)
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
			//switch (category)
			//{
			//	case Category.Faults:
			//		log.WriteLine("! Reproduced fault at iteration {0} : {1}",
			//			fault.iteration, 
			//			DateTime.Now);
			//		break;
			//	case Category.NonReproducable:
			//		log.WriteLine("! Non-reproducable fault detected at iteration {0} : {1}", 
			//			fault.iteration, 
			//			DateTime.Now);
			//		break;
			//	case Category.Reproducing:
			//		log.WriteLine("! Fault detected at iteration {0}, trying to reprduce : {1}", 
			//			fault.iteration, 
			//			DateTime.Now);
			//		break;
			//}
			using (var db = new JobContext(_dbPath))
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
					var size = SaveFile(category, path, item.Value);

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
				string reproDir = Path.Combine(
					_logPath,
					Category.Reproducing.ToString());

				if (Directory.Exists(reproDir))
				{
					try { Directory.Delete(reproDir, true); }
					// Can happen if a process has a file/subdirectory open...
					catch (IOException) { }
				}

				db.Faults.Add(entity);

				SaveFaultMetrics(db, fault, now);

				db.SaveChanges();
			}
		}

		private void SaveFaultMetrics(JobContext db, Fault fault, DateTime now)
		{
			var majorHash = fault.majorHash ?? "UNKNOWN";
			var minorHash = fault.minorHash ?? "UNKONWN";
			var name = majorHash;
			if (fault.minorHash != null)
				name = "{0}_{1}".Fmt(majorHash, minorHash);

			var bucket = db.Buckets.Add(new Bucket
			{
				Name = name,
				MajorHash = majorHash,
				MinorHash = minorHash,
			});

			var metric = db.FaultMetrics.Add(new FaultMetric
			{
				Iteration = fault.iteration,
				Bucket = bucket,
				Timestamp = now,
				Hour = now.Hour,
				Samples = new List<FaultMetricSample>(),
			});

			_samples.ForEach(x =>
			{
				db.States.Attach(x.State);
				db.Actions.Attach(x.Action);
				db.Parameters.Attach(x.Parameter);
				db.Elements.Attach(x.Element);
				db.Mutators.Attach(x.Mutator);
				db.Datasets.Attach(x.Dataset);

				if (_sample.Id != 0)
					db.Samples.Attach(x);

				metric.Samples.Add(new FaultMetricSample { Sample = x });
			});
		}

		long SaveFile(Category category, string fullPath, Stream contents)
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
