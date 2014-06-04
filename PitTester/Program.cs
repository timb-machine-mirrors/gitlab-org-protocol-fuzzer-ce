using System;
using System.Linq;

using Peach.Core.Runtime;
using System.Reflection;
using System.IO;

using Peach.Enterprise;
using Peach.Enterprise.WebServices;
using System.Text;
using System.Xml;

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
				errors.AppendLine();

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
					var defs = PitDefines.Parse(fileName + ".config");
					var noName = string.Join(", ", defs.Where(d => string.IsNullOrEmpty(d.Name)).Select(d => d.Key));
					var noDesc = string.Join(", ", defs.Where(d => string.IsNullOrEmpty(d.Description)).Select(d => d.Key));

					var sb = new StringBuilder();
					
					if (noName.Length > 0)
						sb.AppendFormat("The following keys have an empty name: {0}", noName);
					if (sb.Length > 0)
						sb.AppendLine();
					if (noDesc.Length > 0)
						sb.AppendFormat("The following keys have an empty description: {0}", noDesc);
					if (sb.Length > 0)
						throw new ApplicationException(sb.ToString());


					Console.Write(".");

					++total;
				}
				catch (Exception ex)
				{
					Console.Write("E");

					errors.AppendFormat("{0} -> {1}.config", e.Name, fileName);
					errors.AppendLine();
					errors.AppendLine(ex.Message);
					errors.AppendLine();
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

			Console.WriteLine("Verifying pit files");

			foreach (var e in lib.Entries)
			{
				var fileName = e.Versions[0].Files[0].Name;

				try
				{
					VerifyPit(fileName);

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
			Console.WriteLine("Parsed {0}/{1} pit files", total, lib.Entries.Count());

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

		static void VerifyPit(string fileName)
		{
			int idxDeclaration = 0;
			int idxCopyright = 0;
			int idx = 0;

			using (var rdr = XmlReader.Create(fileName))
			{
				var errors = new StringBuilder();

				while (++idx > 0)
				{
					do
					{
						if (!rdr.Read())
							throw new ApplicationException("Failed to read xml.");
					}
					while (rdr.NodeType == XmlNodeType.Whitespace);

					if (rdr.NodeType == XmlNodeType.XmlDeclaration)
					{
						idxDeclaration = idx;
					}
					else if (rdr.NodeType == XmlNodeType.Comment)
					{
						idxCopyright = idx;

						var split = rdr.Value.Split('\n');
						if (split.Length <= 1)
							errors.AppendLine("Long form copyright message is missing.");
					}
					else if (rdr.NodeType == XmlNodeType.Element)
					{
						if (rdr.Name != "Peach")
						{
							errors.AppendLine("The first xml element is not <Peach>.");
							break;
						}

						if (!rdr.MoveToAttribute("description"))
							errors.AppendLine("Pit is missing description attribute.");
						else if (string.IsNullOrEmpty(rdr.Value))
							errors.AppendLine("Pit description is empty.");

						var author = "Deja Vu Security, LLC";

						if (!rdr.MoveToAttribute("author"))
							errors.AppendLine("Pit is missing author attribute.");
						else if (author != rdr.Value)
							errors.AppendLine("Pit author is '" + rdr.Value + "' but should be '" + author + "'.");

						var ns = "http://peachfuzzer.com/2012/Peach";

						if (!rdr.MoveToAttribute("xmlns"))
							errors.AppendLine("Pit is missing xmlns attribute.");
						else if (ns != rdr.Value)
							errors.AppendLine("Pit xmlns is '" + rdr.Value + "' but should be '" + ns + "'.");

						var schema = "http://peachfuzzer.com/2012/Peach peach.xsd";

						if (!rdr.MoveToAttribute("schemaLocation", System.Xml.Schema.XmlSchema.InstanceNamespace))
							errors.AppendLine("Pit is missing xsi:schemaLocation attribute.");
						else if (schema != rdr.Value)
							errors.AppendLine("Pit xsi:schemaLocation is '" + rdr.Value + "' but should be '" + schema + "'.");

						break;
					}
				}

				if (idxDeclaration != 1)
					errors.AppendLine("Pit is missing xml declaration.");

				if (idxCopyright == 0)
					errors.AppendLine("Pit is missing top level copyright message.");

				if (errors.Length > 0)
					throw new ApplicationException(errors.ToString());
			}
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
