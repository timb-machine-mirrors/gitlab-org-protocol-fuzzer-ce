using System;
using System.Linq;

using Peach.Core.Runtime;
using System.Reflection;
using System.IO;

using Peach.Enterprise;
using Peach.Enterprise.WebServices;

namespace PitTester
{
	public static class Program
	{
		static int Main(string[] args)
		{
			if (args.Length == 0)
				Syntax();

			//var p = new OptionSet()
			//	{
			//		{ "h|?|help", v => Syntax() },
			//		{ "analyzer=", v => analyzer = v },
			//		{ "debug", v => config.debug = 1 },
			//		{ "trace", v => config.debug = 2 },
			//		{ "1", v => config.singleIteration = true},
			//		{ "range=", v => ParseRange(config, v)},
			//		{ "t|test", v => test = true},
			//		{ "c|count", v => config.countOnly = true},
			//		{ "skipto=", v => config.skipToIteration = Convert.ToUInt32(v)},
			//		{ "seed=", v => config.randomSeed = Convert.ToUInt32(v)},
			//		{ "p|parallel=", v => ParseParallel(config, v)},
			//		{ "a|agent=", v => agent = v},
			//		{ "D|define=", v => AddNewDefine(v) },
			//		{ "definedvalues=", v => definedValues.Add(v) },
			//		{ "config=", v => defindeValues.Add(v) },
			//		{ "parseonly", v => parseOnly = true },
			//		{ "bob", var => bob() },
			//		{ "charlie", var => Charlie() },
			//		{ "showdevices", var => ShowDevices() },
			//		{ "showenv", var => ShowEnvironment() },
			//		{ "makexsd", var => MakeSchema() },
			//	};

			//List<string> extra = p.Parse(args);

			//if (extra.Count == 0 && agent == null && analyzer == null)
			//	Syntax();


			var lib = new PitDatabase();

			lib.ValidationEventHandler += delegate(object sender, ValidationEventArgs e)
			{
				Console.WriteLine("Failed to load {0}\n{1}", e.FileName, e.Exception.Message);
			};

			Console.WriteLine("Loading library");

			lib.Load(args[0]);

			Console.WriteLine("Loaded {0} pits", lib.Entries.Count());

			int errors = 0;
			foreach (var e in lib.Entries)
			{
				var fileName = e.Versions[0].Files[0].Name;

				try
				{
					PitDefines.Parse(fileName + ".config");
					Console.Write(".");
					//Console.WriteLine("Valid config for {0}", e.Name);
				}
				catch (Exception )
				{
					Console.Write("E");
					++errors;
					//Console.WriteLine("Inalid config for {0}\n{1}", e.Name, ex.Message);
				}
			}

			Console.WriteLine();
			Console.WriteLine("Parsed {0}/{1} .config files without errors.", lib.Entries.Count() - errors, lib.Entries.Count());


			return 0;
		}

		static void Syntax()
		{
			var self = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
			var syntax = @"{0} pit_library_path";

			Console.WriteLine(syntax, self);

			Environment.Exit(-1);
		}
	}
}
