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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using NLog;

namespace Peach.Core
{
	/// <summary>
	/// Methods for finding and creating instances of 
	/// classes.
	/// </summary>
	public static class ClassLoader
	{
		static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();
		static readonly object _mutex = new object();
		internal static readonly HashSet<Assembly> AssemblyCache = new HashSet<Assembly>();
		static readonly Dictionary<Type, object[]> AttributeCache = new Dictionary<Type, object[]>();
		static readonly Dictionary<Type, IEnumerable<Type>> AllByAttributeCache = new Dictionary<Type, IEnumerable<Type>>();

		public static void Initialize(params string[] pluginsPaths)
		{
			var scripting = new PythonScripting();
			scripting.AddSearchPath(Path.Combine(Utilities.ExecutionDirectory, "Lib"));

			foreach (var path in pluginsPaths)
			{
				if (Directory.Exists(path))
					LoadPlugins(path, scripting);
			}

			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				var attr = asm.GetCustomAttribute<PluginAssemblyAttribute>();
				if (attr != null)
				{
					logger.Trace("Loading plugins from: {0}", asm.FullName);
					AssemblyCache.Add(asm);
				}
			}
		}

		private static void LoadPlugins(string pluginsPath, PythonScripting scripting)
		{
			foreach (var file in Directory.GetFiles(pluginsPath))
			{
				if (!file.EndsWith(".exe") && !file.EndsWith(".dll"))
					continue;

				logger.Trace("Loading plugins from: {0}", file);

				try
				{
					var asm = Load(file);
					asm.GetTypes(); // make sure we can load exported types.
					AssemblyCache.Add(asm);
				}
				catch (Exception ex)
				{
					logger.Trace("ClassLoader skipping \"{0}\", {1}", file, ex.Message);
				}
			}

			var pys = Directory.GetFiles(pluginsPath, "*.py");
			if (pys.Any())
				scripting.AddSearchPath(pluginsPath);

			foreach (var py in pys)
			{
				try
				{
					scripting.ImportModule(Path.GetFileNameWithoutExtension(py));
				}
				catch (Exception ex)
				{
					logger.Warn("ClassLoader skipping \"{0}\", {1}", py, ex.Message);
				}
			}
		}

		static Assembly Load(string fullPath)
		{
			// Do this so we get a consistent error message across versions of .NET and mono.
			if (!File.Exists(fullPath))
				throw new FileNotFoundException("The file \"" + fullPath + "\" does not exist.");

			/*
			 * Assembly.LoadFrom can fail if the security zone of the assembly is
			 * not MyComputer (0).  The call will succeed regardless of security zone
			 * if the assembly was directly linked to the program.
			 * 
			 * Instead of trying to catch this error from Assembly.LoadFrom and
			 * rewrite the appropriate ADS (AssemblyName.dll:Zone.Identifier) with the
			 * contents "[ZoneTransfer]\r\nZoneId=0\r\n" it is far easier to have each
			 * program have an entry in their app.config that enables loading
			 * of untrusted assemblies.
			 * 
			 * Put the following in the app.config:
			 * 
			 * <configuration>
			 *   <runtime>
			 *     <loadFromRemoteSources enabled="true"/>
			 *   </runtime>
			 * </configuration>
			 * 
			 * and the zone security settings will be ignored by the .NET runtime.
			 */

			return Assembly.LoadFrom(fullPath);
		}

		/// <summary>
		/// Extension to the Type class. Return default plugin attribute
		/// matching the specified type or null if not found
		/// </summary>
		/// <typeparam name="TAttr">Attribute type to find.</typeparam>
		/// <param name="type">Type in which the search should run over.</param>
		/// <returns>A generator which yields the attributes specified.</returns>
		public static TAttr GetDefaultAttr<TAttr>(this Type type)
			where TAttr : PluginAttribute
		{
			return GetCustomAttributes(type).OfType<TAttr>().FirstOrDefault(a => a.IsDefault);
		}

		/// <summary>
		/// Extension to the Type class. Return all attributes matching the specified type.
		/// </summary>
		/// <typeparam name="TAttr">Attribute type to find.</typeparam>
		/// <param name="type">Type in which the search should run over.</param>
		/// <returns>A generator which yields the attributes specified.</returns>
		public static IEnumerable<TAttr> GetAttributes<TAttr>(this Type type)
			where TAttr : Attribute
		{
			return GetCustomAttributes(type).OfType<TAttr>();
		}

		/// <summary>
		/// Extension to the Type class. Return all attributes matching the specified type and predicate.
		/// </summary>
		/// <typeparam name="TAttr">Attribute type to find.</typeparam>
		/// <param name="type">Type in which the search should run over.</param>
		/// <param name="predicate">Returns an attribute if the predicate returns true or the predicate itself is null.</param>
		/// <returns>A generator which yields the attributes specified.</returns>
		public static IEnumerable<TAttr> GetAttributes<TAttr>(this Type type, Func<Type, TAttr, bool> predicate)
			where TAttr : Attribute
		{
			foreach (var attr in GetCustomAttributes(type).OfType<TAttr>())
			{
				if (predicate == null || predicate(type, attr))
				{
					yield return attr;
				}
			}
		}

		/// <summary>
		/// Finds all types that are decorated with the specified Attribute type.
		/// </summary>
		/// <typeparam name="TAttr">Attribute type to find.</typeparam>
		/// <returns>A generator which yields KeyValuePair elements of custom attribute and type found.</returns>
		public static IEnumerable<KeyValuePair<TAttr, Type>> GetAllByAttribute<TAttr>()
			where TAttr : Attribute
		{
			return GetAllByAttribute<TAttr>(null);
		}

		/// <summary>
		/// Finds all types that are decorated with the specified Attribute type and matches the specified predicate.
		/// </summary>
		/// <typeparam name="A">Attribute type to find.</typeparam>
		/// <param name="predicate">Returns a value if the predicate returns true or the predicate itself is null.</param>
		/// <returns>A generator which yields KeyValuePair elements of custom attribute and type found.</returns>
		public static IEnumerable<KeyValuePair<A, Type>> GetAllByAttribute<A>(Func<Type, A, bool> predicate)
			where A : Attribute
		{
			IEnumerable<Type> types;
			lock (_mutex)
			{
				if (!AllByAttributeCache.TryGetValue(typeof(A), out types))
				{
					var typesList = new List<Type>();
					foreach (var asm in AssemblyCache)
					{
						foreach (var type in asm.GetTypes())
						{
							if (!type.IsClass || (!type.IsPublic && !type.IsNestedPublic))
								continue;

							if (type.GetCustomAttributes<A>().Any())
								typesList.Add(type);
						}
					}

					AllByAttributeCache.Add(typeof(A), typesList);
					types = typesList;
				}
			}

			foreach (var type in types)
			{
				foreach (var x in type.GetAttributes<A>(predicate))
				{
					yield return new KeyValuePair<A, Type>(x, type);
				}
			}
		}

		/// <summary>
		/// Finds all types that are decorated with the specified Attribute type and matches the specified predicate.
		/// </summary>
		/// <typeparam name="A">Attribute type to find.</typeparam>
		/// <param name="predicate">Returns a value if the predicate returns true or the predicate itself is null.</param>
		/// <returns>A generator which yields elements of the type found.</returns>
		public static IEnumerable<Type> GetAllTypesByAttribute<A>(Func<Type, A, bool> predicate)
			where A : Attribute
		{
			return GetAllByAttribute<A>(predicate).Select(x => x.Value);
		}

		/// <summary>
		/// Finds the first type that matches the specified query.
		/// </summary>
		/// <typeparam name="A">Attribute type to find.</typeparam>
		/// <param name="predicate">Returns a value if the predicate returns true or the predicate itself is null.</param>
		/// <returns>KeyValuePair of custom attribute and type found.</returns>
		public static KeyValuePair<A, Type> FindByAttribute<A>(Func<Type, A, bool> predicate)
			where A : Attribute
		{
			return GetAllByAttribute<A>(predicate).FirstOrDefault();
		}

		/// <summary>
		/// Finds the first type that matches the specified query.
		/// </summary>
		/// <typeparam name="A">Attribute type to find.</typeparam>
		/// <param name="predicate">Returns a value if the predicate returns true or the predicate itself is null.</param>
		/// <returns>Returns only the Type found.</returns>
		public static Type FindTypeByAttribute<A>(Func<Type, A, bool> predicate)
			where A : Attribute
		{
			return GetAllByAttribute<A>(predicate).FirstOrDefault().Value;
		}

		/// <summary>
		/// Finds the first type that matches the specified query.
		/// </summary>
		/// <typeparam name="TAttr">PluginAttribute type to find.</typeparam>
		/// <param name="name">The name of the plugin to search for.</param>
		/// <returns>Returns the Type found or null if not found.</returns>
		public static Type FindPluginByName<TAttr>(string name)
			where TAttr : PluginAttribute
		{
			return GetAllByAttribute<TAttr>(
				(t, a) =>
					a.Name == name ||
					t.GetAttributes<AliasAttribute>().Any(x => x.Name == name))
				.FirstOrDefault().Value;
		}

		/// <summary>
		/// Find and create and instance of class by parent type and 
		/// name.
		/// </summary>
		/// <typeparam name="T">Return Type.</typeparam>
		/// <param name="name">Name of type.</param>
		/// <returns>Returns a new instance of found type, or null.</returns>
		public static T FindAndCreateByTypeAndName<T>(string name)
			where T : class
		{
			lock (_mutex)
			{
				foreach (var asm in AssemblyCache)
				{
					//if (asm.IsDynamic)
					//	continue;

					var type = asm.GetType(name);
					if (type == null)
						continue;

					if (!type.IsClass || (!type.IsPublic && !type.IsNestedPublic))
						continue;

					if (!type.IsSubclassOf(type))
						continue;

					return Activator.CreateInstance(type) as T;
				}
			}

			return null;
		}

		static object[] GetCustomAttributes(Type type)
		{
			lock (_mutex)
			{
				object[] attrs;

				if (AttributeCache.TryGetValue(type, out attrs))
					return attrs;

				try
				{
					attrs = type.GetCustomAttributes(true);
				}
				catch (TypeLoadException)
				{
					attrs = new object[0];
				}

				AttributeCache.Add(type, attrs);

				return attrs;
			}
		}
	}
}
