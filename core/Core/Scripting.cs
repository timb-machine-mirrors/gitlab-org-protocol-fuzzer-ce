
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
using System.Reflection;
using IronRuby;
using Peach.Core.IO;

namespace Peach.Core
{
	class ResourceAwarePlatformAdapationLayer : PlatformAdaptationLayer
	{
		private static readonly char Seperator = Path.DirectorySeparatorChar;

		private readonly string _prefix; 
		private readonly Assembly _asm;
		private readonly Dictionary<string, string> _resourceFiles = new Dictionary<string, string>();

		public ResourceAwarePlatformAdapationLayer(Assembly asm, string prefix)
		{
			_asm = asm;
			_prefix = prefix;

			CreateResourceFileSystemEntries();
		}

		private void CreateResourceFileSystemEntries()
		{
			foreach (var name in _asm.GetManifestResourceNames())
			{
				if (!name.EndsWith(".py"))
					continue;

				var filename = name.Substring(_prefix.Length);
				filename = filename.Substring(0, filename.Length - 3); // Remove .py
				filename = filename.Replace('.', Seperator);
				_resourceFiles.Add(filename + ".py", name);
			}
		}
		
		private Stream OpenResourceInputStream(string path)
		{
			string resourceName;
			if (_resourceFiles.TryGetValue(RemoveCurrentDir(path), out resourceName))
			{
				return _asm.GetManifestResourceStream(resourceName);
			}
			return null;
		}

		private bool ResourceDirectoryExists(string path)
		{
			return _resourceFiles.Keys.Any(f => f.StartsWith(RemoveCurrentDir(path) + Seperator));
		}

		private bool ResourceFileExists(string path)
		{
			return _resourceFiles.ContainsKey(RemoveCurrentDir(path));
		}

		private static string RemoveCurrentDir(string path)
		{
			return path
				.Replace(Directory.GetCurrentDirectory() + Seperator, "")
				.Replace("." + Seperator, "");
		}

		public override bool FileExists(string path)
		{
			return ResourceFileExists(path) || base.FileExists(path);
		}

		public override string[] GetFileSystemEntries(string path, string searchPattern, bool includeFiles, bool includeDirectories)
		{
			var fullPath = Path.Combine(path, searchPattern);
			if (ResourceFileExists(fullPath) || ResourceDirectoryExists(fullPath))
				return new[] { fullPath };

			if (!ResourceDirectoryExists(path))
				return base.GetFileSystemEntries(path, searchPattern, includeFiles, includeDirectories);
	
			return new string[0]; 
		}

		public override bool DirectoryExists(string path)
		{
			return ResourceDirectoryExists(path) || base.DirectoryExists(path);
		}

		public override Stream OpenInputFileStream(string path)
		{
			return OpenResourceInputStream(path) ?? base.OpenInputFileStream(path);
		}

		public override Stream OpenInputFileStream(string path, FileMode mode, FileAccess access, FileShare share)
		{
			return OpenResourceInputStream(path) ?? base.OpenInputFileStream(path, mode, access, share);
		}

		public override Stream OpenInputFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize)
		{
			return OpenResourceInputStream(path) ?? base.OpenInputFileStream(path, mode, access, share, bufferSize);
		}
	}

	class ResourceAwareScriptHost : ScriptHost
	{
		readonly PlatformAdaptationLayer _pal ;

		public ResourceAwareScriptHost(Assembly asm, string prefix)
		{
			_pal = new ResourceAwarePlatformAdapationLayer(asm, prefix);
		}

		public override PlatformAdaptationLayer PlatformAdaptationLayer
		{
			get { return _pal; }
		}
	}

	public class PythonScripting : Scripting
	{
		private readonly Assembly _asm;
		private readonly string _prefix;

		public PythonScripting(Assembly asm = null, string prefix = "")
		{
			_asm = asm;
			_prefix = prefix;
		}

		protected override ScriptEngine GetEngine()
		{
			var setup = Python.CreateRuntimeSetup(null);
			if (_asm != null)
			{
				setup.HostType = typeof(ResourceAwareScriptHost);
				setup.HostArguments = new object[] { _asm, _prefix };
			}

			var runtime = new ScriptRuntime(setup);
			var engine = Python.GetEngine(runtime);

			// Need to add python stdlib to search path
			var paths = engine.GetSearchPaths();
			foreach (var path in ClassLoader.SearchPaths)
				paths.Add(Path.Combine(path, "Lib"));
			engine.SetSearchPaths(paths);

			return engine;
		}
	}

	public class RubyScripting : Scripting
	{
		protected override ScriptEngine GetEngine()
		{
			return Ruby.CreateEngine();
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
				switch (severity)
				{
					case Severity.FatalError:
					case Severity.Error:
						Errors.Add("{0} at line {1}.".Fmt(message, source.MapLine(span)));
						break;
					case Severity.Warning:
					case Severity.Ignore:
						Warnings.Add("{0} at line {1}.".Fmt(message, source.MapLine(span)));
						break;
				}
			}
		}

		#endregion

		#region Compile

		readonly Dictionary<string, CompiledCode> _scriptCache = new Dictionary<string, CompiledCode>();

		private CompiledCode CompileCode(ScriptScope scope, string code, SourceCodeKind kind, bool cache)
		{
			CompiledCode compiled;

			if (!_scriptCache.TryGetValue(code, out compiled))
			{
				var errors = new ScriptErrorListener();
				var source = scope.Engine.CreateScriptSourceFromString(code, kind);
				compiled = source.Compile(errors);

				if (compiled == null)
				{
					var err = errors.Errors.FirstOrDefault() ?? errors.Warnings.FirstOrDefault();
					if (err == null)
						throw new PeachException("Failed to compile expression [{0}].".Fmt(code));

					throw new PeachException("Failed to compile expression [{0}], {1}".Fmt(code, err));
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
			var compiled = CompileCode(scope, code, SourceCodeKind.Statements, false);

			try
			{
				compiled.Execute(scope);
			}
			catch (SoftException)
			{
				throw;
			}
			catch (PeachException)
			{
				throw;
			}
			catch (Exception ex)
			{
				if (ex.GetBaseException() is ThreadAbortException)
					throw;

				throw new SoftException("Failed to execute expression [{0}], {1}.".Fmt(code, ex.Message), ex);
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
			var compiled = CompileCode(scope, code, SourceCodeKind.Expression, cache);

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
			catch (SoftException)
			{
				throw;
			}
			catch (PeachException)
			{
				throw;
			}
			catch (Exception ex)
			{
				if (ex.GetBaseException() is ThreadAbortException)
					throw;

				throw new SoftException("Failed to evaluate expression [{0}], {1}.".Fmt(code, ex.Message), ex);
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
