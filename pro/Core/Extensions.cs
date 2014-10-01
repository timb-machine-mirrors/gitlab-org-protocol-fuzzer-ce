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
	}
}
