using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.MutationStrategies
{
	public abstract class WeightedMutationStrategy : MutationStrategy
	{
		[DebuggerDisplay("{InstanceName} {ElementName} Mutators = {Mutators.Count}")]
		protected class MutableItem : INamed, IWeighted
		{
			#region Obsolete Functions

			[Obsolete("This property is obsolete and has been replaced by the Name property.")]
			public string name { get { return Name; } }

			#endregion

			public MutableItem(string instanceName, string elementName, ElementWeight weight)
				: this(instanceName, elementName, new Mutator[0])
			{
				Weight = (int)weight;
			}

			public MutableItem(string instanceName, string elementName, ICollection<Mutator> mutators)
			{
				InstanceName = instanceName;
				ElementName = elementName;
				Mutators = new WeightedList<Mutator>(mutators);
				Weight = 1;
			}

			public string Name { get { return InstanceName; } }
			public int Weight { get; set; }
			public string InstanceName { get; private set; }
			public string ElementName { get; private set; }
			public WeightedList<Mutator> Mutators { get; private set; }
			public int SelectionWeight { get { return Mutators.SelectionWeight; } }

			public int TransformWeight(Func<int, int> how)
			{
				return Weight * Mutators.TransformWeight(how);
			}
		}

		[DebuggerDisplay("{Name} Count = {Count}")]
		[DebuggerTypeProxy(typeof(DebugView))]
		protected class MutationScope : WeightedList<MutableItem>
		{
			class DebugView
			{
				readonly MutationScope _obj;

				public DebugView(MutationScope obj)
				{
					_obj = obj;
				}

				[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
				public MutableItem[] Items
				{
					get { return _obj.ToArray(); }
				}
			}

			public MutationScope(string name)
			{
				Name = name;
			}

			public MutationScope(string name, IEnumerable<MutableItem> collection)
				: base(collection)
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
		}

		/// <summary>
		/// Maximum number of fields to mutate at once.
		/// </summary>
		public int MaxFieldsToMutate
		{
			get;
			set;
		}

		protected WeightedMutationStrategy(Dictionary<string, Variant> args)
			: base(args)
		{
			MaxFieldsToMutate = 6;
		}

		protected int GetMutationCount()
		{
			while (true)
			{
				// For half bell curves, sigma should be 1/3 of our range
				var sigma = MaxFieldsToMutate / 3.0;

				var num = Random.NextGaussian(0, sigma);

				// Only want half a bell curve
				num = Math.Abs(num);

				var asInt = (int)Math.Floor(num) + 1;

				if (asInt > MaxFieldsToMutate)
					continue;

				return asInt;
			}
		}
	}
}
