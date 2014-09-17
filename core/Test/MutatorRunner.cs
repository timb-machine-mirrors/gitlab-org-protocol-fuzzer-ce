using System;
using System.Collections.Generic;
using System.Linq;

using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Core.Test
{
	public class MutatorRunner
	{
		public interface Mutation
		{
			BitwiseStream Value { get; }
		}

		class SequentialMutation : Mutation
		{
			Runner runner;
			int mutation;
			DataElement value;

			public SequentialMutation(Runner runner, int mutation)
			{
				this.runner = runner;
				this.mutation = mutation;
			}

			public BitwiseStream Value
			{
				get
				{
					if (value == null)
					{
						runner.Iteration = (uint)mutation + 1;
						runner.Mutator.mutation = (uint)mutation;
						value = runner.Element.root.Clone();
						Mutate(runner.Mutator, value.find(runner.Element.fullName));
					}

					return value.Value;
				}
			}

			protected virtual void Mutate(Mutator mutator, DataElement obj)
			{
				mutator.sequentialMutation(obj);
			}
		}

		class RandomMutation : SequentialMutation
		{
			public RandomMutation(Runner runner, int mutation)
				: base(runner, mutation)
			{
			}

			protected override void Mutate(Mutator mutator, DataElement obj)
			{
				mutator.randomMutation(obj);
			}
		}

		class Runner : MutationStrategy
		{
			uint iteration;

			public Mutator Mutator { get; set; }
			public DataElement Element { get; set; }

			public Runner(Type type, DataElement element)
				: base(null)
			{
				Context = new RunContext() { config = new RunConfiguration() };
				Element = element;

				if (type != null)
				{
					Mutator = (Mutator)Activator.CreateInstance(type, element);
					Mutator.context = this;
				}
			}

			public override bool UsesRandomSeed
			{
				get { throw new NotImplementedException(); }
			}

			public override bool IsDeterministic
			{
				get { throw new NotImplementedException(); }
			}

			public override uint Count
			{
				get { throw new NotImplementedException(); }
			}

			public override uint Iteration
			{
				get
				{
					return iteration;
				}
				set
				{
					iteration = value;
					SeedRandom();
				}
			}

			public bool IsSupported(Type mutator, DataElement elem)
			{
				return SupportedDataElement(mutator, elem);
			}
		}

		Runner runner;
		Type type;

		public MutatorRunner(string name)
		{
			runner = new Runner(null, null);
			type = ClassLoader.GetAllTypesByAttribute<MutatorAttribute>((t, a) => a.Name == name).FirstOrDefault();

			if (type == null)
				throw new ArgumentException("Could not find mutator named '{0}'.".Fmt(name));
		}

		public bool IsSupported(DataElement element)
		{
			return runner.IsSupported(type, element);
		}

		public IEnumerable<Mutation> Sequential(DataElement element)
		{
			var strategy = new Runner(type, element);

			for (int i = 0; i < strategy.Mutator.count; ++i)
				yield return new SequentialMutation(strategy, i);
		}

		public IEnumerable<Mutation> Random(int count, DataElement element)
		{
			var strategy = new Runner(type, element);

			for (int i = 0; i < count; ++i)
				yield return new RandomMutation(strategy, i);
		}
	}
}
