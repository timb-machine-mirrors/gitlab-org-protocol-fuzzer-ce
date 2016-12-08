using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Peach.Core;
using Peach.Core.Dom;
using Action = Peach.Core.Dom.Action;
using Logger = NLog.Logger;

namespace Peach.Pro.Core.MutationStrategies
{
	[MutationStrategy("WebProxy")]
	[Serializable]
	public class WebProxyStrategy : WeightedMutationStrategy
	{
		private static readonly Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		private uint _iteration;

		public WebProxyStrategy(Dictionary<string, Variant> args)
			: base(args)
		{
			MaxFieldsToMutate = 2;
		}

		public bool ControlRecordIteration { get; set; }

		public override bool UsesRandomSeed
		{
			get { return true; }
		}

		public override bool IsDeterministic
		{
			get { return false; }
		}

		public override uint Count
		{
			get { return uint.MaxValue; }
		}

		public override uint Iteration
		{
			get
			{
				return _iteration;
			}
			set
			{
				Context.controlIteration |= ControlRecordIteration;
				Context.controlRecordingIteration |= ControlRecordIteration;

				ControlRecordIteration = false;
				_iteration = value;
			}
		}

		public override void Initialize(RunContext context, Engine engine)
		{
			base.Initialize(context, engine);

			dataMutators.AddRange(
				EnumerateValidMutators()
					.Where(m => m.GetStaticField<bool>("affectDataModel")));
		}

		public void StateModelStarting(RunContext context)
		{
			SeedRandom();

			// Reset per-iteration state
			mutations = null;
			mutationHistory.Clear();

			if (context.controlIteration && context.controlRecordingIteration)
			{
				mutableItems.Clear();

				mutationScopeGlobal = new MutationScope("All");
				mutationScopeState = new List<MutationScope>();
				mutationScopeAction = new List<MutationScope>();
			}
			else
			{
				// Random.Next() Doesn't include max and we want it to
				var fieldsToMutate = GetMutationCount();

				if (mutableItems.Count == 0)
				{
					Logger.Trace("No mutable items.");
					mutations = new MutableItem[0];
				}
				else
				{
					// Our scope choice should auto-weight to Action, State, All
					// the more states and actions the less All will get chosen
					// might need to improve this in the future.
					var scope = Random.WeightedChoice(mutableItems);

					Logger.Trace("SCOPE: {0}", scope.Name);

					mutations = Random.WeightedSample(scope, fieldsToMutate);
				}
			}
		}

		public void StateModelFinished(RunContext context)
		{
			if (context.controlIteration && context.controlRecordingIteration)
			{
				// Build the final list of mutable items
				// NOTE: We can't alter the contents of a scope after adding it to
				// mutableItems because the weights won't be updated.

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
					Debug.Assert(!mutationScopeState.Any(m => m.Count > 0 && m.ChildScopes > 1));
					// The sum of mutations should be the same
					Debug.Assert(mutableItems.Select(m => m.Count).Sum() == mutationScopeGlobal.Count);
					// Clear mutable items since global is the same as the single action
					mutableItems.Clear();
				}

				// Add global scope 1st
				if (mutationScopeGlobal.Count > 0)
					mutableItems.Add(mutationScopeGlobal);

				mutableItems.TransformWeight(_tuneWeights);

				// Cleanup containers used to collect the different scopes
				mutationScopeGlobal = null;
				mutationScopeAction = null;
				mutationScopeState = null;
			}
		}

		public void ActionStarting(RunContext context, Action action)
		{
			// Is this a supported action?
			if (!action.outputData.Any())
				return;

			if (context.controlIteration && context.controlRecordingIteration)
			{
				var state = action.parent;
				var name = "Run_{0}.{1}".Fmt(state.runCount, state.Name);
				var scope = new MutationScope(name);
				mutationScopeState.Add(scope);

				RecordDataModel(action);
			}
			else if (!context.controlIteration)
			{
				MutateDataModel(action);
			}
		}

		#region Dumb Proxy Fuzzing (No Test Events)

		public void Mutate(Action action)
		{
			SeedRandom();

			var scope = RecordDataModels(action);
			var fieldsToMutate = GetMutationCount();
			mutations = Random.WeightedSample(scope, fieldsToMutate);
			MutateDataModel(action);
		}

		private MutationScope RecordDataModels(Action action)
		{
			var scope = new MutationScope("All");

			foreach (var data in action.outputData)
			{
				foreach (var elem in data.dataModel.PreOrderTraverse())
				{
					if (elem.Weight == ElementWeight.Off)
						continue;

					var rec = new MutableItem(data.instanceName, elem.fullName, elem.Weight);
					var e = elem;

					rec.Mutators.AddRange(dataMutators
						.Where(m => SupportedDataElement(m, e))
						.Select(m => GetMutatorInstance(m, e))
						.Where(m => m.SelectionWeight > 0));

					if (rec.Mutators.Count > 0)
					{
						scope.Add(rec);
					}
				}
			}

			return scope;
		}

		#endregion
	}
}
