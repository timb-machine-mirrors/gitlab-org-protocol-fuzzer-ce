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

namespace Peach.Pro.Core.Runtime.Enterprise
{
	class JobWatcher : Watcher
	{
		readonly string _logPath;
		Fault _reproFault;
		List<Fault.State> _states;

		public JobWatcher(string id)
		{
			var config = Utilities.GetUserConfig();
			var logRoot = config.AppSettings.Settings.Get("LogPath");
			if (string.IsNullOrEmpty(logRoot))
				logRoot = Utilities.GetAppResourcePath("db");

			_logPath = Path.Combine(logRoot, id);
			Directory.CreateDirectory(_logPath);
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			Job job;

			try
			{
				job = ReadJob();
			}
			catch (Exception)
			{
				Console.WriteLine("No job");
				job = new Job
				{
					Name = Path.GetFileNameWithoutExtension(context.config.pitFile),
					RangeStart = context.config.rangeStart,
					RangeStop = context.config.rangeStart,
					IterationCount = 0,
					Seed = context.config.randomSeed,
					StartDate = DateTime.UtcNow,
					HasMetrics = true,
				};
			}

			job.StartIteration = context.currentIteration;
			job.Mode = JobMode.Fuzzing;
			job.Status = JobStatus.Running;

			WriteJob(job);
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			var job = ReadJob();
			job.Mode = JobMode.Fuzzing;
			job.StopDate = DateTime.UtcNow;
			job.Status = JobStatus.Stopped;
			WriteJob(job);
		}

		protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			_states = new List<Fault.State>();

			// TODO: rate throttle
			var job = ReadJob();
			if (context.reproducingFault)
			{
				if (context.reproducingIterationJumpCount == 0)
				{
					Debug.Assert(context.reproducingInitialIteration == currentIteration);
					job.Mode = JobMode.Reproducing;
				}
				else
				{
					job.Mode = JobMode.Searching;
				}
				job.CurrentIteration = context.reproducingInitialIteration;
			}
			else
			{
				job.Mode = JobMode.Fuzzing;
				job.CurrentIteration = currentIteration;
			}
			WriteJob(job);
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

			SaveFault(fault);
		}

		protected override void Engine_ReproFault(RunContext context, uint currentIteration, StateModel stateModel, Fault[] faults)
		{
			Debug.Assert(_reproFault == null);

			_reproFault = CombineFaults(context, currentIteration, stateModel, faults);
			SaveFault(_reproFault);
		}

		protected override void Engine_ReproFailed(RunContext context, uint currentIteration)
		{
			Debug.Assert(_reproFault != null);

			// Update the searching ranges for the fault
			_reproFault.iterationStart = context.reproducingInitialIteration - context.reproducingIterationJumpCount;
			_reproFault.iterationStop = _reproFault.iteration;

			SaveFault(_reproFault);
			_reproFault = null;
		}

		protected override void StateStarting(RunContext context, State state)
		{
			_states.Add(new Fault.State
			{
				name = state.Name,
				actions = new List<Fault.Action>()
			});
		}

		protected override void ActionStarting(RunContext context, Peach.Core.Dom.Action action)
		{
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

		protected override void DataMutating(RunContext context, ActionData data, DataElement element, Mutator mutator)
		{
			var rec = _states.Last().actions.Last();

			var tgtName = data.dataModel.Name;
			var tgtParam = data.Name ?? "";
			var tgtDataSet = data.selectedData != null ? data.selectedData.Name : "";
			var model = rec.models.FirstOrDefault(m => m.name == tgtName && m.parameter == tgtParam && m.dataSet == tgtDataSet);
			Debug.Assert(model != null);

			model.mutations.Add(new Fault.Mutation() { element = element.fullName, mutator = mutator.Name });
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

		protected void SaveFault(Fault fault)
		{
			//switch (category)
			//{
			//	case Category.Faults:
			//		log.WriteLine("! Reproduced fault at iteration {0} : {1}",
			//			fault.iteration, DateTime.Now);
			//		break;
			//	case Category.NonReproducable:
			//		log.WriteLine("! Non-reproducable fault detected at iteration {0} : {1}", fault.iteration, DateTime.Now);
			//		break;
			//	case Category.Reproducing:
			//		log.WriteLine("! Fault detected at iteration {0}, trying to reprduce : {1}", fault.iteration, DateTime.Now);
			//		break;
			//}

			// root/category/bucket/iteration
			//var subDir = Path.Combine(
			//	RootDir, 
			//	category.ToString(), 
			//	fault.folderName, 
			//	fault.iteration.ToString());

			//foreach (var kv in fault.toSave)
			//{
			//	var fileName = Path.Combine(subDir, kv.Key);

			//	SaveFile(category, fileName, kv.Value);
			//}

			//OnFaultSaved(category, fault, subDir);

			//log.Flush();
		}

		private Job ReadJob()
		{
			return ReadObject<Job>("job.json");
		}

		private void WriteJob(Job job)
		{
			WriteObject(job, "job.json");
		}

		private T ReadObject<T>(string name)
		{
			var source = Path.Combine(_logPath, name);
			using (var stream = File.OpenRead(source))
			{
				return ReadObject<T>(stream);
			}
		}

		private T ReadObject<T>(Stream stream)
		{
			var serializer = JsonSerializer.CreateDefault();
			using (var reader = new StreamReader(stream))
			{
				return (T)serializer.Deserialize(reader, typeof(T));
			}
		}

		private void WriteObject(object obj, string name)
		{
			var target = Path.Combine(_logPath, name);
			var tmp = target + ".tmp";

			using (var stream = new FileStream(tmp, FileMode.Create))
			{
				WriteObject(obj, stream);
			}

			// save state to allow for resume
			if (File.Exists(target))
				File.Replace(tmp, target, null);
			else
				File.Move(tmp, target);
		}

		private void WriteObject(object obj, Stream stream)
		{
			var settings = new JsonSerializerSettings
			{
				Formatting = Formatting.Indented
			};
			var serializer = JsonSerializer.CreateDefault(settings);
			using (var writer = new StreamWriter(stream))
			{
				serializer.Serialize(writer, obj);
			}
		}
	}
}
