using System;
using System.Collections.Generic;
using System.Linq;
using Peach.Core;
using Peach.Core.Dom;
using Logger = NLog.Logger;

namespace Peach.Pro.Core.MutationStrategies
{
	[MutationStrategy("WebProxy")]
	[Serializable]
	public class WebProxyStrategy : WeightedMutationStrategy
	{
		private static readonly Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		private readonly List<Type> _dataMutators = new List<Type>();

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

			_dataMutators.AddRange(
				EnumerateValidMutators()
					.Where(m => m.GetStaticField<bool>("affectDataModel")));
		}

		public void Mutate(ActionData data)
		{
			SeedRandom();

			var scope = RecordDataModel(data);
			var fieldsToMutate = GetMutationCount();
			var mutations = Random.WeightedSample(scope, fieldsToMutate);

			foreach (var mutation in mutations)
			{
				ApplyMutation(data, mutation);
			}
		}

		private MutationScope RecordDataModel(ActionData data)
		{
			var scope = new MutationScope("All");

			foreach (var elem in data.dataModel.PreOrderTraverse())
			{
				if (elem.Weight == ElementWeight.Off)
					continue;

				var rec = new MutableItem(scope.Name, elem.fullName, elem.Weight);
				var e = elem;

				rec.Mutators.AddRange(_dataMutators
					.Where(m => SupportedDataElement(m, e))
					.Select(m => GetMutatorInstance(m, e))
					.Where(m => m.SelectionWeight > 0));

				if (rec.Mutators.Count > 0)
				{
					scope.Add(rec);
				}
			}

			return scope;
		}

		private void ApplyMutation(ActionData data, MutableItem mutation)
		{
			var elem = data.dataModel.find(mutation.ElementName);
			if (elem != null && elem.mutationFlags == MutateOverride.None)
			{
				var mutator = Random.WeightedChoice(mutation.Mutators);
				Context.OnDataMutating(data, elem, mutator);
				Logger.Debug("Action_Starting: Fuzzing: {0}", mutation.ElementName);
				Logger.Debug("Action_Starting: Mutator: {0}", mutator.Name);
				mutator.randomMutation(elem);
			}
			else
			{
				Logger.Debug("Action_Starting: Skipping Fuzzing: {0}", mutation.ElementName);
			}
		}
	}
}
