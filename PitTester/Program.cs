using System;
using System.Linq;

using Peach.Core.Runtime;
using System.Reflection;
using System.IO;

using Peach.Enterprise;
using Peach.Enterprise.WebServices;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace PitTester
{
	public static class Program
	{
		static int LogLevel = 0;

		static int Main(string[] args)
		{
			var notest = false;

			var p = new OptionSet()
			{
					{ "h|?|help", v => Syntax() },
					{ "debug", v => LogLevel = 1 },
					{ "trace", v => LogLevel = 2 },
					{ "notest", v => notest = true },
			};

			var extra = p.Parse(args);

			if (extra.Count != 1)
				Syntax();

			Peach.Core.Runtime.Program.ConfigureLogging(LogLevel);

			var libraryPath = args[0];
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

			lib.Load(libraryPath);

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
					var old = Peach.Core.Analyzers.PitParser.parseDefines(fileName + ".config");

					if (defs.Count != old.Count)
						throw new ApplicationException(string.Format("PitParser didn't properly parse defines file.  Expected '{0}' defines, got '{1}' defines.", defs.Count, old.Count));

					for (int i = 0; i < defs.Count; ++i)
					{
						if (defs[i].Key != old[i].Key)
							throw new ApplicationException(string.Format("PitParser didn't properly parse defines file. Expected '{1}' defines, got '{2}' for key at index {0}.", i, defs[i].Key, old[i].Key));
						if (defs[i].Value != old[i].Value)
							throw new ApplicationException(string.Format("PitParser didn't properly parse defines file. Expected '{1}' defines, got '{2}' for value at index {0}.", i, defs[i].Value, old[i].Value));
					}

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
				int success = 1;

				for (int i = 0; i < e.Versions[0].Files.Count; ++i)
				{
					var fileName = e.Versions[0].Files[i].Name;

					try
					{
						VerifyPit(fileName, i == 0);
					}
					catch (Exception ex)
					{
						success = 0;

						errors.AppendFormat("{0} -> {1}", e.Name, fileName);
						errors.AppendLine();
						errors.AppendLine(ex.Message);
					}
				}

				total += success;

				Console.Write(success == 0 ? "E" : ".");
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

			errors.Clear();
			total = 0;

			if (notest)
				return 0;

			Console.WriteLine("Testing pit files");

			foreach (var e in lib.Entries)
			{
				var fileName = e.Versions[0].Files[0].Name;

				try
				{
					TestPit(libraryPath, fileName, null);

					Console.Write(".");

					++total;
				}
				catch (FileNotFoundException)
				{
					Console.Write("I");
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
			Console.WriteLine("Passed {0}/{1} pit tests", total, lib.Entries.Count());

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

		static void TestPit(string libraryPath, string pitFile, string testName)
		{
			var testFile = pitFile + ".test";
			if (!File.Exists(testFile))
				throw new FileNotFoundException();

			var pitName = pitFile.Substring(libraryPath.Length + 1);

			Peach.Enterprise.Test.PitTester.TestPit(libraryPath, pitName, testName);
		}

		static void VerifyPit(string fileName, bool isTest)
		{
			var errors = new StringBuilder();

			int idxDeclaration = 0;
			int idxCopyright = 0;
			int idx = 0;

			using (var rdr = XmlReader.Create(fileName))
			{
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

						var ns = PeachElement.Namespace;

						if (!rdr.MoveToAttribute("xmlns"))
							errors.AppendLine("Pit is missing xmlns attribute.");
						else if (ns != rdr.Value)
							errors.AppendLine("Pit xmlns is '" + rdr.Value + "' but should be '" + ns + "'.");

						var schema = PeachElement.SchemaLocation;

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

			}

			using (var rdr = XmlReader.Create(fileName))
			{
				var doc = new XPathDocument(rdr);
				var nav = doc.CreateNavigator();
				var nsMgr = new XmlNamespaceManager(nav.NameTable);
				nsMgr.AddNamespace("p", PeachElement.Namespace);

				var it = nav.Select("/p:Peach/p:Test", nsMgr);

				var expexted = isTest ? 1 : 0;

				if (it.Count != expexted)
					errors.AppendLine("Number of <Test> elements is " + it.Count + " but should be " + expexted + ".");
			}

			if (errors.Length > 0)
				throw new ApplicationException(errors.ToString());
		}

		static void Syntax()
		{
			var self = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
			var syntax = @"{0} [--debug --trace --notest] pit_library_path";

			Console.WriteLine(syntax, self);

			Environment.Exit(-1);
		}
	}
}
