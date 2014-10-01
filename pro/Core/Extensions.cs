using System;
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

			return ret;
		}
	}
}
