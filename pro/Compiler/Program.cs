using System;
using System.Collections.Generic;
using System.Diagnostics;
using Peach.Core;
using Peach.Core.Runtime;
using Peach.Pro.Core;

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
						v => Syntax() 
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
					Syntax();

				Utilities.ConfigureLogging(_logLevel);

				Run(extra[0]);

				return 0;
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

		static void Syntax()
		{
			Console.WriteLine("Usage: {0} [OPTION...] pit_path", Utilities.ExecutableName);
			_options.WriteOptionDescriptions(Console.Out);
			Environment.Exit(-1);
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

		static void Run(string pitPath)
		{
			Console.WriteLine("pitc: {0}", pitPath);

			string pitLibraryPath;
			if (!_defines.TryGetValue("PitLibraryPath", out pitLibraryPath))
				pitLibraryPath = ".";

			FieldTreeGenerator.Save(pitPath, pitLibraryPath);
		}
	}
}
