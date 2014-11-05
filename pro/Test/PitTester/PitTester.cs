using Peach.Enterprise;
using Peach.Enterprise.WebServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace PitTester
{
	public class PitTester
	{
		public static void TestPit(string libraryPath, string pitFile, bool singleIteration, uint? seed)
		{
			var testFile = pitFile + ".test";
			if (!File.Exists(testFile))
				throw new FileNotFoundException();

			var pitName = GetRelativePath(libraryPath, pitFile);

			Peach.Enterprise.Test.PitTester.TestPit(libraryPath, pitName, null, singleIteration, seed);
		}

		public static void VerifyPitConfig(string fileName)
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
		}

		public static void VerifyPit(string pitLibraryPath, string fileName, bool isTest)
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

				var sm = nav.Select("/p:Peach/p:StateModel", nsMgr);
				while (sm.MoveNext())
				{
					var smName = sm.Current.GetAttribute("name", "") ?? "<unknown>";

					var actions = sm.Current.Select("//p:Action[@type='call' and @publisher='Peach.Agent']", nsMgr);

					bool gotStart = false;
					bool gotEnd = false;

					while (actions.MoveNext())
					{
						var meth = actions.Current.GetAttribute("method", "");
						if (meth == "StartIterationEvent")
							gotStart = true;
						else if (meth == "ExitIterationEvent")
							gotEnd = true;
						else
							errors.AppendLine(string.Format("StateModel '{0}' has an unexpected call action.  Method is '{1}' and should be 'StartIterationEvent' or 'EndIterationEvent'.", smName, meth));
					}

					if (!gotStart)
						errors.AppendLine(string.Format("StateModel '{0}' does not call agent with 'StartIterationEvent'.", smName));

					if (!gotEnd)
						errors.AppendLine(string.Format("StateModel '{0}' does not call agent with 'ExitIterationEvent'.", smName));
				}
			}

			try
			{
				if (isTest)
				{
					var args = new Dictionary<string, object>();
					var defs = Peach.Core.Analyzers.PitParser.parseDefines(fileName + ".config");
					defs.Insert(0, new KeyValuePair<string, string>("PitLibraryPath", pitLibraryPath));
					defs = PitDefines.Evaluate(defs);
					args[Peach.Core.Analyzers.PitParser.DEFINED_VALUES] = defs;

					new Godel.Core.GodelPitParser().asParser(args, fileName);
				}
			}
			catch (Exception ex)
			{
				errors.AppendLine("PitParser exception: " + ex.Message);
			}

			if (errors.Length > 0)
				throw new ApplicationException(errors.ToString());
		}

		static string GetRelativePath(string basePath, string fullPath)
		{
			if (basePath.LastOrDefault() != Path.DirectorySeparatorChar)
				basePath += Path.DirectorySeparatorChar;

			var relPath = fullPath.Substring(basePath.Length);

			return relPath;
		}
	}
}
