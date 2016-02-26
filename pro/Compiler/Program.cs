using System;
using System.Collections.Generic;
using System.Diagnostics;
using Peach.Core;
using Peach.Core.Runtime;
using Peach.Pro.Core;
using System.Reflection;
using System.IO;
using System.Security.Cryptography;

namespace PitCompiler
{
	public static class Program
	{
		static int _logLevel;
		static OptionSet _options;
		static readonly Dictionary<string, string> _defines = new Dictionary<string, string>();

		static int Main(string[] args)
		{
			try
			{
				_options = new OptionSet()
				{
					{ 
						"h|?|help", 
						"Show this help",
						v => Syntax(false) 
					},
					{
						"V|version",
						"Show the current version of this tool",
						v => Version()
					},
					{ 
						"debug", 
						"Enable debug messages",
						v => _logLevel = 1 
					},
					{ 
						"trace", 
						"Enable trace messages",
						v => _logLevel = 2 
					},
					{
						"D|define=",
						"Specify a pit define.",
						AddDefine
					},
				};

				var extra = _options.Parse(args);
				if (extra.Count != 1)
					Syntax(true);

				Utilities.ConfigureLogging(_logLevel);

				return Run(extra[0]);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Error: {0}", ex.Message);
				NLog.LogManager.GetLogger("pitc").Debug(ex);
				return -1;
			}
			finally
			{
				// for debugging use.
				if (Debugger.IsAttached)
					Debugger.Break();
			}
		}

		static void Syntax(bool error)
		{
			Console.WriteLine("Usage: {0} [OPTION...] pit_path", Utilities.ExecutableName);
			_options.WriteOptionDescriptions(Console.Out);
			Environment.Exit(error ? -1 : 0);
		}

		static void Version()
		{
			Console.WriteLine("{0}: v{1} ({2})",
				Utilities.ExecutableName,
				Assembly.GetExecutingAssembly().GetName().Version,
				ComputeVersionHash()
			);
			Environment.Exit(0);
		}

		static string ComputeVersionHash()
		{
			using (var algorithm = HashAlgorithm.Create("MD5"))
			using (var cs = new CryptoStream(Stream.Null, algorithm, CryptoStreamMode.Write))
			{
				ComputeVersionHash(Assembly.GetEntryAssembly(), cs, new HashSet<string>());
				cs.FlushFinalBlock();
				return BitConverter.ToString(algorithm.Hash).Replace("-", string.Empty);
			}
		}

		static void ComputeVersionHash(Assembly asm, CryptoStream cs, HashSet<string> seen)
		{
			if (seen.Contains(asm.Location) || asm.GlobalAssemblyCache)
				return;
			seen.Add(asm.Location);

			try
			{
				using (var stream = new FileStream(asm.Location, FileMode.Open))
					stream.CopyTo(cs);
			}
			catch (Exception)
			{
				// this can happen when trying to read mscorlib.dll
				// ignore it since we don't care if system assemblies change versions
				return;
			}

//			Console.WriteLine(asm.FullName);

			foreach (var asmRef in asm.GetReferencedAssemblies())
			{
				ComputeVersionHash(Assembly.Load(asmRef), cs, seen);
			}
		}

		static void AddDefine(string arg)
		{
			var parts = arg.Split('=');
			if (parts.Length != 2)
				throw new PeachException("Error, defined values supplied via -D/--define must have an equals sign providing a key-pair set.");

			var key = parts[0];
			var value = parts[1];

			_defines[key] = value;
		}

		static int Run(string pitPath)
		{
			Console.WriteLine("pitc: {0}", pitPath);

			string pitLibraryPath;
			if (!_defines.TryGetValue("PitLibraryPath", out pitLibraryPath))
				pitLibraryPath = ".";

			bool hasErrors = false;

			var compiler = new Peach.Pro.Core.PitCompiler(pitLibraryPath, pitPath);
			var errors = compiler.Run();
			foreach (var error in errors)
			{
				hasErrors = true;
				Console.Error.WriteLine(error);
			}

			return hasErrors ? -1 : 0;
		}
	}
}
