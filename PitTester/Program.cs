using System;
using System.Linq;

using Peach.Core.Runtime;
using System.Reflection;
using System.IO;

using Peach.Enterprise;
using Peach.Enterprise.WebServices;
using System.Text;

namespace PitTester
{
	public static class Program
	{
		static int LogLevel = 0;

		static int Main(string[] args)
		{
			var p = new OptionSet()
			{
					{ "h|?|help", v => Syntax() },
					{ "debug", v => LogLevel = 1 },
					{ "trace", v => LogLevel = 2 },
			};

			var extra = p.Parse(args);

			if (extra.Count != 1)
				Syntax();

			Peach.Core.Runtime.Program.ConfigureLogging(LogLevel);

			var lib = new PitDatabase();
			var errors = new StringBuilder();
			var total = 0;

			lib.LoadEventHandler += delegate(object sender, LoadEventArgs e)
			{
				Console.Write(".");
			};

			lib.ValidationEventHandler += delegate(object sender, ValidationEventArgs e)
			{
				Console.WriteLine("E");

				errors.AppendLine(e.FileName);
				errors.AppendLine(e.Exception.Message);

				++total;
			};

			Console.WriteLine("Loading pit library");

			lib.Load(args[0]);

			Console.WriteLine();
			Console.WriteLine("Loaded {0}/{1} pit files", lib.Entries.Count(), lib.Entries.Count() + total);

			if (errors.Length > 0)
			{
				Console.WriteLine();
				Console.WriteLine("Errors:");
				Console.WriteLine();
				Console.WriteLine(errors);
			}
			else
			{
				Console.WriteLine();
			}

			errors.Clear(); ;
			total = 0;

			Console.WriteLine("Verifying pit config files");

			foreach (var e in lib.Entries)
			{
				var fileName = e.Versions[0].Files[0].Name;

				try
				{
					PitDefines.Parse(fileName + ".config");

					Console.Write(".");

					++total;
				}
				catch (Exception ex)
				{
					Console.Write("E");

					errors.AppendFormat("{0} -> {1}", e.Name, fileName);
					errors.AppendLine();
					errors.AppendLine(ex.Message);
				}
			}

			Console.WriteLine();
			Console.WriteLine("Parsed {0}/{1} .config files", total, lib.Entries.Count());

			if (errors.Length > 0)
			{
				Console.WriteLine();
				Console.WriteLine("Errors:");
				Console.WriteLine();
				Console.WriteLine(errors);
			}
			else
			{
				Console.WriteLine();
			}

			errors.Clear(); ;
			total = 0;

			return 0;
		}

		static void Syntax()
		{
			var self = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
			var syntax = @"{0} [--debug --trace] pit_library_path";

			Console.WriteLine(syntax, self);

			Environment.Exit(-1);
		}
	}
}
