using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Peach.Core
{
	public static class Extensions
	{
		public static T GetStaticField<T>(this Type type, string name)
		{
			var bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy;
			var field = type.GetField(name, bindingAttr);
			var ret = (T)field.GetValue(null);
			return ret;
		}

		public static T WeightedChoice<T>(this Random rng, WeightedList<T> list) where T : IWeighted
		{
			// returns between 0 <= x < UpperBound
			var val = rng.Next(list.Max);

			// Finds first element with sum-weight greater than value
			var ret = list.UpperBound(val);

			return ret.Value;
		}

		public static T[] WeightedSample<T>(this Random rng, WeightedList<T> list, int count) where T : IWeighted
		{
			// Shrink count so that we return the list
			// in a weighted order.
			if (count > list.Count)
				count = list.Count;

			var max = list.Max;
			var ret = new List<T>();
			var conversions = new SortedList<long, Func<long, long>>();

			for (int i = 0; i < count; ++i)
			{
				var rand = rng.Next(max);

				foreach (var c in conversions)
					rand = c.Value(rand);

				var kv = list.UpperBound(rand);
				var item = kv.Value;
				var weight = item.SelectionWeight;
				var lowerBound = kv.Key - weight;

				conversions.Add(kv.Key, (c) => {
					if (c >= lowerBound)
						return c + weight;
					else
						return c;
				});

				System.Diagnostics.Debug.Assert(!ret.Contains(item));
				System.Diagnostics.Debug.Assert(max >= weight);

				ret.Add(item);
				max -= weight;
			}

			return ret.ToArray();
		}

	}
}
