
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
using System.Diagnostics;

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
	[Parameter("StateMutation", typeof(bool), "Enable state mutations.", "false")]
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

			public string InstanceName { get; private set; }
			public string ElementName { get; private set; }
			public List<Mutator> Mutators { get; private set; }
		}

		protected class Iterations : KeyedCollection<string, ElementId>
		{
			protected override string GetKeyForItem(ElementId item)
			{
				return item.InstanceName + item.ElementName;
			}
		}

		[DebuggerDisplay("{InstanceName} {ElementName} Mutators = {Mutators.Count}")]
		protected class MutableItem : INamed
		{
			//public MutableItem(string instanceName)
			//{
			//	InstanceName = instanceName;
			//	ElementName = "";
			//	Mutators = new List<Mutator>();
			//}

			public MutableItem(string instanceName, string elementName)
			{
				InstanceName = instanceName;
				ElementName = elementName;
				Mutators = new List<Mutator>();
			}

			public string name { get { return InstanceName; } }
			public string InstanceName { get; private set; }
			public string ElementName { get; private set; }
			public List<Mutator> Mutators { get; private set; }
		}

		[DebuggerDisplay("{Name} Count = {Count}")]
		[DebuggerTypeProxy(typeof(DebugView))]
		protected class MutationScope : List<MutableItem>, IComparable<MutationScope>
		{
			class DebugView
			{
				MutationScope obj;

				public DebugView(MutationScope obj)
				{
					this.obj = obj;
				}

				[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
				public MutableItem[] Items
				{
					get { return obj.ToArray(); }
				}
			}

			public MutationScope(string name)
			{
				Name = name;
			}

			public string Name
			{
				get;
				private set;
			}

			public int ChildScopes
			{
				get;
				set;
			}

			public int CompareTo(MutationScope other)
			{
				return string.CompareOrdinal(Name, other.Name);
			}
		}

		[DebuggerDisplay("{name} - {Opions.Count} Options")]
		protected class DataSetTracker : INamed
		{
			public DataSetTracker(string ModelName, List<Data> Options)
			{
				this.ModelName = ModelName;
				this.Options = Options;
				this.Iteration = 1;
			}

			public string name { get { return ModelName; } }
			public string ModelName { get; private set; }
			public List<Data> Options { get; private set; }
			public uint Iteration { get; set; }
		}

		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Collection of all dataSets across all fully qualified model names.
		/// NamedCollection guarantees element order based on insertion.
		/// </summary>
		NamedCollection<DataSetTracker> dataSets = new NamedCollection<DataSetTracker>();

		/// <summary>
		/// The iteration when the last data set switch occured.
		/// </summary>
		uint lastSwitchIteration = 1;

		/// <summary>
		/// How often to switch files.
		/// </summary>
		uint switchCount = 200;

		/// <summary>
		/// Random number generator used for switching data sets.
		/// This is independent from context.Random so that if we skip to iteration
		/// 505, we will use a random number generator seeded with the 'switch'
		/// iteration of 401.
		/// </summary>
		Random randomDataSet;

		/// <summary>
		/// Mutators that affect the data model
		/// </summary>
		List<Type> dataMutators = new List<Type>();

		/// <summary>
		/// Mutators that affect the state model
		/// </summary>
		List<Mutator> stateMutators = new List<Mutator>();

		/// <summary>
		/// Used on control record iterations to collect
		/// mutable elements across all states and actions.
		/// </summary>
		MutationScope mutationScopeGlobal;

		/// <summary>
		/// Used on control record iterations to collect
		/// mutable elements on a per state basis.
		/// </summary>
		List<MutationScope> mutationScopeState;

		/// <summary>
		/// Used on control record iterations to collect
		/// mutable elements on a per action basis.
		/// </summary>
		List<MutationScope> mutationScopeAction;

		/// <summary>
		/// List of all mutable items at each mutation scope.
		/// </summary>
		List<MutationScope> mutableItems = new List<MutationScope>();

		//List<List<MutableItem>> mutableByState
		List<NamedCollection<MutableItem>> mutationsByState = new List<NamedCollection<MutableItem>>();

		MutableItem[] mutations;

		//List<string> scope = new List<string>();

#if DISABLED
		/// <summary>
		/// Elements across all states and actions
		/// </summary>
		//Iterations _iterations;

		/// <summary>
		/// Elements for a single state
		/// </summary>
		//Dictionary<string, Iterations> _iterationsByState;

		/// <summary>
		/// Elements for a single action. Key is "state.action".
		/// </summary>
		//Dictionary<string, Iterations> _iterationsByAction;

		/// <summary>
		/// All available iterations.
		/// </summary>
		//List<Iterations> _allIterations = new List<Iterations>();

		/// <summary>
		/// Iterations collection for currently selected scope. This
		/// collection issued to select elements and mutators for a
		/// test case.
		/// </summary>
		//Iterations _iterationsInCurrentScope;

		/// <summary>
		/// container also contains states if we have mutations
		/// we can apply to them.  State names are prefixed with "STATE_" to avoid
		/// conflicting with data model names.
		/// Use a list to maintain the order this strategy learns about data models
		/// </summary>
		//ElementId[] _mutations;

		/// <summary>
		/// State model mutation selected (or null for none).
		/// </summary>
		//Mutator _stateModelMutation = null;
#endif

		uint _iteration;

		/// <summary>
		/// Maximum number of fields to mutate at once.
		/// </summary>
		int maxFieldsToMutate = 6;

		bool stateMutations = false;

		public RandomStrategy(Dictionary<string, Variant> args)
			: base(args)
		{
			if (args.ContainsKey("SwitchCount"))
				switchCount = uint.Parse((string)args["SwitchCount"]);
			if (args.ContainsKey("MaxFieldsToMutate"))
				maxFieldsToMutate = int.Parse((string)args["MaxFieldsToMutate"]);
			if (args.ContainsKey("StateMutations"))
				stateMutations = bool.Parse((string)args["StateMutations"]);
		}

		#region Mutation Strategy Overrides

		public override void Initialize(RunContext context, Engine engine)
		{
			base.Initialize(context, engine);

			context.ActionStarting += ActionStarting;
			context.StateStarting += StateStarting;
			engine.IterationStarting += IterationStarting;
			engine.IterationFinished += IterationFinished;
			context.StateModelStarting += StateModelStarting;

			foreach (var m in EnumerateValidMutators())
			{
				if (m.GetStaticField<bool>("affectDataModel"))
					dataMutators.Add(m);

				if (m.GetStaticField<bool>("affectStateModel"))
					stateMutators.Add(GetMutatorInstance(m, context.test.stateModel));
			}
		}

		public override void Finalize(RunContext context, Engine engine)
		{
			base.Finalize(context, engine);

			context.ActionStarting -= ActionStarting;
			context.StateStarting -= StateStarting;
			engine.IterationStarting -= IterationStarting;
			engine.IterationFinished -= IterationFinished;
			context.StateModelStarting -= StateModelStarting;
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
				SeedRandomDataSet();
			}
		}

		public override uint Count
		{
			get
			{
				return uint.MaxValue;
			}
		}

		#endregion

		void IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			if (context.controlIteration && context.controlRecordingIteration)
			{
				mutations = null;

				dataSets.Clear();
				mutableItems.Clear();

				mutationScopeGlobal = new MutationScope("All");
				mutationScopeState = new List<MutationScope>();
				mutationScopeAction = new List<MutationScope>();
			}
			else
			{
				// Random.Next() Doesn't include max and we want it to
				var fieldsToMutate = Random.Next(1, maxFieldsToMutate + 1);

				// Our scope choice should auto-weight to Action, State, All
				// the more states and actions the less All will get chosen
				// might need to improve this in the future.
				var scope = Random.Choice(mutableItems);

				logger.Trace("SCOPE: {0}", scope.Name);

				mutations = Random.Sample(scope, fieldsToMutate);

				//if (context.test.stateModel.states.Count > 1 && stateMutators.Count > 0)
				//{
				//	var rnd = Random.Next(context.test.stateModel.states.Count);
				//	// TODO: weight state mutations better
				//	if (rnd == 0)
				//		_stateModelMutation = Random.Choice(stateMutators);
				//}
			}
		}

		void IterationFinished(RunContext context, uint currentIteration)
		{
			if (context.controlIteration && context.controlRecordingIteration)
			{
				// Add mutations scoped by state, where there is at least one available
				// mutation and more than one action scopes.  If there is only one action
				// scope then the state scope and action scope are identical.
				mutableItems.AddRange(mutationScopeState.Where(m => m.Count > 0 && m.ChildScopes > 1));

				// Add mutations scoped by action only for actions that have
				// mutable items.
				mutableItems.AddRange(mutationScopeAction.Where(m => m.Count > 0));

				// If there is only a single mutable action, it means global scope
				// should be the same as the single action
				if (mutableItems.Count == 1)
				{
					// No states should have contributed to mutableItems
					System.Diagnostics.Debug.Assert(mutationScopeState.Where(m => m.Count > 0 && m.ChildScopes > 1).Count() == 0);
					// The sum of mutations should be the same
					System.Diagnostics.Debug.Assert(mutableItems.Select(m => m.Count).Sum() == mutationScopeGlobal.Count);
					// Clear mutable items since global is the same as the single action
					mutableItems.Clear();
				}
				else
				{
					// Sort the list
					mutableItems.Sort();
				}

				// Add global scope 1st
				mutableItems.Insert(0, mutationScopeGlobal);

				// Cleanup containers used to collect the different scopes
				mutationScopeGlobal = null;
				mutationScopeAction = null;
				mutationScopeState = null;

				// TODO: Calculate weights of mutations at each scope
			}
		}

		void StateModelStarting(RunContext context, StateModel stateModel)
		{
			//if (_stateModelMutation != null)
			//	_stateModelMutation.randomMutation(stateModel);
		}

		//Dom.Action _currentAction;
		//string _currentActionName;

		void ActionStarting(RunContext context, Dom.Action action)
		{
			//_currentAction = action;

			// Is this a supported action?
			if (!action.outputData.Any())
				return;

			if (context.controlIteration && context.controlRecordingIteration)
			{
				//_currentActionName = _currentState.name + "." + _currentAction.name;
				//_iterationsByAction[_currentActionName] = new Iterations();
				
				RecordDataSet(action);
				SyncDataSet(action);
				RecordDataModel(action);
			}
			else if (!context.controlIteration)
			{
				MutateDataModel(action);
			}
		}

		//State _currentState;

		void StateStarting(RunContext context, State state)
		{
			//_currentState = state;

			if (context.controlIteration && _context.controlRecordingIteration)
			{
				var name = "Run_{0}.{1}".Fmt(state.runCount, state.name);
				var scope = new MutationScope(name);
				mutationScopeState.Add(scope);
			}

//			_iterationsByState[state.name] = new Iterations();
		}

		#region DataSet Tracking And Switching

		private uint GetSwitchIteration()
		{
			// Returns the iteration we should switch our dataSet based off our
			// current iteration. For example, if switchCount is 10, this function
			// will return 1, 11, 21, 31, 41, 51, etc.
			var ret = Iteration - ((Iteration - 1) % switchCount);
			return ret;
		}

		private void SeedRandomDataSet()
		{
			if (lastSwitchIteration != Iteration &&
				Iteration == GetSwitchIteration() &&
				dataSets.Where(d => d.Options.Count > 1).Any())
			{
				logger.Debug("Switch iteration, setting controlIteration and controlRecordingIteration.");

				// Only enable switch iteration if there is at least one data set
				// with two or more options.
				randomDataSet = null;
			}

			if (randomDataSet == null)
			{
				randomDataSet = new Random(this.Seed + GetSwitchIteration());

				Context.controlIteration = true;
				Context.controlRecordingIteration = true;
				lastSwitchIteration = Iteration;
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
					if (!dataSets.Contains(item.modelName))
						dataSets.Add(new DataSetTracker(item.modelName, options));
				}
			}
		}

		private void SyncDataSet(Dom.Action action)
		{
			System.Diagnostics.Debug.Assert(Iteration != 0);

			// Compute the iteration we need to switch on
			var switchIteration = GetSwitchIteration();

			foreach (var item in action.outputData)
			{
				// Note: use the model name, not the instance name so
				// we only set the data set once for re-enterant states.
				var modelName = item.modelName;

				DataSetTracker val;
				if (!dataSets.TryGetValue(modelName, out val))
					return;

				// If the last switch was within the current iteration range then we don't have to switch.
				if (switchIteration == val.Iteration)
					return;

				// Don't switch files if we are only using a single file :)
				if (val.Options.Where(x => !x.Ignore).Count() < 2)
					return;

				do
				{
					var opt = randomDataSet.Choice(val.Options);

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

		#endregion

		private void RecordDataModel(Dom.Action action)
		{
			var scopeState = mutationScopeState.Last();

			var name = "Run_{0}.{1}.{2}".Fmt(action.parent.runCount, action.parent.name, action.name);
			var scopeAction = new MutationScope(name);

			foreach (var item in action.outputData)
			{
				var allElements = new List<DataElement>();
				RecursevlyGetElements(item.dataModel, allElements);

				foreach (var elem in allElements)
				{
					var rec = new MutableItem(item.instanceName, elem.fullName);

					rec.Mutators.AddRange(dataMutators
						.Where(m => SupportedDataElement(m, elem))
						.Select(m => GetMutatorInstance(m, elem)));

					if (rec.Mutators.Count > 0)
					{
						mutationScopeGlobal.Add(rec);
						scopeState.Add(rec);
						scopeAction.Add(rec);
					}
				}
			}

			mutationScopeAction.Add(scopeAction);

			// If the action scope has mutable items, then the
			// state scope has a valid child scope.  This is
			// used later to prune empty state scopes.
			if (scopeAction.Count > 0)
				scopeState.ChildScopes += 1;
		}

		private void ApplyMutation(ActionData data)
		{
			var instanceName = data.instanceName;

			foreach (var item in mutations)
			{
				if (item.InstanceName != instanceName)
					continue;

				var elem = data.dataModel.find(item.ElementName);
				if (elem != null && elem.mutationFlags == MutateOverride.None)
				{
					var mutator = Random.Choice(item.Mutators);
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
			System.Diagnostics.Debug.Assert(Iteration > 0);

			foreach (var item in action.outputData)
			{
				ApplyMutation(item);
			}
		}

		public override State MutateChangingState(State state)
		{
	////		if (_context.controlIteration)
	//			return state;

	//		if(_stateModelMutation != null)
	//		{
	//			Context.OnStateMutating(state, _stateModelMutation);

	//			logger.Debug("MutateChangingState: Fuzzing state change: {0}", state.name);
	//			logger.Debug("MutateChangingState: Mutator: {0}", _stateModelMutation.name);

	//			return _stateModelMutation.changeState(_currentState, _currentAction, state);
	//		}

			return state;
		}

		public override Dom.Action NextAction(State state, Dom.Action lastAction, Dom.Action nextAction)
		{
	//		if (_stateModelMutation != null)
	//			return _stateModelMutation.nextAction(state, lastAction, nextAction);

			return nextAction;
		}
	}
}

// end
