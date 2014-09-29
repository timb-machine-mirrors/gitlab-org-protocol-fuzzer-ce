
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
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Reflection;
using System.Linq;

using Peach.Core.IO;
using Peach.Core.Dom;
using Peach.Core.Cracker;

using NLog;

/*
 * If not 1st iteration, pick fandom data model to change
 * 
 */
namespace Peach.Core.MutationStrategies
{
	[DefaultMutationStrategy]
	[MutationStrategy("Random", true)]
	[MutationStrategy("RandomStrategy")]
	[Parameter("SwitchCount", typeof(int), "Number of iterations to perform per-mutator befor switching.", "200")]
	[Parameter("MaxFieldsToMutate", typeof(int), "Maximum fields to mutate at once.", "6")]
	public class RandomStrategy : MutationStrategy
	{
		protected class ElementId
		{
			public ElementId(string InstanceName, string ElementName)
			{
				this.Mutators = new List<Mutator>();
				this.InstanceName = InstanceName;
				this.ElementName = ElementName;
			}

			public List<Mutator> Mutators { get; private set; }
			public string InstanceName { get; private set; }
			public string ElementName { get; private set; }
		}

		protected class Iterations : KeyedCollection<string, ElementId>
		{
			protected override string GetKeyForItem(ElementId item)
			{
				return item.InstanceName + item.ElementName;
			}
		}

		protected class DataSetTracker
		{
			public DataSetTracker(string ModelName, List<Data> Options)
			{
				this.ModelName = ModelName;
				this.Options = Options;
				this.Iteration = 1;
			}

			public string ModelName { get; private set; }
			public List<Data> Options { get; private set; }
			public uint Iteration { get; set; }
		}

		protected class DataSets : KeyedCollection<string, DataSetTracker>
		{
			protected override string GetKeyForItem(DataSetTracker item)
			{
				return item.ModelName;
			}
		}

		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		DataSets _dataSets;
		List<Type> _mutators;

		/// <summary>
		/// Mutators that affect the state model
		/// </summary>
		List<Mutator> _stateMutators = new List<Mutator>();

		/// <summary>
		/// Elements across all states and actions
		/// </summary>
		Iterations _iterations;

		/// <summary>
		/// Elements for a single state
		/// </summary>
		Dictionary<string, Iterations> _iterationsByState;

		/// <summary>
		/// Elements for a single action. Key is "state.action".
		/// </summary>
		Dictionary<string, Iterations> _iterationsByAction;

		/// <summary>
		/// All available iterations.
		/// </summary>
		List<Iterations> _allIterations = new List<Iterations>();

		/// <summary>
		/// Iterations collection for currently selected scope. This
		/// collection issued to select elements and mutators for a
		/// test case.
		/// </summary>
		Iterations _iterationsInCurrentScope;

		/// <summary>
		/// container also contains states if we have mutations
		/// we can apply to them.  State names are prefixed with "STATE_" to avoid
		/// conflicting with data model names.
		/// Use a list to maintain the order this strategy learns about data models
		/// </summary>
		ElementId[] _mutations;

		/// <summary>
		/// State model mutation selected (or null for none).
		/// </summary>
		Mutator _stateModelMutation = null;

		uint _iteration;
		Random _randomDataSet;
		uint _lastIteration = 1;

		/// <summary>
		/// How often to switch files.
		/// </summary>
		int switchCount = 200;

		/// <summary>
		/// Maximum number of fields to mutate at once.
		/// </summary>
		int maxFieldsToMutate = 6;

		public RandomStrategy(Dictionary<string, Variant> args)
			: base(args)
		{
			if (args.ContainsKey("SwitchCount"))
				switchCount = int.Parse((string)args["SwitchCount"]);
			if (args.ContainsKey("MaxFieldsToMutate"))
				maxFieldsToMutate = int.Parse((string)args["MaxFieldsToMutate"]);
		}

		public override void Initialize(RunContext context, Engine engine)
		{
			base.Initialize(context, engine);

			context.ActionStarting += ActionStarting;
			context.StateStarting += StateStarting;
			engine.IterationStarting += engine_IterationStarting;
			engine.IterationFinished += engine_IterationFinished;
			engine.TestStarting += engine_TestStarting;
			context.StateModelStarting += context_StateModelStarting;
			_mutators = new List<Type>();
			_mutators.AddRange(EnumerateValidMutators().Where(
				v => (bool)v.GetField(
						"affectDataModel", 
						BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
							.GetValue(null)));

		}

		void engine_TestStarting(RunContext context)
		{
			// TODO - Make this not suck
			foreach (var type in EnumerateValidMutators().Where(v => v.Name.StartsWith("State") || v.Name.StartsWith("Action")))
			{
				//type.GetField("affectStateModel",
				//	BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.CreateInstance).GetValue(null);

				_stateMutators.Add(GetMutatorInstance(type, context.test.stateModel));
			}
		}

		void engine_IterationFinished(RunContext context, uint currentIteration)
		{
			_stateModelMutation = null;

			if (context.controlIteration && context.controlRecordingIteration)
			{
				// If we have one state and one action we will add
				// the same iterations a few times. this is okay.
				_allIterations.Clear();
				_allIterations.AddRange(_iterationsByState.Values);
				_allIterations.AddRange(_iterationsByAction.Values);
				_allIterations.Add(_iterations);
			}
		}

		void engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			if (context.controlIteration && context.controlRecordingIteration)
			{
				_iterationsByAction = new Dictionary<string, Iterations>();
				_iterationsByState = new Dictionary<string, Iterations>();
				_iterations = new Iterations();
				_dataSets = new DataSets();
				_mutations = null;
			}
			else
			{
				// Random.Next() Doesn't include max and we want it to
				var fieldsToMutate = Random.Next(1, maxFieldsToMutate + 1);

				// Our scope choice should auto-weight to Action, State, All
				// the more states and actions the less All will get chosen
				// might need to improve this in the future.
				_iterationsInCurrentScope = Random.Choice(_allIterations);
				_mutations = Random.Sample(_iterationsInCurrentScope, fieldsToMutate);

				if (logger.IsTraceEnabled)
				{
					if (_iterationsByState.ContainsValue(_iterationsInCurrentScope))
						logger.Trace("SCOPE: State: {0}", _iterationsByState.Where(v => v.Value == _iterationsInCurrentScope).First().Key);
					else if (_iterationsByAction.ContainsValue(_iterationsInCurrentScope))
						logger.Trace("SCOPE: Action: {0}", _iterationsByAction.Where(v => v.Value == _iterationsInCurrentScope).First().Key);
					else
						logger.Trace("SCOPE: All");
				}

				if (context.test.stateModel.states.Count > 1 && Random.NextInt32() % context.test.stateModel.states.Count == 0)
					_stateModelMutation = Random.Choice(_stateMutators);
			}
		}

		public override void Finalize(RunContext context, Engine engine)
		{
			base.Finalize(context, engine);

			context.ActionStarting -= ActionStarting;
			context.StateStarting -= StateStarting;
			engine.IterationStarting -= engine_IterationStarting;
			engine.IterationFinished -= engine_IterationFinished;
			engine.TestStarting -= engine_TestStarting;
			context.StateModelStarting -= context_StateModelStarting;
		}

		void context_StateModelStarting(RunContext context, StateModel stateModel)
		{
			if (_stateModelMutation != null)
				_stateModelMutation.randomMutation(stateModel);
		}

		private uint GetSwitchIteration()
		{
			// Returns the iteration we should switch our dataSet based off our
			// current iteration. For example, if switchCount is 10, this function
			// will return 1, 11, 21, 31, 41, 51, etc.
			uint ret = _iteration - ((_iteration - 1) % (uint)switchCount);
			return ret;
		}

		public override bool UsesRandomSeed
		{
			get
			{
				return true;
			}
		}

		public override bool IsDeterministic
		{
			get
			{
				return false;
			}
		}

		public override uint Iteration
		{
			get
			{
				return _iteration;
			}
			set
			{
				_iteration = value;
				SeedRandom();

				if (_lastIteration != _iteration &&
					_iteration == GetSwitchIteration() &&
					_dataSets != null &&
					_dataSets.Where(d => d.Options.Count > 1).Any())
				{
					logger.Debug("Iteration: Switch iteration, setting controlIteration and controlRecordingIteration.");

					// Only enable switch iteration if there is at least one data set
					// with two or more options.
					_randomDataSet = null;
				}

				if (_randomDataSet == null)
				{
					_randomDataSet = new Random(this.Seed + GetSwitchIteration());

					_context.controlIteration = true;
					_context.controlRecordingIteration = true;
					_lastIteration = _iteration;
				}

				_mutations = null;
			}
		}

		Dom.Action _currentAction;
		string _currentActionName;

		void ActionStarting(RunContext context, Dom.Action action)
		{
			_currentAction = action;

			// Is this a supported action?
			if (!action.outputData.Any())
				return;

			if (_context.controlIteration && _context.controlRecordingIteration)
			{
				_currentActionName = _currentState.name + "." + _currentAction.name;
				_iterationsByAction[_currentActionName] = new Iterations();
				
				RecordDataSet(action);
				SyncDataSet(action);
				RecordDataModel(action);
			}
			else if (!_context.controlIteration)
			{
				MutateDataModel(action);
			}
		}

		State _currentState;

		void StateStarting(RunContext context, State state)
		{
			_currentState = state;

			if (!_context.controlIteration || !_context.controlRecordingIteration)
				return;

			_iterationsByState[state.name] = new Iterations();
		}

		private void SyncDataSet(Dom.Action action)
		{
			System.Diagnostics.Debug.Assert(_iteration != 0);

			// Compute the iteration we need to switch on
			uint switchIteration = GetSwitchIteration();

			foreach (var item in action.outputData)
			{
				// Note: use the model name, not the instance name so
				// we only set the data set once for re-enterant states.
				var modelName = item.modelName;

				if (!_dataSets.Contains(modelName))
					return;

				var val = _dataSets[modelName];

				// If the last switch was within the current iteration range then we don't have to switch.
				if (switchIteration == val.Iteration)
					return;

				// Don't switch files if we are only using a single file :)
				if (val.Options.Where(x => !x.Ignore).Count() < 2)
					return;

				do
				{
					var opt = _randomDataSet.Choice(val.Options);

					// If data set was determined to be bad, ignore it
					if (opt.Ignore)
						continue;

					try
					{
						// Apply the data set option
						item.Apply(opt);

						// Save off the last switch iteration
						val.Iteration = switchIteration;

						// Done!
						return;
					}
					catch (PeachException ex)
					{
						logger.Debug(ex.Message);
						logger.Debug("Unable to apply data '{0}', removing from sample list.", opt.name);

						// Mark data set as ignored.
						// This is so skip-to will still be deterministic
						opt.Ignore = true;
					}
				}
				while (val.Options.Where(x => !x.Ignore).Any());

				throw new PeachException("Error, RandomStrategy was unable to apply data for \"" + item.dataModel.fullName + "\"");
			}
		}

		private void GatherMutators(string instanceName, DataElementContainer cont)
		{
			List<DataElement> allElements = new List<DataElement>();
			RecursevlyGetElements(cont, allElements);
			foreach (DataElement elem in allElements)
			{
				var rec = new ElementId(instanceName, elem.fullName);

				foreach (Type t in _mutators)
				{
					// can add specific mutators here
					if (SupportedDataElement(t, elem))
					{
						var mutator = GetMutatorInstance(t, elem);
						rec.Mutators.Add(mutator);
					}
				}

				if (rec.Mutators.Count > 0)
				{
					_iterations.Add(rec);
					_iterationsByAction[_currentActionName].Add(rec);
					_iterationsByState[_currentState.name].Add(rec);
				}
			}
		}

		private void RecordDataModel(Core.Dom.Action action)
		{
			foreach (var item in action.outputData)
			{
				GatherMutators(item.instanceName, item.dataModel);
			}
		}

		private void RecordDataSet(Core.Dom.Action action)
		{
			foreach (var item in action.outputData)
			{
				var options = item.allData.ToList();

				if (options.Count > 0)
				{
					// Don't use the instance name here, we only pick the data set
					// once per state, not each time the state is re-entered.
					var rec = new DataSetTracker(item.modelName, options);

					if (!_dataSets.Contains(item.modelName))
						_dataSets.Add(rec);
				}
			}
		}

		private void ApplyMutation(ActionData data)
		{
			var instanceName = data.instanceName;

			foreach (var item in _mutations)
			{
				if (item.InstanceName != instanceName)
					continue;

				var elem = data.dataModel.find(item.ElementName);
				if (elem != null && elem.MutatedValue == null)
				{
					Mutator mutator = Random.Choice(item.Mutators);
					Context.OnDataMutating(data, elem, mutator);
					logger.Debug("Action_Starting: Fuzzing: {0}", item.ElementName);
					logger.Debug("Action_Starting: Mutator: {0}", mutator.name);
					mutator.randomMutation(elem);
				}
				else
				{
					logger.Debug("Action_Starting: Skipping Fuzzing: {0}", item.ElementName);
				}
			}
		}

		private void MutateDataModel(Core.Dom.Action action)
		{
			// MutateDataModel should only be called after ParseDataModel
			System.Diagnostics.Debug.Assert(_iteration > 0);

			foreach (var item in action.outputData)
			{
				ApplyMutation(item);
			}
		}

		public override State MutateChangingState(State state)
		{
			if (_context.controlIteration)
				return state;

			if(_stateModelMutation != null)
			{
				Context.OnStateMutating(state, _stateModelMutation);

				logger.Debug("MutateChangingState: Fuzzing state change: {0}", state.name);
				logger.Debug("MutateChangingState: Mutator: {0}", _stateModelMutation.name);

				return _stateModelMutation.changeState(_currentState, _currentAction, state);
			}

			return state;
		}

		public override Dom.Action NextAction(State state, Dom.Action lastAction, Dom.Action nextAction)
		{
			if (_stateModelMutation != null)
				return _stateModelMutation.nextAction(state, lastAction, nextAction);

			return nextAction;
		}

		public override uint Count
		{
			get
			{
				return uint.MaxValue;
			}
		}
	}
}

// end
