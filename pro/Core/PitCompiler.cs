using System;
using System.Collections.Generic;
using System.Linq;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Pro.Core.WebServices.Models;
using Array = Peach.Core.Dom.Array;
using Peach.Pro.Core.Publishers;
using Newtonsoft.Json;
using System.IO;
using NLog;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Schema;

namespace Peach.Pro.Core
{
	public class PitCompiler
	{
		private static NLog.Logger Logger = LogManager.GetCurrentClassLogger();
		private string _pitLibraryPath;
		private string _pitPath;
		private string _pitMetaPath;
		private Peach.Core.Dom.Dom _dom;
		private List<string> _errors = new List<string>();
		
		const string Namespace = "http://peachfuzzer.com/2012/Peach";
		const string SchemaLocation = "http://peachfuzzer.com/2012/Peach peach.xsd";		

		static readonly Dictionary<string, string[]> OptionalParams = new Dictionary<string, string[]>
		{
			{ "RawEther", new[] { "MinMTU", "MaxMTU", "MinFrameSize", "MaxFrameSize", "PcapTimeout" }},
			{ "RawV4", new[] { "MinMTU", "MaxMTU" }},
			{ "RawV6", new[] { "MinMTU", "MaxMTU" }},
			{ "Udp", new[] { "MinMTU", "MaxMTU" }},
			{ "DTls", new[] { "MinMTU", "MaxMTU" }},
			{ "File", new[] { "Append", "Overwrite" }},
			{ "ConsoleHex", new[] { "BytesPerLine" }},
			{ "Null", new[] { "MaxOutputSize" }}
		};

		public PitCompiler(string pitLibraryPath, string pitPath)
		{
			_pitLibraryPath = pitLibraryPath;
			_pitPath = pitPath;
			_pitMetaPath = MetaPath(pitPath);
		}

		public static PitMetadata LoadMetadata(string pitPath)
		{
			var input = MetaPath(pitPath);
			if (!File.Exists(input))
				return null;
			
			var serializer = new JsonSerializer();
			using (var stream = new StreamReader(input))
			using (var reader = new JsonTextReader(stream))
			{
				return serializer.Deserialize<PitMetadata>(reader);
			}
		}

		public IEnumerable<string> Run(bool verifyConfig = true, bool doLint = true)
		{
			try
			{
				Parse(verifyConfig, doLint);
				SaveMetadata();
			}
			catch (Exception ex)
			{
				_errors.Add(ex.Message);
				Logger.Debug(ex);
			}
			return _errors;
		}

		internal void SaveMetadata()
		{
			var metadata = new PitMetadata
			{
				Fields = MakeFields()
			};

			var serializer = new JsonSerializer();
			using (var stream = new StreamWriter(_pitMetaPath))
			using (var writer = new JsonTextWriter(stream))
			{
				writer.Formatting = Newtonsoft.Json.Formatting.Indented;
				serializer.Serialize(writer, metadata);
			}
		}

		class CustomParser : ProPitParser
		{
			protected override void handlePublishers(System.Xml.XmlNode node, Test parent)
			{
				// ignore publishers
				var args = new Dictionary<string, Variant>();
				var pub = new NullPublisher(args)
				{
					Name = node.getAttr("name", null) ?? parent.publishers.UniqueName()
				};
				parent.publishers.Add(pub);
			}
		}

		private static string MetaPath(string pitPath)
		{
			return Path.ChangeExtension(pitPath, ".meta.json");
		}

		internal void Parse(bool verifyConfig, bool doLint)
		{
			var defs = PitDefines.ParseFile(_pitPath + ".config", _pitLibraryPath);
			var defsWithDefaults = defs.Evaluate().Select(PitDefines.PopulateRequiredDefine);

			var args = new Dictionary<string, object> {
				{PitParser.DEFINED_VALUES, defsWithDefaults }
			};

			var parser = new CustomParser();
			_dom = parser.asParser(args, _pitPath);
			_dom.context = new RunContext();
			_dom.context.test = _dom.tests.First();

			if (verifyConfig)
				VerifyConfig(defs, args);

			if (doLint)
				VerifyPitFiles(_dom, true);
		}

		private void VerifyConfig(PitDefines defs, Dictionary<string, object> args)
		{
			var defsList = defs.Walk().ToList();
			_errors.AddRange(defsList
				.Where(d => d.ConfigType != ParameterType.Space && string.IsNullOrEmpty(d.Name))
				.Select(d => "PitDefine '{0}' missing 'Name' attribute.".Fmt(d.Key))
			);
			_errors.AddRange(defsList
				.Where(d => d.ConfigType != ParameterType.Space && string.IsNullOrEmpty(d.Description))
				.Select(d => "PitDefine '{0}' missing 'Description' attribute.".Fmt(d.Key ?? d.Name))
			);

			object objUsed;
			if (args.TryGetValue(PitParser.USED_DEFINED_VALUES, out objUsed))
			{
				var used = (HashSet<string>)objUsed;
				used.ForEach(x => defsList.RemoveAll(d => d.Key == x));
			}

			defsList.RemoveAll(d => d.ConfigType == ParameterType.Space || d.ConfigType == ParameterType.Group);

			_errors.AddRange(defsList.Select(d => "Detected unused PitDefine: '{0}'.".Fmt(d.Key)));

			if (defs.Platforms.Any(p => p.Platform != Platform.OS.All))
				_errors.Add("Configuration file should not have platform specific defines.");
		}

		private void VerifyPitFiles(Peach.Core.Dom.Dom dom, bool isTest)
		{
			VerifyPit(dom.fileName, isTest);
			foreach (var ns in dom.ns)
			{
				VerifyPitFiles(ns, false);
			}
		}

		internal List<PitField> MakeFields()
		{
			var root = new List<PitField>();

			foreach (var state in _dom.context.test.stateModel.states)
			{
				var stateNode = new PitField 
				{
					Id = state.FieldId ?? state.Name 
				};

				var isFirst = true;
				foreach (var action in state.actions)
				{
					var actionNode = new PitField 
					{
						Id = action.FieldId ?? action.Name
					};

					var hasData = false;
					var elementNodes = new List<PitField>();
					var fieldNodes = new List<PitField>();
					foreach (var actionData in action.outputData)
					{
						CollectNodes(actionData.dataModel.DisplayTraverse(), elementNodes, x => x.fullName);
						CollectNodes(actionData.dataModel.PreOrderTraverse(), fieldNodes, x => x.FullFieldId);
						hasData = true;
					}

					if (hasData)
					{
						if (isFirst)
						{
							root.Add(stateNode);
							isFirst = false;
						}

						stateNode.Fields.Add(actionNode);
						actionNode.Fields = fieldNodes.Any() ? fieldNodes : elementNodes;
					}
				}
			}

			return root;
		}

		private void CollectNodes(
			IEnumerable<DataElement> elements,
			List<PitField> rootFields,
			Func<DataElement, string> selector)
		{
			var fullNames = elements
				.Select(selector)
				.Distinct()
				.Where(x => x != null);

			foreach (var fullName in fullNames)
			{
				var parentFields = rootFields;
				var parts = fullName.Split('.');
				foreach (var part in parts)
				{
					var next = parentFields.SingleOrDefault(x => x.Id == part);
					if (next == null)
					{
						next = new PitField { Id = part };
						parentFields.Add(next);
					}
					parentFields = next.Fields;
				}
			}
		}

		private void VerifyPit(string fileName, bool isTest)
		{
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
							_errors.Add("Long form copyright message is missing.");
					}
					else if (rdr.NodeType == XmlNodeType.Element)
					{
						if (rdr.Name != "Peach")
						{
							_errors.Add("The first xml element is not <Peach>.");
							break;
						}

						if (!rdr.MoveToAttribute("description"))
							_errors.Add("Pit is missing description attribute.");
						else if (string.IsNullOrEmpty(rdr.Value))
							_errors.Add("Pit description is empty.");

						const string author = "Peach Fuzzer, LLC";

						if (!rdr.MoveToAttribute("author"))
							_errors.Add("Pit is missing author attribute.");
						else if (author != rdr.Value)
							_errors.Add("Pit author is '{0}' but should be '{1}'.".Fmt(rdr.Value, author));

						if (!rdr.MoveToAttribute("xmlns"))
							_errors.Add("Pit is missing xmlns attribute.");
						else if (Namespace != rdr.Value)
							_errors.Add("Pit xmlns is '{0}' but should be '{1}'.".Fmt(rdr.Value, Namespace));

						if (!rdr.MoveToAttribute("schemaLocation", XmlSchema.InstanceNamespace))
							_errors.Add("Pit is missing xsi:schemaLocation attribute.");
						else if (SchemaLocation != rdr.Value)
							_errors.Add("Pit xsi:schemaLocation is '{0}' but should be '{1}'.".Fmt(rdr.Value, SchemaLocation));

						break;
					}
				}

				if (idxDeclaration != 1)
					_errors.Add("Pit is missing xml declaration.");

				if (idxCopyright == 0)
					_errors.Add("Pit is missing top level copyright message.");

			}

			{
				var doc = new XmlDocument();

				// Must call LoadXml() so that we can catch embedded newlines!
				doc.LoadXml(File.ReadAllText(fileName));

				var nav = doc.CreateNavigator();
				var nsMgr = new XmlNamespaceManager(nav.NameTable);
				nsMgr.AddNamespace("p", Namespace);

				var it = nav.Select("/p:Peach/p:Test", nsMgr);

				var expected = isTest ? 1 : 0;

				if (it.Count != expected)
					_errors.Add("Number of <Test> elements is {0} but should be {1}.".Fmt(it.Count, expected));

				while (it.MoveNext())
				{
					var maxSize = it.Current.GetAttribute("maxOutputSize", string.Empty);
					if (string.IsNullOrEmpty(maxSize))
						_errors.Add("<Test> element is missing maxOutputSize attribute.");

					var lifetime = it.Current.GetAttribute("targetLifetime", string.Empty);
					if (string.IsNullOrEmpty(lifetime))
						_errors.Add("<Test> element is missing targetLifetime attribute.");

					if (!ShouldSkipRule(it, "Skip_Lifetime"))
					{
						var parts = fileName.Split(Path.DirectorySeparatorChar);
						var fileFuzzing = new[] { "Image", "Video", "Application" };
						if (parts.Any(fileFuzzing.Contains) || parts.Last().Contains("Client"))
						{
							if (lifetime != "iteration")
								_errors.Add("<Test> element has incorrect targetLifetime attribute. Expected 'iteration' but found '{0}'.".Fmt(lifetime));
						}
						else
						{
							if (lifetime != "session")
								_errors.Add("<Test> element has incorrect targetLifetime attribute. Expected 'session' but found '{0}'.".Fmt(lifetime));
						}
					}

					var loggers = it.Current.Select("p:Logger", nsMgr);
					if (loggers.Count != 0)
						_errors.Add("Number of <Logger> elements is {0} but should be 0.".Fmt(loggers.Count));

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
								_errors.Add(
									"<Publisher> parameter '{0}' is hard-coded, use a PitDefine ".Fmt(name) +
									"(suppress with 'Allow_HardCodedParamValue')"
								);
							}

							parms.Add(name);
						}

						var comments = pubs.Current.SelectChildren(XPathNodeType.Comment);
						while (comments.MoveNext())
						{
							var value = comments.Current.Value.Trim();
							const string ignore = "PitLint: Allow_MissingParamValue=";
							if (value.StartsWith(ignore))
								parms.Add(value.Substring(ignore.Length));
						}

						var pub = ClassLoader.FindPluginByName<PublisherAttribute>(cls);
						if (pub == null)
						{
							_errors.Add("<Publisher> class '{0}' is not recognized.".Fmt(cls));
						}
						else
						{
							var pri = pub.GetAttributes<PublisherAttribute>().First();
							if (pri.Name != cls)
								_errors.Add("'{0}' <Publisher> is referenced with deprecated name '{1}'.".Fmt(pri.Name, cls));

							string[] optionalParams;
							if (!OptionalParams.TryGetValue(pri.Name, out optionalParams))
								optionalParams = new string[0];

							foreach (var attr in pub.GetAttributes<ParameterAttribute>())
							{
								if (!optionalParams.Contains(attr.name) && !parms.Contains(attr.name))
									_errors.Add("{0} publisher missing configuration for parameter '{1}'.".Fmt(pri.Name, attr.name));
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
							_errors.Add(string.Format("StateModel '{0}' has an unexpected call action.  Method is '{1}' and should be 'StartIterationEvent' or 'ExitIterationEvent'.", smName, meth));
					}

					if (!gotStart)
						_errors.Add(string.Format("StateModel '{0}' does not call agent with 'StartIterationEvent'.", smName));

					if (!gotEnd)
						_errors.Add(string.Format("StateModel '{0}' does not call agent with 'ExitIterationEvent'.", smName));
				}

				var whenAction = nav.Select("/p:Peach/p:StateModel/p:State/p:Action[contains(@when, 'controlIteration')]", nsMgr);
				while (whenAction.MoveNext())
				{
					if (!ShouldSkipRule(whenAction, "Allow_WhenControlIteration"))
						_errors.Add("Action has when attribute containing controlIteration: {0}".Fmt(whenAction.Current.OuterXml));
				}

				var badValues = nav.Select("//*[contains(@value, '\n')]", nsMgr);
				while (badValues.MoveNext())
				{
					if (badValues.Current.GetAttribute("valueType", "") != "hex")
						_errors.Add("Element has value attribute with embedded newline: {0}".Fmt(badValues.Current.OuterXml));
				}
			}
		}

		private static bool ShouldSkipRule(XPathNodeIterator it, string rule)
		{
			var stack = new Stack<string>();
			var preceding = it.Current.Select("preceding-sibling::*|preceding-sibling::comment()");

			while (preceding.MoveNext())
			{
				if (preceding.Current.NodeType == XPathNodeType.Comment)
					stack.Push(preceding.Current.Value);
				else
					stack.Clear();
			}

			return stack.Any(item => item.Contains("PitLint: {0}".Fmt(rule)));
		}
	}
}
