using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Net;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;

namespace Peach.Core
{
	public static class ParameterParser
	{
		/// <summary>
		/// Parses a dictionary of arguments, similiar to python kwargs.
		/// For each parameter attribute on 'T', the appropriate property
		/// on 'obj' will be set. Eg, given integer parameter 'option1':
		/// obj.option1 = int.Parse(args["option1"])
		/// </summary>
		/// <typeparam name="T">Class type</typeparam>
		/// <param name="obj">Instance of class T</param>
		/// <param name="args">Dictionary of arguments</param>
		public static void Parse<T>(T obj, Dictionary<string, string> args) where T : class
		{
			foreach (var item in GetProperties(obj))
			{
				var attr = item.Key;
				var prop = item.Value;

				string value;

				if (args.TryGetValue(attr.name, out value))
					ApplyProperty(obj, prop, attr, value);
				else if (!attr.required)
					ApplyProperty(obj, prop, attr, attr.defaultValue);
				else if (attr.required)
					RaiseError(obj.GetType(), "is missing required parameter '{0}'.", attr.name);
			}
		}

		/// <summary>
		/// Parses a dictionary of arguments, similiar to python kwargs.
		/// For each parameter attribute on 'T', the appropriate property
		/// on 'obj' will be set. Eg, given integer parameter 'option1':
		/// obj.option1 = int.Parse(args["option1"])
		/// </summary>
		/// <typeparam name="T">Class type</typeparam>
		/// <param name="obj">Instance of class T</param>
		/// <param name="args">Dictionary of arguments</param>
		public static void Parse<T>(T obj, Dictionary<string, Variant> args) where T : class
		{
			foreach (var item in GetProperties(obj))
			{
				var attr = item.Key;
				var prop = item.Value;

				Variant value;

				if (args.TryGetValue(attr.name, out value))
					ApplyProperty(obj, prop, attr, (string)value);
				else if (!attr.required)
					ApplyProperty(obj, prop, attr, attr.defaultValue);
				else if (attr.required)
					RaiseError(obj.GetType(), "is missing required parameter '{0}'.", attr.name);
			}
		}

		/// <summary>
		/// Will convert a string value to the type described in the ParameterAttribute.
		/// If an appropriate conversion function can not be found, this function will
		/// look for a static method on 'type' to perform the conversion.  For example,
		/// if the attribute type was class 'SomeClass', the function signature would be:
		/// static void Parse(string str, out SomeClass val)
		/// 
		/// If the value is string.Empty and the destination type is nullable, the value
		/// null will be returned.
		/// </summary>
		/// <param name="type">Object type that is decorated with the Parameter attribute.</param>
		/// <param name="attr">Parameter attribute describing the destination type.</param>
		/// <param name="value">String value to convert.</param>
		/// <returns></returns>
		public static object FromString(Type type, ParameterAttribute attr, string value)
		{
			return FromString(attr, type, attr.type, attr.name, value);
		}

		public static IEnumerable<KeyValuePair<string, string>> Get<T>(T obj) where T : class
		{
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var item in GetProperties(obj))
			{
				var attr = item.Key;
				var prop = item.Value;

				var value = prop.GetValue(obj, null).ToString();

				if (attr.required || attr.defaultValue != value)
					yield return new KeyValuePair<string, string>(attr.name, value);
			}
		}

		private static IEnumerable<KeyValuePair<ParameterAttribute, PropertyInfo>> GetProperties<T>(T obj)
			where T : class
		{
			var type = obj.GetType();

			foreach (var attr in obj.GetType().GetAttributes<ParameterAttribute>(null))
			{
				const BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance;

				var prop = obj.GetType().GetProperty(attr.name, bindingAttr, null, attr.type, new Type[0], null)
					?? obj.GetType().GetProperty("_" + attr.name, bindingAttr, null, attr.type, new Type[0], null);

				if (prop == null)
					RaiseError(type, "has no property for parameter '{0}'.", attr.name);
				else if (!prop.CanWrite)
					RaiseError(type, "has no settable property for parameter '{0}'.", attr.name);
				else
					yield return new KeyValuePair<ParameterAttribute, PropertyInfo>(attr, prop);
			}

		}

		private static object FromString(
			ParameterAttribute attr,
			Type pluginType,
			Type destType,
			string name,
			string value)
		{
			object val = null;

			if (destType.IsArray)
			{
				if (destType.GetArrayRank() != 1)
					throw new NotSupportedException();

				var delim = attr.ListDelimiter;
				var parts = value.Split(new[] { delim }, StringSplitOptions.RemoveEmptyEntries);
				var array = (IList)Activator.CreateInstance(destType, new object[] { parts.Length });
				var elemType = destType.GetElementType();

				for (var i = 0; i < parts.Length; ++i)
				{
					array[i] = FromString(attr, pluginType, elemType, name, parts[i]);
				}

				return array;
			}

			var nullable = !destType.IsValueType;

			if (destType.IsGenericType && destType.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				destType = destType.GetGenericArguments()[0];
				nullable = true;
			}

			if (value == string.Empty)
			{
				if (!nullable)
					RaiseError(pluginType, "could not set value type parameter '{0}' to 'null'.", name);
			}
			else
			{
				try
				{
					if (destType == typeof(IPAddress))
						val = IPAddress.Parse(value);
					else if (destType == typeof(Regex))
						val = ParseRegex(value);
					else if (destType.IsEnum)
						val = Enum.Parse(destType, value, true);
					else
						val = ChangeType(pluginType, value, destType);
				}
				catch (Exception ex)
				{
					RaiseError(ex, pluginType, "could not set parameter '{0}'.  {1}", name, ex.Message);
				}
			}

			return val;
		}

		private static void ApplyProperty<T>(T obj, PropertyInfo prop, ParameterAttribute attr, string value)
			where T : class
		{
			var type = obj.GetType();

			var val = FromString(type, attr, value);

			prop.SetValue(obj, val, null);
		}

		private static object ChangeType(Type ownerType, string value, Type destType)
		{
			if (Type.GetTypeCode(destType) != TypeCode.Object)
				return Convert.ChangeType(value, destType);

			// Look for a static Parse(string) on destType
			var method = destType.GetMethod(
				"Parse",
				BindingFlags.Public | BindingFlags.Static,
				Type.DefaultBinder,
				new[] { typeof(string) },
				null);

			if (method != null)
			{
				if (method.ReturnType != destType)
					method = null;
			}

			if (method == null)
			{
				// Find a converter on this type with the signature:
				// static void Parse(string str, out "type" val)
				const BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Static;
				var types = new[] { typeof(string), destType.MakeByRefType() };
				var level = ownerType;

				do
				{
					method = level.GetMethod("Parse", bindingAttr, Type.DefaultBinder, types, null);
					level = level.BaseType;

					if (method != null && (method.ReturnType != typeof(void) || !method.GetParameters()[1].IsOut))
						method = null;
				}
				while (method == null && level != null);

				if (method == null)
					throw new InvalidCastException("No suitable method exists for converting a string to " + destType.Name + ".");
			}

			try
			{
				if (method.ReturnType != typeof(void))
					return method.Invoke(null, new object[] { value });

				var parameters = new object[] { value, null };
				method.Invoke(null, parameters);
				return parameters[1];
			}
			catch (TargetInvocationException ex)
			{
				var baseEx = ex.GetBaseException();
				if (baseEx is ThreadAbortException)
					throw baseEx;

				var inner = ex.InnerException;
				if (inner == null)
					throw;

				var outer = (Exception)Activator.CreateInstance(inner.GetType(), inner.Message, inner);
				throw outer;
			}
		}

		private static Regex ParseRegex(string regex)
		{
			try
			{
				return new Regex(regex, RegexOptions.Multiline);
			}
			catch (Exception ex)
			{
				throw new ArgumentException("The value '{0}' is not a valid regular expression.".Fmt(regex), ex);
			}
		}

		private static void RaiseError(Type type, string fmt, params object[] args)
		{
			RaiseError(null, type, fmt, args);
		}

		private static void RaiseError(Exception ex, Type type, string fmt, params object[] args)
		{
			var attr = type.GetAttributes<PluginAttribute>().OrderBy(a => a.IsDefault).FirstOrDefault();

			var cls = "Class";
			var name = type.Name;

			if (attr != null)
			{
				name = attr.Name;
				cls = attr.Type.Name;

				if (attr.Type.IsInterface && cls.StartsWith("I"))
					cls = cls.Substring(1);
			}

			var msg = string.Format("{0} '{1}' {2}", cls, name, string.Format(fmt, args));
			throw new PeachException(msg, ex);
		}

	}
}