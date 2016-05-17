using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Peach.Core;
using Peach.Core.Runtime;
using System.Reflection;
using System.IO;
using System.Security.Cryptography;
using Peach.Pro.Test.Core;
using Peach.Pro.Core;

namespace PitCompiler
{
	public static class Program
	{
		static int _logLevel;
		static OptionSet _options;
		static readonly Dictionary<string, string> _defines = new Dictionary<string, string>();
		static bool _protect;
		static string _prefix = "";
		static string _salt;
		static bool _help;

		static int Main(string[] args)
		{
			try
			{
				_options = new OptionSet()
				{
					{
						"h|?|help",
						"Show this help",
						x => _help = true
					},
					{
						"V|version",
						"Show the current version of this tool",
						x => Version()
					},
					{
						"debug",
						"Enable debug messages",
						x => _logLevel = 1
					},
					{
						"trace",
						"Enable trace messages",
						x => _logLevel = 2
					},
					{
						"D|define=",
						"Specify a pit define.",
						AddDefine
					},
					{
						"protect",
						"Protect a resource assembly",
						x => _protect = true
					},
					{
						"prefix=",
						"Prefix for resources in an assembly",
						x => _prefix = x
					},
					{
						"salt=",
						"Path to file containing salt",
						x => _salt = x
					}
				};

				var extra = _options.Parse(args);

				Utilities.ConfigureLogging(_logLevel);

				if (_protect)
					return Protect(extra);

				if (_help)
					Syntax(false);

				if (extra.Count != 1)
					Syntax(true);

				return Run(extra[0]);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Error: {0}", ex);
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

		static void ProtectSyntax(bool error)
		{
			Console.WriteLine("Usage: {0} --protect [OPTION...] input_asm output_asm", Utilities.ExecutableName);
			_options.WriteOptionDescriptions(Console.Out);
			Environment.Exit(error ? -1 : 0);
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

		static int Protect(List<string> args)
		{
			if (_help)
				ProtectSyntax(false);

			if (args.Count != 2)
				ProtectSyntax(true);

			var input = args[0];
			var output = args[1];
			var dir = Path.GetDirectoryName(output);
			var asm = Assembly.LoadFile(input);

			if (!File.Exists(_salt))
			{
				Console.Error.WriteLine("File specified by --salt does not exist");
				ProtectSyntax(true);
			}
			var salt = File.ReadAllLines(_salt).FirstOrDefault();

			var manifest = PitResourceLoader.EncryptResources(asm, _prefix, output, salt);

			var privatePath = Path.Combine(dir, "private.json");
			using (var stream = File.OpenWrite(privatePath))
			{
				PitResourceLoader.SaveManifest(stream, manifest);
			}

			return 0;
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

			using (var stream = new FileStream(asm.Location, FileMode.Open, FileAccess.Read, FileShare.Read))
				stream.CopyTo(cs);

			//Console.WriteLine(asm.FullName);

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

			var compiler = new Peach.Pro.Core.PitCompiler(pitLibraryPath, pitPath);
			var errors = compiler.Run();

			bool hasErrors = false;
			foreach (var error in errors)
			{
				hasErrors = true;
				Console.Error.WriteLine(error);
			}

			if (compiler.TotalNodes > 1000)
			{
				Console.Error.WriteLine("pitc: '{0}' has too many nodes: {1}", 
					Path.GetFileName(pitPath), 
					compiler.TotalNodes
				);
			}

			return hasErrors ? -1 : 0;
		}
	}
}
