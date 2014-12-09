using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Dom.XPath;
using Peach.Core.Fixups;
using Peach.Core.IO;
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
		public static event Peach.Core.Engine.IterationStartingEventHandler IterationStarting;

		public static void OnIterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			if (IterationStarting != null)
				IterationStarting(context, currentIteration, totalIterations);
		}

		public static void TestPit(string libraryPath, string pitFile, bool singleIteration, uint? seed)
		{
			var testFile = pitFile + ".test";
			if (!File.Exists(testFile))
				throw new FileNotFoundException();

			//var pitName = GetRelativePath(libraryPath, pitFile);

			//var fileName = Path.Combine(pitLibrary, pitName);

			//var defines = PitDefines.Parse(fileName + ".config");
			var testData = TestData.Parse(testFile);

			var defs = new List<KeyValuePair<string, string>>();
			foreach (var item in testData.Defines)
				if (item.Key != "PitLibraryPath")
					defs.Add(new KeyValuePair<string, string>(item.Key, item.Value));
			defs.Add(new KeyValuePair<string, string>("PitLibraryPath", libraryPath));

			var args = new Dictionary<string, object>();
			args[Peach.Core.Analyzers.PitParser.DEFINED_VALUES] = defs;

			var parser = new Peach.Core.Analyzers.PitParser();

			var dom = parser.asParser(args, pitFile);

			foreach (var test in dom.tests)
			{
				// Don't run extra control iterations...
				test.controlIteration = 0;

				test.agents.Clear();

				var data = testData.Tests.Where(t => t.Name == test.name).First();

				var logger = new TestLogger(data, testData.Ignores.Select(i => i.Xpath));

				test.loggers.Clear();
				test.loggers.Add(logger);

				foreach (var key in test.publishers.Keys)
					test.publishers[key] = new TestPublisher(key, logger);
			}

			var fixupOverrides = new Dictionary<string, Variant>();

			if (testData.Slurps.Count > 0)
			{
				var doc = new XmlDocument();
				var resolver = new PeachXmlNamespaceResolver();
				var navi = new PeachXPathNavigator(dom);

				foreach (var slurp in testData.Slurps)
				{
					var iter = navi.Select(slurp.SetXpath, resolver);
					if (!iter.MoveNext())
						throw new SoftException("Error, slurp valueXpath returned no values. [" + slurp.SetXpath + "]");

					var n = doc.CreateElement("Foo");
					n.SetAttribute("valueType", slurp.ValueType);
					n.SetAttribute("value", slurp.Value);

					var blob = new Blob();
					new Peach.Core.Analyzers.PitParser().handleCommonDataElementValue(n, blob);

					do
					{
						var setElement = ((PeachXPathNavigator)iter.Current).currentNode as DataElement;
						if (setElement == null)
							throw new PeachException("Error, slurp setXpath did not return a Data Element. [" + slurp.SetXpath + "]");

						setElement.DefaultValue = blob.DefaultValue;

						if (setElement.fixup is VolatileFixup)
						{
							var dm = setElement.root as DataModel;
							if (dm != null && dm.actionData != null)
							{
								// If the element is under an action, and has a volatile fixup
								// store off the value for overriding during TestStarting
								var key = "Peach.VolatileOverride.{0}.{1}".Fmt(dm.actionData.outputName, setElement.fullName);
								fixupOverrides[key] = blob.DefaultValue;
							}
						}

						if (blob.DefaultValue.GetVariantType() == Variant.VariantType.BitStream)
							((BitwiseStream)blob.DefaultValue).Position = 0;
					}
					while (iter.MoveNext());

					//ApplySlurp(dom, slurp);
				}
			}

			var config = new RunConfiguration();
			config.range = true;
			config.rangeStart = 0;
			config.rangeStop = 500;
			config.pitFile = Path.GetFileName(pitFile);
			config.runName = "Default";
			config.singleIteration = singleIteration;

			if (seed.HasValue)
				config.randomSeed = seed.Value;

			var q = testData.Tests.Where(i => i.Name == config.runName).First();
			if (!string.IsNullOrEmpty(q.Seed))
			{
				uint s;
				if (!uint.TryParse(q.Seed, out s))
					throw new PeachException("Error, could not parse test seed '{0}' as an unsigned integer.".Fmt(q.Seed));

				config.randomSeed = s;
			}


			uint num = 0;
			var e = new Engine(null);

			e.TestStarting += ctx =>
			{
				foreach (var kv in fixupOverrides)
					ctx.stateStore.Add(kv.Key, kv.Value);
			};

			e.IterationStarting += (ctx, it, tot) => num = it;

			try
			{
				e.startFuzzing(dom, config);
			}
			catch (Exception ex)
			{
				var msg = "Encountered an unhandled exception on iteration {0}, seed {1}.\n{2}".Fmt(
					num,
					config.randomSeed,
					ex.Message);
				throw new PeachException(msg, ex);
			}
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
						else if (!gotStart && !ShouldSkipStart(actions))
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

		private static bool ShouldSkipStart(XPathNodeIterator actions)
		{
			var preceding = actions.Current.SelectSingleNode("preceding-sibling::comment()");
			return (preceding != null && preceding.Value.Contains("PitTester: Skip_StartIterationEvent"));
		}
	}
}
