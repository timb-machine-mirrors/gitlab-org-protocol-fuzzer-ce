﻿
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
using System.Linq;
using System.Threading;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Math;
using System.IO;
using System.Text;
using NLog;
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

		#region Error Listener

		class ScriptErrorListener : ErrorListener
		{
			public readonly List<string> Errors = new List<string>();
			public readonly List<string> Warnings = new List<string>();

			public override void ErrorReported(ScriptSource source, string message, SourceSpan span, int errorCode, Severity severity)
			{
				var sb = new StringBuilder(message.Length);
				sb.Append(char.ToUpperInvariant(message[0]));
				sb.Append(message.Substring(1));

				switch (severity)
				{
					case Severity.FatalError:
					case Severity.Error:
						Errors.Add("{0} lines {1}: {2}.".Fmt(
							sb,
							span.Start,
							source.MapLine(span)));
						break;
					case Severity.Warning:
					case Severity.Ignore:
						Warnings.Add("{0} lines {1}: {2}.".Fmt(
							sb,
							span.Start,
							source.MapLine(span)));
						break;
				}
			}
		}

		#endregion

		#region Compile

		readonly Dictionary<string, CompiledCode> _scriptCache = new Dictionary<string, CompiledCode>();

		private CompiledCode CompileCode(ScriptScope scope, string code, bool cache)
		{
			CompiledCode compiled;

			if (!_scriptCache.TryGetValue(code, out compiled))
			{
				var errors = new ScriptErrorListener();
				var source = scope.Engine.CreateScriptSourceFromString(code, SourceCodeKind.Expression);
				compiled = source.Compile(errors);

				if (compiled == null)
				{
					var err = errors.Errors.FirstOrDefault() ?? errors.Warnings.FirstOrDefault();
					if (err == null)
						throw new PeachException("Failed to comile expression [{0}].".Fmt(code));

					throw new PeachException("Failed to comile expression [{0}]. {1}".Fmt(code, err));
				}

				if (cache)
					_scriptCache[code] = compiled;
			}

			return compiled;
		}

		#endregion

		#region Exec & Eval

		/// <summary>
		/// Global scope for this instance of scripting
		/// </summary>
		ScriptScope _scope = null;

		/// <summary>
		/// Create the global scope, or return existing one
		/// </summary>
		/// <returns></returns>
		public ScriptScope CreateScope()
		{
			if (_scope == null)
			{
				_scope = engine.CreateScope();

				Apply(_scope, modules);
			}

			return _scope;
		}

		/// <summary>
		/// Execute a scripting program. Not cached.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="localScope"></param>
		public void Exec(string code, Dictionary<string, object> localScope)
		{
			var scope = CreateScope(localScope);
			var compiled = CompileCode(scope, code, false);

			try
			{
				compiled.Execute(scope);
			}
			catch (Exception ex)
			{
				if (ex.GetBaseException() is ThreadAbortException)
					throw;

				throw new SoftException("Failed to execute expression [{0}]. {1}.".Fmt(code, ex.Message), ex);
			}
			finally
			{
				CleanupScope(scope, localScope);
			}
		}

		/// <summary>
		/// Evaluate an expression. Pre-compiled expressions are cached by default.
		/// </summary>
		/// <param name="code">Expression to evaluate</param>
		/// <param name="localScope">Local scope for expression</param>
		/// <param name="cache">Cache compiled script for re-use (defaults to true)</param>
		/// <returns>Result from expression</returns>
		public object Eval(string code, Dictionary<string, object> localScope, bool cache = true)
		{
			var scope = CreateScope(localScope);
			var compiled = CompileCode(scope, code, cache);

			try
			{
				var obj = compiled.Execute(scope);

				// changing this to be sane (using as instead of is) causes weird compiler issues!
				// you've been warned.
				if (obj is BigInteger)
				{
					var bint = (BigInteger) obj;

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
				if (ex.GetBaseException() is ThreadAbortException)
					throw;

				throw new SoftException("Failed to evalate expression [{0}]. {1}.".Fmt(code, ex.Message), ex);
			}
		}

		#endregion

		#region Private Helpers

		/// <summary>
		/// Returns the global scope with localScope added in.
		/// </summary>
		/// <param name="localScope"></param>
		/// <returns></returns>
		private ScriptScope CreateScope(Dictionary<string, object> localScope)
		{
			var scope = CreateScope();

			Apply(scope, localScope);

			return scope;
		}

		/// <summary>
		/// Clear out our scope object. This is very slow!
		/// </summary>
		/// <param name="scope"></param>
		private void CleanupScope(ScriptScope scope)
		{
			// Clean up any internal state created by the scope
			var names = scope.GetVariableNames().ToList();
			foreach (var name in names)
				scope.RemoveVariable(name);
		}

		/// <summary>
		/// Remove local scope items from global scope. This is quick.
		/// </summary>
		/// <param name="scope"></param>
		/// <param name="localScope"></param>
		private void CleanupScope(ScriptScope scope, Dictionary<string, object> localScope)
		{
			foreach (var name in localScope.Keys)
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
