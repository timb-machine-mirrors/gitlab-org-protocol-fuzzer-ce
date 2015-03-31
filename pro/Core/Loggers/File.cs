
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
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Encoding = Peach.Core.Encoding;
using Logger = Peach.Core.Logger;

namespace Peach.Pro.Core.Loggers
{
	/// <summary>
	/// Standard file system Logger.
	/// </summary>
	[Logger("File")]
	[Logger("Filesystem", true)]
	[Logger("Logger.Filesystem")]
	[Parameter("Path", typeof(string), "Log folder")]
	public class FileLogger : Logger
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		Fault _reproFault;
		TextWriter _log;
		List<Fault.State> _states;

		public FileLogger(Dictionary<string, Variant> args)
		{
			Path = (string)args["Path"];
		}

		/// <summary>
		/// The user configured base path for all the logs
		/// </summary>
		public string Path { get; private set; }

		/// <summary>
		/// The specific path used to log faults for a given test.
		/// </summary>
		protected string RootDir { get; private set; }

		public enum Category { Faults, Reproducing, NonReproducable }

		protected void SaveFault(Category category, Fault fault)
		{
			switch (category)
			{
				case Category.Faults:
					_log.WriteLine("! Reproduced fault at iteration {0} : {1}",
						fault.iteration, DateTime.Now);
					break;
				case Category.NonReproducable:
					_log.WriteLine("! Non-reproducable fault detected at iteration {0} : {1}", fault.iteration, DateTime.Now);
					break;
				case Category.Reproducing:
					_log.WriteLine("! Fault detected at iteration {0}, trying to reprduce : {1}", fault.iteration, DateTime.Now);
					break;
			}

			// root/category/bucket/iteration
			var subDir = System.IO.Path.Combine(RootDir, category.ToString(), fault.folderName, fault.iteration.ToString());

			foreach (var kv in fault.toSave)
			{
				var fileName = System.IO.Path.Combine(subDir, kv.Key);

				SaveFile(category, fileName, kv.Value);
			}

			OnFaultSaved(category, fault, subDir);

			_log.Flush();
		}

		protected override void Engine_ReproFault(RunContext context, uint currentIteration, Peach.Core.Dom.StateModel stateModel, Fault[] faults)
		{
			System.Diagnostics.Debug.Assert(_reproFault == null);

			_reproFault = CombineFaults(context, currentIteration, stateModel, faults);
			SaveFault(Category.Reproducing, _reproFault);
		}

		protected override void Engine_ReproFailed(RunContext context, uint currentIteration)
		{
			System.Diagnostics.Debug.Assert(_reproFault != null);

			// Update the searching ranges for the fault
			_reproFault.iterationStart = context.reproducingInitialIteration - context.reproducingIterationJumpCount;
			_reproFault.iterationStop = _reproFault.iteration;

			SaveFault(Category.NonReproducable, _reproFault);
			_reproFault = null;
		}

		protected override void Engine_Fault(RunContext context, uint currentIteration, StateModel stateModel, Fault[] faults)
		{
			var fault = CombineFaults(context, currentIteration, stateModel, faults);

			if (_reproFault != null)
			{
				// Save reproFault toSave in fault
				foreach (var kv in _reproFault.toSave)
				{
					var key = System.IO.Path.Combine("Initial", _reproFault.iteration.ToString(), kv.Key);
					fault.toSave.Add(key, kv.Value);
				}

				_reproFault = null;
			}

			SaveFault(Category.Faults, fault);
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
					var fileName = string.Join(".", new[] { fault.agentName, fault.monitorName, fault.detectionSource, kv.Key }.Where(a => !string.IsNullOrEmpty(a)));
					ret.toSave.Add(fileName, new MemoryStream(kv.Value));
				}

				if (!string.IsNullOrEmpty(fault.description))
				{
					var fileName = string.Join(".", new[] { fault.agentName, fault.monitorName, fault.detectionSource, "description.txt" }.Where(a => !string.IsNullOrEmpty(a)));
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
			var settings = new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore };
			var json = JsonConvert.SerializeObject(new { States = _states }, Formatting.Indented, settings);
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

		protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			_states = new List<Fault.State>();

			if (currentIteration != 1 && currentIteration % 100 != 0)
				return;

			if (totalIterations.HasValue && totalIterations.Value < uint.MaxValue)
			{
				_log.WriteLine(". Iteration {0} of {1} : {2}", currentIteration, (uint)totalIterations, DateTime.Now);
				_log.Flush();
			}
			else
			{
				_log.WriteLine(". Iteration {0} : {1}", currentIteration, DateTime.Now);
				_log.Flush();
			}
		}

		protected override void StateStarting(RunContext context, State state)
		{
			_states.Add(
				new Fault.State()
				{
					name = state.Name,
					actions = new List<Fault.Action>()
				});
		}

		protected override void ActionStarting(RunContext context, Peach.Core.Dom.Action action)
		{
			var rec = new Fault.Action()
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
			var model = rec.models.FirstOrDefault(m => 
				m.name == tgtName && 
				m.parameter == tgtParam && 
				m.dataSet == tgtDataSet);
			System.Diagnostics.Debug.Assert(model != null);

			model.mutations.Add(new Fault.Mutation() { element = element.fullName, mutator = mutator.Name });
		}

		protected override void Engine_TestError(RunContext context, Exception e)
		{
			// Happens if we can't open the log during TestStarting()
			// because of permission issues
			if (_log != null)
			{
				_log.WriteLine("! Test error:");
				_log.WriteLine(e);
				_log.Flush();
			}
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			if (_log != null)
			{
				_log.WriteLine(". Test finished: " + context.test.Name);
				_log.Flush();
				_log.Close();
				_log.Dispose();
				_log = null;
			}
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			if (_log != null)
			{
				_log.Flush();
				_log.Close();
				_log.Dispose();
				_log = null;
			}

			RootDir = GetBasePath(context);

			_log = OpenStatusLog();

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

		protected virtual TextWriter OpenStatusLog()
		{
			try
			{
				Directory.CreateDirectory(RootDir);
			}
			catch (Exception e)
			{
				throw new PeachException(e.Message, e);
			}

			return File.CreateText(System.IO.Path.Combine(RootDir, "status.txt"));
		}

		protected virtual string GetBasePath(RunContext context)
		{
			return GetLogPath(context, Path);
		}

		/// <summary>
		/// Delegate for FaultSaved event.
		/// </summary>
		/// <param name="fault">Fault object that was saved</param>
		/// <param name="category">Category of fault</param>
		/// <param name="rootPath">Fault root folder</param>
		public delegate void FaultSavedEvent(Category category, Fault fault, string rootPath);
		public event FaultSavedEvent FaultSaved;

		protected virtual void OnFaultSaved(Category category, Fault fault, string rootPath)
		{
			if (category != Category.Reproducing)
			{
				// Ensure any past saving of this fault as Reproducing has been cleaned up
				var reproDir = System.IO.Path.Combine(RootDir, Category.Reproducing.ToString());

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
			}

			if (FaultSaved != null)
				FaultSaved(category, fault, rootPath);
		}

		protected virtual void SaveFile(Category category, string fullPath, Stream contents)
		{
			try
			{
				var dir = System.IO.Path.GetDirectoryName(fullPath);
				Directory.CreateDirectory(dir);

				contents.Seek(0, SeekOrigin.Begin);

				using (var f = new FileStream(fullPath, FileMode.CreateNew))
				{
					contents.CopyTo(f, BitStream.BlockCopySize);
				}
			}
			catch (Exception e)
			{
				throw new PeachException(e.Message, e);
			}
		}
	}
}
