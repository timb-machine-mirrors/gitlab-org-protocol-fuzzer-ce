
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
using System.Collections.Generic;
using System.Text;
using System.Linq;
using IronPython;
using IronPython.Hosting;
using IronRuby;
using IronRuby.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Math;
using System.Reflection;
using System.IO;
using Peach.Core.IO;

namespace Peach.Core
{
	public class PythonScripting : Scripting
	{
		protected override ScriptEngine GetEngine()
		{
			// Need to add python stdlib to search path
			var engine = IronPython.Hosting.Python.CreateEngine();
			var paths = engine.GetSearchPaths();

			foreach (string path in ClassLoader.SearchPaths)
				paths.Add(Path.Combine(path, "Lib"));

			engine.SetSearchPaths(paths);

			return engine;
		}
	}

	public class RubyScripting : Scripting
	{
		protected override ScriptEngine GetEngine()
		{
			return IronRuby.Ruby.CreateEngine();
		}
	}

	/// <summary>
	/// Scripting class provides easy to use
	/// methods for using Python/Ruby with Peach.
	/// </summary>
	public abstract class Scripting
	{
		#region Private Members

		private Dictionary<string, object> modules;
		private ScriptEngine engine;

		#endregion

		#region Constructor

		public Scripting()
		{
			modules = new Dictionary<string, object>();
			engine = GetEngine();
		}

		#endregion

		#region Abstract Fucntions

		protected abstract ScriptEngine GetEngine();

		#endregion

		#region Module Imports

		public void ImportModule(string module)
		{
			if (!modules.ContainsKey(module))
				modules.Add(module, engine.ImportModule(module));
		}

		public IEnumerable<string> Modules
		{
			get
			{
				return modules.Keys;
			}
		}

		#endregion

		#region Search Paths

		public void AddSearchPath(string path)
		{
			var paths = engine.GetSearchPaths();

			if (!paths.Contains(path))
			{
				paths.Add(path);
				engine.SetSearchPaths(paths);
			}
		}

		public IEnumerable<string> Paths
		{
			get
			{
				return engine.GetSearchPaths();
			}
		}

		#endregion

		#region Exec & Eval

		public void Exec(string code, Dictionary<string, object> localScope)
		{
			var scope = CreateScope(localScope);

			try
			{
				scope.Engine.Execute(code, scope);
			}
			catch (Exception ex)
			{
				throw new PeachException("Error executing expression [" + code + "]: " + ex.ToString(), ex);
			}
			finally
			{
				CleanupScope(scope);
			}
		}

		public object Eval(string code, Dictionary<string, object> localScope)
		{
			var scope = CreateScope(localScope);

			try
			{
				var source = scope.Engine.CreateScriptSourceFromString(code, SourceCodeKind.Expression);
				var obj = source.Execute(scope);

				if (obj != null && obj.GetType() == typeof(BigInteger))
				{
					BigInteger bint = (BigInteger)obj;

					int i32;
					uint ui32;
					long i64;
					ulong ui64;

					if (bint.AsInt32(out i32))
						return i32;

					if (bint.AsInt64(out i64))
						return i64;

					if (bint.AsUInt32(out ui32))
						return ui32;

					if (bint.AsUInt64(out ui64))
						return ui64;
				}

				return obj;
			}
			catch (Exception ex)
			{
				throw new PeachException("Error executing expression [" + code + "]: " + ex.ToString(), ex);
			}
			finally
			{
				CleanupScope(scope);
			}
		}

		#endregion

		#region Private Helpers

		private ScriptScope CreateScope(Dictionary<string, object> localScope)
		{
			var scope = engine.CreateScope();

			Apply(scope, modules);
			Apply(scope, localScope);

			return scope;
		}

		private void CleanupScope(ScriptScope scope)
		{
			// Clean up any internal state created by the scope
			var names = scope.GetVariableNames().ToList();
			foreach (var name in names)
				scope.RemoveVariable(name);
		}

		private static void Apply(ScriptScope scope, Dictionary<string, object> vars)
		{
			foreach (var item in vars)
			{
				string name = item.Key;
				object value = item.Value;

				var bs = value as BitwiseStream;
				if (bs != null)
				{
					var buffer = new byte[bs.Length];
					var offset = 0;
					var count = buffer.Length;

					bs.Seek(0, System.IO.SeekOrigin.Begin);

					int nread;
					while ((nread = bs.Read(buffer, offset, count)) != 0)
					{
						offset += nread;
						count -= nread;
					}

					value = buffer;
				}

				scope.SetVariable(name, value);
			}
		}

		#endregion
	}
}
