using System.Globalization;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Dom.XPath;
using Peach.Core.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Peach.Pro.Core;
using Peach.Pro.Core.Fixups;
using Peach.Pro.Core.MutationStrategies;
using Peach.Pro.Core.WebServices;

namespace PitTester
{
	public class PitTester
	{
		static readonly Dictionary<string, string[]> OptionalParams = new Dictionary<string,string[]>
		{
			{ "RawEther", new[] { "MinMTU", "MaxMTU", "MinFrameSize", "MaxFrameSize", "PcapTimeout" }},
			{ "RawV4", new[] { "MinMTU", "MaxMTU" }},
			{ "RawV6", new[] { "MinMTU", "MaxMTU" }},
			{ "Udp", new[] { "MinMTU", "MaxMTU" }},
			{ "File", new[] { "Append", "Overwrite" }},
			{ "ConsoleHex", new[] { "BytesPerLine" }},
			{ "Null", new[] { "MaxOutputSize" }}
		};

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

			var testData = TestData.Parse(testFile);

			var defs = new List<KeyValuePair<string, string>>();
			var configFile = pitFile + ".config";
			if (File.Exists(configFile))
			{
				var baseDefs = Peach.Core.Analyzers.PitParser.parseDefines(configFile);
				baseDefs.Insert(0, new KeyValuePair<string, string>("PitLibraryPath", libraryPath));

				var testDefs = testData.Defines.ToDictionary(x => x.Key, x => x.Value);

				foreach (var item in baseDefs)
				{
					string value;
					if (testDefs.TryGetValue(item.Key, out value))
					{
						if (value == item.Value)
						{
							Console.WriteLine("Warning, .test and .config value are identical for PitDefine named: \"{0}\"", 
								item.Key
							);
						}
						defs.Add(new KeyValuePair<string, string>(item.Key, value));
						testDefs.Remove(item.Key);
					}
					else
						defs.Add(item);
				}

				if (testDefs.Count > 0)
				{
					throw new PeachException("Error, PitDefine(s) in .test not found in .config: {0}".Fmt(
						string.Join(", ", testDefs.Keys))
					);
				}
			}
			else
			{
				defs.Add(new KeyValuePair<string, string>("PitLibraryPath", libraryPath));
				foreach (var item in testData.Defines)
				{
					defs.Add(new KeyValuePair<string, string>(item.Key, item.Value));
				}
			}

			defs = PitDefines.Evaluate(defs);

			var args = new Dictionary<string, object>();
			args[Peach.Core.Analyzers.PitParser.DEFINED_VALUES] = defs;

			var parser = new Peach.Core.Analyzers.PitParser();

			var dom = parser.asParser(args, pitFile);

			foreach (var test in dom.tests)
			{
				// Don't run extra control iterations...
				test.controlIteration = 0;

				test.agents.Clear();

				var data = testData.Tests.FirstOrDefault(t => t.Name == test.Name);
				if (data == null)
					throw new PeachException("Error, no test definition found for pit test named '{0}'.".Fmt(test.Name));

				var logger = new TestLogger(data, testData.Ignores.Select(i => i.Xpath));

				test.loggers.Clear();
				test.loggers.Add(logger);

				for (var i = 0; i < test.publishers.Count; ++i)
				{
					var oldPub = test.publishers[i];
					var newPub = new TestPublisher(logger) { Name = oldPub.Name };
					test.publishers[i] = newPub;
				}
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
						var setElement = ((PeachXPathNavigator)iter.Current).CurrentNode as DataElement;
						if (setElement == null)
							throw new PeachException("Error, slurp setXpath did not return a Data Element. [" + slurp.SetXpath + "]");

						setElement.DefaultValue = blob.DefaultValue;

						if (setElement.fixup is Peach.Core.Fixups.VolatileFixup)
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

			// See #214
			// If there are is any action that has more than one data set
			// that use <Field> and the random strategy is
			// in use, turn off data set switching...
			var noSwitch = dom.tests
				.Select(t => t.stateModel)
				.SelectMany(sm => sm.states)
				.SelectMany(s => s.actions)
				.SelectMany(a => a.allData)
				.Select(ad => ad.dataSets)
				.Any(ds => ds.SelectMany(d => d).OfType<DataField>().Count() > 1);

			if (noSwitch)
			{
				foreach (var t in dom.tests.Where(t => t.strategy is RandomStrategy))
				{
					t.strategy = new RandomStrategy(new Dictionary<string, Variant>
					{
						{ "SwitchCount", new Variant(int.MaxValue.ToString(CultureInfo.InvariantCulture)) },
					});
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
			e.IterationStarting += (ctx, it, tot) => num = it;

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

		public static void VerifyDataSets(string pitLibraryPath, string fileName, bool verifyBytes = true)
		{
			var defs = Peach.Core.Analyzers.PitParser.parseDefines(fileName + ".config");
			if (defs.Any(k => k.Key == "PitLibraryPath"))
			{
				defs.Remove(defs.First(k => k.Key == "PitLibraryPath"));
				defs.Add(new KeyValuePair<string, string>("PitLibraryPath", pitLibraryPath));
			}

			var args = new Dictionary<string, object>();
			args[Peach.Core.Analyzers.PitParser.DEFINED_VALUES] = defs;

			var parser = new Peach.Core.Analyzers.PitParser();

			var dom = parser.asParser(args, fileName);

			dom.context = new RunContext();

			var sb = new StringBuilder();

			foreach (var test in dom.tests)
			{
				dom.context.test = test;

				foreach (var state in test.stateModel.states)
				{
					foreach (var action in state.actions)
					{
						foreach (var actionData in action.allData)
						{
							foreach (var data in actionData.allData)
							{
								try
								{
									if (data is DataFile)
									{
										// Verify file cracks correctly
										try
										{
											actionData.Apply(data);
										}
										catch (Exception ex)
										{
											throw new PeachException(string.Format("Error cracking data file '{0}' to '{1}.{2}.{3}.{4}'.",
												((DataFile)data).FileName, test.Name, state.Name, action.Name, actionData.dataModel.Name), ex);
										}

										// SHould we skip verifying bytes?
										if (!verifyBytes)
											continue;

										var bs = actionData.dataModel.Value;
										var value = new MemoryStream();
										bs.Seek(0, SeekOrigin.Begin);
										bs.CopyTo(value);
										value.Seek(0, SeekOrigin.Begin);

										var dataFileBytes = File.ReadAllBytes(((DataFile)data).FileName);

										// Verify all bytes match
										for (var i = 0; i < dataFileBytes.Length && i < value.Length; i++)
										{
											var b = value.ReadByte();
											if (dataFileBytes[i] != b)
											{
												throw new PeachException(
													string.Format(
														"Error: Data did not match at {0}.  Got {1:x2} expected {2:x2}. Data file '{3}' to '{4}.{5}.{6}.{7}'.",
														i, b, dataFileBytes[i], ((DataFile)data).FileName, test.Name, state.Name, action.Name,
														actionData.dataModel.Name));
											}
										}

										// Verify length matches
										if (dataFileBytes.Length != value.Length)
											throw new PeachException(
												string.Format(
													"Error: Data size mismatch. Got {0} bytes, expected {1}. Data file '{2}' to '{3}.{4}.{5}.{6}'.",
													value.Length, dataFileBytes.Length, ((DataFile)data).FileName, test.Name, state.Name, action.Name,
													actionData.dataModel.Name));
									}
									else if (data is DataField)
									{
										// Verify fields apply correctly
										try
										{
											actionData.Apply(data);
										}
										catch (Exception ex)
										{
											throw new PeachException(string.Format("Error applying data fields '{0}' to '{1}.{2}.{3}.{4}'.",
												data.Name, test.Name, state.Name, action.Name, actionData.dataModel.Name), ex);
										}
									}
								}
								catch (Exception ಠ_ಠ)
								{
									sb.AppendLine(ಠ_ಠ.Message);
								}
							}
						}
					}
				}
			}

			if (sb.Length > 0)
				throw new PeachException(sb.ToString());
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

			var sb = new StringBuilder();
			
			var logger = defs.Where(d => d.Key == "LoggerPath").ToArray();
			if (logger.Length == 0)
			{
				sb.AppendLine("Missing a define for key 'LoggerPath'.");
			}
			else
			{
				var expected = "##Peach.LogRoot##/" + Path.GetFileNameWithoutExtension(fileName);

				if (logger.Length > 1)
					sb.AppendLine("There is more than one define for 'LoggerPath'.");

				if (logger[0].Value != expected)
					sb.AppendLine("LoggerPath is set as '" + logger[0].Value + "' but it should be '" + expected + "'.");
			}

			var noName = string.Join(", ", defs.Where(d => string.IsNullOrEmpty(d.Name)).Select(d => d.Key));
			var noDesc = string.Join(", ", defs.Where(d => string.IsNullOrEmpty(d.Description)).Select(d => d.Key));

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

						const string author = "Peach Fuzzer, LLC";

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

				var expected = isTest ? 1 : 0;

				if (it.Count != expected)
					errors.AppendLine("Number of <Test> elements is " + it.Count + " but should be " + expected + ".");

				while (it.MoveNext())
				{
					var maxSize = it.Current.GetAttribute("maxOutputSize", string.Empty);
					if (string.IsNullOrEmpty(maxSize))
						errors.AppendLine("<Test> element is missing maxOutputSize attribute.");

					var lifetime = it.Current.GetAttribute("targetLifetime", string.Empty);
					if (string.IsNullOrEmpty(lifetime))
						errors.AppendLine("<Test> element is missing targetLifetime attribute.");

					var parts = fileName.Split(Path.DirectorySeparatorChar);
					var fileFuzzing = new[] {"Image", "Video", "Application"};
					if (parts.Any(fileFuzzing.Contains) || parts.Last().Contains("Client"))
					{
						if (lifetime != "iteration")
							errors.AppendLine("<Test> element has incorrect targetLifetime attribute. Expected 'iteration' but found '{0}'.".Fmt(lifetime));
					}
					else
					{
						if (lifetime != "session")
							errors.AppendLine("<Test> element has incorrect targetLifetime attribute. Expected 'session' but found '{0}'.".Fmt(lifetime));
					}

					var loggers = it.Current.Select("p:Logger", nsMgr);
					if (loggers.Count != 1)
						errors.AppendLine("Number of <Logger> elements is " + loggers.Count + " but should be 1.");

					while (loggers.MoveNext())
					{
						var cls = loggers.Current.GetAttribute("class", string.Empty);
						if (cls == "Metrics")
							errors.AppendLine("Found obsolete <Logger> element for class '" + cls + "'.");
						else if (cls != "File")
							errors.AppendLine("<Logger> element has class '" + cls + "' but should be 'File'.");

						var parameters = loggers.Current.Select("p:Param", nsMgr);

						if (parameters.Count != 1)
							errors.AppendLine("Number of logger <Param> elements is " + parameters.Count + "but should be 1.");

						while (parameters.MoveNext())
						{
							var name = parameters.Current.GetAttribute("name", string.Empty);
							if (name != "Path")
								errors.AppendLine("<Logger> element has unexpected parameter named '" + name + "'.");

							var path = parameters.Current.GetAttribute("value", string.Empty);
							if (path != "##LoggerPath##")
								errors.AppendLine("Path parameter on <Logger> element is '" + path + "' but should be '##LoggerPath##'.");
						}
					}

					var pubs = it.Current.Select("p:Publisher", nsMgr);
					while (pubs.MoveNext())
					{
						var cls = pubs.Current.GetAttribute("class", string.Empty);
						var parms = new List<string>();

						var parameters = pubs.Current.Select("p:Param", nsMgr);
						while (parameters.MoveNext())
						{
							var name = parameters.Current.GetAttribute("name", string.Empty);
							var value = parameters.Current.GetAttribute("value", string.Empty);
							if (!ShouldSkipRule(parameters, "Allow_HardCodedParamValue") && 
								(!value.StartsWith("##") || !value.EndsWith("##")))
							{
								errors.AppendLine(
									"<Publisher> parameter '{0}' is hard-coded, use a PitDefine ".Fmt(name) +
									"(suppress with 'Allow_HardCodedParamValue')"
								);
							}

							parms.Add(name);
						}

						var comments = parameters.Current.SelectSingleNode("following-sibling::comment()");
						while (comments != null)
						{
							var value = comments.Value.Trim();
							const string ignore = "PitLint: Allow_MissingParamValue=";
							if (value.StartsWith(ignore))
								parms.Add(value.Substring(ignore.Length));

							if (!comments.MoveToNext())
								comments = null;
						}

						var pub = ClassLoader.FindPluginByName<PublisherAttribute>(cls);
						if (pub == null)
						{
							errors.AppendLine("<Publisher> class '{0}' is not recognized.".Fmt(cls));
						}
						else
						{
							var pri = pub.GetAttributes<PublisherAttribute>().First();
							if (pri.Name != cls)
								errors.AppendLine("'{0}' <Publisher> is referenced with deprecated name '{1}'.".Fmt(pri.Name, cls));

							string[] optionalParams;
							if (!OptionalParams.TryGetValue(pri.Name, out optionalParams))
								optionalParams = new string[0];

							foreach (var attr in pub.GetAttributes<ParameterAttribute>())
							{
								if (!optionalParams.Contains(attr.name) && !parms.Contains(attr.name))
									errors.AppendLine("{0} publisher missing configuration for parameter '{1}'.".Fmt(pri.Name, attr.name));
							}
						}
					}
				}

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
						else if (!gotStart && !ShouldSkipRule(actions, "Skip_StartIterationEvent"))
							errors.AppendLine(string.Format("StateModel '{0}' has an unexpected call action.  Method is '{1}' and should be 'StartIterationEvent' or 'ExitIterationEvent'.", smName, meth));
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

		private static bool ShouldSkipRule(XPathNodeIterator it, string rule)
		{
			var preceding = it.Current.SelectSingleNode("preceding-sibling::comment()");
			if (preceding == null)
				return false;

			var skip = false;

			do
			{
				skip |= preceding.Value.Contains("PitLint: {0}".Fmt(rule));
			}
			while (!skip && preceding.MoveToNext());

			return skip;
		}
	}
}
