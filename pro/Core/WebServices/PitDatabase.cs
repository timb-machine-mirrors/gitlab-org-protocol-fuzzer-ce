using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Dom;
using Peach.Pro.Core.WebServices.Models;
using Encoding = System.Text.Encoding;
using File = System.IO.File;
using Monitor = Peach.Pro.Core.WebServices.Models.Monitor;

namespace Peach.Pro.Core.WebServices
{
	#region Peach Pit Xml Elements

	[XmlRoot("Peach", Namespace = Namespace)]
	[Serializable]
	public class PeachElement
	{
		public const string Namespace = "http://peachfuzzer.com/2012/Peach";
		public const string SchemaLocation = "http://peachfuzzer.com/2012/Peach peach.xsd";

		public PeachElement()
		{
			Children = new List<ChildElement>();
		}

		public abstract class ChildElement
		{
		}

		public class TestElement : ChildElement
		{
			public class AgentReferenceElement : ChildElement
			{
				[XmlAttribute("ref")]
				public string Ref { get; set; }
			}

			public class StateModelReferenceElement : ChildElement
			{
				[XmlAttribute("ref")]
				public string Ref { get; set; }
			}

			public TestElement()
			{
				Children = new List<ChildElement>();
			}

			[XmlAttribute("name")]
			public string Name { get; set; }

			[XmlElement("Agent", typeof(AgentReferenceElement))]
			[XmlElement("StateModel", typeof(StateModelReferenceElement))]
			public List<ChildElement> Children { get; set; }

			public IEnumerable<AgentReferenceElement> AgentRefs
			{
				get { return Children.OfType<AgentReferenceElement>(); }
			}

			public IEnumerable<StateModelReferenceElement> StateModelRefs
			{
				get { return Children.OfType<StateModelReferenceElement>(); }
			}
		}

		public class AgentElement : ChildElement
		{
			public class MonitorElement : ChildElement
			{
				[XmlAttribute("class")]
				public string Class { get; set; }

				[XmlAttribute("name")]
				public string Name { get; set; }

				[XmlElement("Param", typeof(ParamElement))]
				public List<ParamElement> Params { get; set; }
			}

			[XmlAttribute("name")]
			public string Name { get; set; }

			[XmlAttribute("location")]
			public string Location { get; set; }

			[XmlElement("Monitor", typeof(MonitorElement))]
			public List<MonitorElement> Monitors { get; set; }
		}

		public class ParamElement : ChildElement
		{
			[XmlAttribute("name")]
			public string Name { get; set; }

			[XmlAttribute("value")]
			public string Value { get; set; }
		}

		public class IncludeElement : ChildElement
		{
			[XmlAttribute("ns")]
			public string Ns { get; set; }

			[XmlAttribute("src")]
			public string Source { get; set; }
		}

		public class StateModelElement : ChildElement
		{
			public class StateElement
			{
				public class ActionElement
				{
					[XmlAttribute("type")]
					public string Type { get; set; }

					[XmlAttribute("method")]
					public string Method { get; set; }

					[XmlAttribute("publisher")]
					public string Publisher { get; set; }
				}

				public StateElement()
				{
					Actions = new List<ActionElement>();
				}

				[XmlElement("Action", typeof(ActionElement))]
				public List<ActionElement> Actions { get; set; }
			}

			public StateModelElement()
			{
				States = new List<StateElement>();
			}

			[XmlAttribute("name")]
			public string Name { get; set; }

			[XmlAttribute("initialState")]
			public string InitialState { get; set; }

			[XmlElement("State", typeof(StateElement))]
			public List<StateElement> States { get; set; }
		}

		[XmlAttribute("author")]
		[DefaultValue("")]
		public string Author { get; set; }

		[XmlAttribute("description")]
		[DefaultValue("")]
		public string Description { get; set; }

		[XmlAttribute("version")]
		[DefaultValue("")]
		public string Version { get; set; }

		[XmlElement("Include", typeof(IncludeElement))]
		[XmlElement("Test", typeof(TestElement))]
		[XmlElement("Agent", typeof(AgentElement))]
		[XmlElement("StateModel", typeof(StateModelElement))]
		public List<ChildElement> Children { get; set; }
	}

	#endregion

	public class ValidationEventArgs : EventArgs
	{
		public ValidationEventArgs(Exception exception, string fileName)
		{
			Exception = exception;
			FileName = fileName;
		}

		public Exception Exception { get; private set; }

		public string FileName { get; private set; }
	}

	public class LoadEventArgs : EventArgs
	{
		public LoadEventArgs(Pit pit, string fileName)
		{
			Pit = pit;
			FileName = fileName;
		}

		public Pit Pit { get; private set; }

		public string FileName { get; private set; }
	}

	public class PitDatabase
	{
		public static readonly string PitServicePrefix = "/p/pits";
		public static readonly string LibraryServicePrefix = "/p/libraries";

		private static PeachElement Parse(string fileName)
		{
			try
			{
				var settingsRdr = new XmlReaderSettings
				{
					ValidationType = ValidationType.Schema,
					NameTable = new NameTable(),
				};

				// Default the namespace to peach
				var nsMgrRdr = new XmlNamespaceManager(settingsRdr.NameTable);
				nsMgrRdr.AddNamespace("", PeachElement.Namespace);

				var parserCtx = new XmlParserContext(settingsRdr.NameTable, nsMgrRdr, null, XmlSpace.Default);

				var s = new XmlSerializer(typeof(PeachElement));
				using (var rdr = XmlReader.Create(fileName, settingsRdr, parserCtx))
				{
					var elem = (PeachElement)s.Deserialize(rdr);
					return elem;
				}
			}
			catch (Exception ex)
			{
				throw new PeachException(
					"Dependency error: {0} -- {1}".Fmt(fileName, ex.Message), ex
				);
			}
		}

		private static string MakeGuid(string value)
		{
			using (var md5 = new MD5CryptoServiceProvider())
			{
				var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
				var sb = new StringBuilder();
				foreach (var b in bytes)
					sb.Append(b.ToString("x2"));
				return sb.ToString();
			}
		}

		private static readonly PeachVersion Version = MakePeachVer();

		private static PeachVersion MakePeachVer()
		{
			var ver = Assembly.GetExecutingAssembly().GetName().Version;

			return new PeachVersion
			{
				PeachUrl = "",
				Major = ver.Major.ToString(CultureInfo.InvariantCulture),
				Minor = ver.Minor.ToString(CultureInfo.InvariantCulture),
				Build = ver.Build.ToString(CultureInfo.InvariantCulture),
				Revision = ver.Revision.ToString(CultureInfo.InvariantCulture),
			};
		}

		private static void AddDefine(IList<KeyValuePair<string, string>> list, string key, string value)
		{
			for (var i = 0; i < list.Count; ++i)
			{
				if (list[i].Key != key)
					continue;

				list[i] = new KeyValuePair<string, string>(key, value);
				return;
			}

			list.Add(new KeyValuePair<string, string>(key, value));
		}

		/// <summary>
		/// Parses the .config and returns the object required by the PitParser to execute a pit
		/// </summary>
		/// <param name="pitLibraryPath"></param>
		/// <param name="pitConfig"></param>
		/// <returns></returns>
		public static List<KeyValuePair<string, string>> ParseConfig(string pitLibraryPath, string pitConfig)
		{
			var defs = new List<KeyValuePair<string, string>>();

			// It is ok if a .config doesn't exist
			if (File.Exists(pitConfig))
			{
				foreach (var d in PitDefines.Parse(pitConfig))
				{
					AddDefine(defs, d.Key, d.Value);
				}
			}


			AddDefine(defs, "Peach.OS", Platform.GetOS().ToString().ToLower());
			AddDefine(defs, "Peach.Cwd", Environment.CurrentDirectory);
			AddDefine(defs, "Peach.Pwd", Utilities.ExecutionDirectory);
			AddDefine(defs, "Peach.LogRoot", Configuration.LogRoot);
			AddDefine(defs, "PitLibraryPath", pitLibraryPath);

			var final = PitDefines.Evaluate(defs);

			return final;
		}

		public List<Monitor> GetAllMonitors()
		{
			var ret = new List<Monitor>();

			foreach (var kv in ClassLoader.GetAllByAttribute<MonitorAttribute>())
			{
#if !DEBUG
				if (kv.Key.Internal)
					continue;
#endif

				var m = MakeMonitor(kv.Key, kv.Value, null);

				ret.Add(m);
			}

			ret.Sort(MonitorSorter);

			return ret;
		}

		/// <summary>
		/// Get the list of all available calls that can be used with the specified pit guid.
		/// </summary>
		/// <param name="guid">Pit guid.</param>
		/// <returns>List of calls</returns>
		public List<string> GetCallsById(string guid)
		{
			return GetCallsByUrl(PitServicePrefix + "/" + guid);
		}

		public List<string> GetCallsByUrl(string url)
		{
			var pit = GetPitDetailByUrl(url);
			if (pit == null)
				return null;

			return pit.CallMethods;
		}

		internal Monitor MakeMonitor(MonitorAttribute attr, Type type, List<string> calls)
		{
			var os = "";
			if (attr.OS == Platform.OS.Unix)
			{
				var ex = new NotSupportedException("Monitor {0} specifies unsupported OS {1}".Fmt(attr.Name, attr.OS));
				if (ValidationEventHandler != null)
					ValidationEventHandler(this, new ValidationEventArgs(ex, ""));
			}
			else if (attr.OS != Platform.OS.All)
			{
				os = attr.OS.ToString();
			}

			var m = new Monitor
			{
				Description = type.GetAttributes<System.ComponentModel.DescriptionAttribute>().Select(a => a.Description).FirstOrDefault() ?? "",
				MonitorClass = attr.Name,
				Map = new List<Parameter>(),
				OS = os,
			};

			foreach (var p in type.GetAttributes<ParameterAttribute>())
			{
				var model = ParameterAttrToModel(attr.Name, p);

				if (model.Type == ParameterType.String && model.Name.Contains("OnCall"))
					model.Type = ParameterType.Call;

				m.Map.Add(model);
			}

			m.Map.Sort(ParameterSorter);
			return m;
		}

		public PitDatabase()
		{
			entries = new Dictionary<string, PitDetail>();
			libraries = new Dictionary<string, Library>();
			interfaces = null;
		}

		public PitDatabase(string libraryPath)
		{
			Load(libraryPath);
		}

		private string pitLibraryPath;

		public event EventHandler<ValidationEventArgs> ValidationEventHandler;
		public event EventHandler<LoadEventArgs> LoadEventHandler;

		public void Load(string path)
		{
			pitLibraryPath = path;
			roots = new Dictionary<string, LibraryRoot>();
			entries = new Dictionary<string, PitDetail>();
			libraries = new Dictionary<string, Library>();
			interfaces = null;

			HashSet<string> old;
			lock (_cache)
			{
				old = new HashSet<string>(_cache.Keys);
			}

			AddLibrary(path, "", "Pits", true);
			AddLibrary(path, "User", "Configurations", false);

			lock (_cache)
			{
				foreach (var key in old.Except(entries.Keys))
				{
					_cache.Remove(key);
				}
			}
		}

		/// <summary>
		/// 
		/// Throws:
		///   UnauthorizedAccessException if the destination library is locked.
		///   KeyNotFoundException if libraryUrl/pitUtl is not valid.
		///   ArgumentException if a pit with the specified name already exists.
		/// </summary>
		/// <param name="libraryUrl">The destination library to save the pit in.</param>
		/// <param name="pitUrl">The url of the source pit to copy.</param>
		/// <param name="name">The name of the newly copied pit.</param>
		/// <param name="description">The description of the newly copied pit.</param>
		/// <returns>The url of the newly copied pit.</returns>
		public string CopyPit(string libraryUrl, string pitUrl, string name, string description)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("A non-empty pit name is required.", "name");

			if (Path.GetFileName(name) != name)
				throw new ArgumentException("A valid pit name is required.", "name");

			var dstLib = GetLibraryByUrl(libraryUrl);
			if (dstLib == null)
				throw new KeyNotFoundException("The destination pit library could not be found.");
			if (dstLib.Locked)
				throw new UnauthorizedAccessException("The destination pit library is locked.");

			// Only support a single user library for now
			Debug.Assert(dstLib.Name == "Configurations");

			var srcPit = GetPitByUrl(pitUrl);
			if (srcPit == null)
				throw new KeyNotFoundException("The source pit could not be found.");

			var dstRoot = roots[dstLib.LibraryUrl];
			var srcFile = srcPit.Versions[0].Files[0].Name;
			var srcCat = srcPit.Tags[0].Values[1];

			var dstDir = Path.Combine(dstRoot.PitLibraryPath, dstRoot.SubDir, srcCat);

			if (!Directory.Exists(dstDir))
				Directory.CreateDirectory(dstDir);

			var dstFile = Path.Combine(dstDir, name + ".xml");

			if (File.Exists(dstFile))
				throw new ArgumentException("A pit already exists with the specified name.");

			var doc = new XmlDocument();

			using (var rdr = XmlReader.Create(srcFile))
			{
				doc.Load(rdr);
			}

			var nav = doc.CreateNavigator();

			Debug.Assert(nav.NameTable != null);

			var nsMgr = new XmlNamespaceManager(nav.NameTable);
			nsMgr.AddNamespace("p", PeachElement.Namespace);

			var it = nav.SelectSingleNode("/p:Peach", nsMgr);

			SetPitAttr(it, "author", Environment.UserName);
			SetPitAttr(it, "description", description);

			try
			{
				var settings = new XmlWriterSettings
				{
					Indent = true,
					Encoding = Encoding.UTF8,
					IndentChars = "  ",
				};

				using (var writer = XmlWriter.Create(dstFile, settings))
				{
					doc.WriteTo(writer);
				}

				// If there is a config file, copy it over
				if (File.Exists(srcFile + ".config"))
					File.Copy(srcFile + ".config", dstFile + ".config");
			}
			catch
			{
				try
				{
					if (File.Exists(srcFile))
						File.Delete(dstFile);
				}
				// ReSharper disable once EmptyGeneralCatchClause
				catch
				{
				}

				try
				{
					if (File.Exists(srcFile + ".config"))
						File.Delete(dstFile + ".config");
				}
				// ReSharper disable once EmptyGeneralCatchClause
				catch
				{
				}

				throw;
			}

			var item = AddEntry(dstLib.Versions[0], dstRoot.PitLibraryPath, dstFile);

			return item.PitUrl;
		}

		/// <summary>
		/// Save Pit Config
		/// </summary>
		/// <param name="pit"></param>
		/// <param name="config"></param>
		/// <remarks>
		/// only allow user parameters to add/delete
		/// only allow user parameters where the key doesn't collide with system parameters
		/// only allow system parameter values to be updated
		/// </remarks>
		public static void SaveConfig(Pit pit, List<Parameter> config)
		{
			var fileName = pit.Versions[0].Files[0].Name + ".config";

			var defines = new List<PitDefines.Define>();
			var reserved = new HashSet<string>();

			if (File.Exists(fileName))
			{
				foreach (var def in PitDefines.Parse(fileName))
				{
					if (def.ConfigType != ParameterType.User)
					{
						var param = config.SingleOrDefault(x => x.Key == def.Key && x.Type != ParameterType.System);
						if (param != null)
						{
							def.Value = param.Value;
						}
						defines.Add(def);
						reserved.Add(def.Key);
					}
				}
			}

			foreach (var param in config)
			{
				if (param.Type == ParameterType.User && !reserved.Contains(param.Key))
				{
					defines.Add(new PitDefines.UserDefine
					{
						Key = param.Key,
						Name = param.Name,
						Value = param.Value,
						Description = param.Description
					});
				}
			}

			var final = new PitDefines
			{
				Platforms = new List<PitDefines.Collection>(new[] {
					new PitDefines.All
					{
						Defines = defines.ToList(),
					}
				}),
			};

			XmlTools.Serialize(fileName, final);
		}

		public List<Models.Agent> GetAgentsById(string guid)
		{
			return GetAgentsByUrl(PitServicePrefix + "/" + guid);
		}

		public List<Models.Agent> GetAgentsByUrl(string url)
		{
			var pit = GetPitDetailByUrl(url);
			if (pit == null)
				return null;

			var ret = new List<Models.Agent>();
			foreach (var agent in pit.Agents)
			{
				var a = new Models.Agent
				{
					AgentUrl = agent.Location,
					Name = agent.Name,
					Monitors = new List<Monitor>()
				};

				foreach (var monitor in agent.Monitors)
				{
					var m = new Monitor
					{
						Name = monitor.Name,
						MonitorClass = monitor.Class,
						Map = new List<Parameter>()
					};

					var monitor1 = monitor;
					var type = ClassLoader.FindPluginByName<MonitorAttribute>(monitor1.Class);
					if (type == null)
					{
						// No plugin found, make up some reasonable content
						foreach (var param in monitor.Params)
						{
							m.Map.Add(new Parameter
							{
								Name = param.Name,
								Value = param.Value,
								Type = ParameterType.String,
							});
						}
					}
					else
					{
						m.Description = type.GetAttributes<System.ComponentModel.DescriptionAttribute>()
							.Select(d => d.Description)
							.FirstOrDefault() ?? "";

						foreach (var attr in type.GetAttributes<ParameterAttribute>())
						{
							var p = ParameterAttrToModel(monitor.Class, attr);

							p.Value = monitor.Params
								.Where(i => i.Name == attr.name)
								.Select(i => i.Value)
								.FirstOrDefault() ?? p.Value;

							m.Map.Add(p);
						}
					}

					m.Map.Sort(ParameterSorter);

					a.Monitors.Add(m);
				}
				ret.Add(a);
			}

			return ret;
		}

		private static int ParameterSorter(Parameter lhs, Parameter rhs)
		{
			if (IsRequired(lhs) == IsRequired(rhs))
				return string.CompareOrdinal(lhs.Name, rhs.Name);

			return IsRequired(lhs) ? -1 : 1;
		}

		private static int MonitorSorter(Monitor lhs, Monitor rhs)
		{
			return string.CompareOrdinal(lhs.MonitorClass, rhs.MonitorClass);
		}

		public static void SaveAgents(Pit pit, List<Models.Agent> agents)
		{
			var fileName = pit.Versions[0].Files[0].Name;
			var doc = new XmlDocument();

			var settingsRdr = new XmlReaderSettings
			{
				ValidationType = ValidationType.Schema,
				NameTable = new NameTable(),
			};

			// Default the namespace to peach
			var nsMgrRdr = new XmlNamespaceManager(settingsRdr.NameTable);
			nsMgrRdr.AddNamespace("", PeachElement.Namespace);

			var parserCtx = new XmlParserContext(settingsRdr.NameTable, nsMgrRdr, null, XmlSpace.Default);

			using (var rdr = XmlReader.Create(fileName, settingsRdr, parserCtx))
			{
				doc.Load(rdr);
			}

			var nav = doc.CreateNavigator();

			Debug.Assert(nav.NameTable != null);

			var nsMgr = new XmlNamespaceManager(nav.NameTable);
			nsMgr.AddNamespace("p", PeachElement.Namespace);

			while (true)
			{
				// Have to select one at a time and delete one at a time
				// to work on mono and windows.
				var oldAgent = nav.SelectSingleNode("//p:Agent", nsMgr);

				if (oldAgent != null)
					oldAgent.DeleteSelf();
				else
					break;
			}

			var test = nav.SelectSingleNode("/p:Peach/p:Test", nsMgr);
			if (test == null)
				throw new PeachException("Could not find a <Test> element in the pit '" + fileName + "'.");

			var final = new OrderedDictionary<string, XmlWriter>();
			foreach (var item in agents)
			{
				XmlWriter w;
				var agentKey = item.AgentUrl ?? "";
				if (!final.TryGetValue(agentKey, out w))
				{
					var agentName = "Agent" + final.Count;
					if (!string.IsNullOrEmpty(item.Name))
						agentName = item.Name;

					w = test.InsertBefore();
					w.WriteStartElement("Agent", PeachElement.Namespace);
					w.WriteAttributeString("name", agentName);
					if (!string.IsNullOrEmpty(item.AgentUrl))
						w.WriteAttributeString("location", item.AgentUrl);

					// AppendChild so the agents stay in order
					using (var testWriter = test.AppendChild())
					{
						testWriter.WriteStartElement("Agent", PeachElement.Namespace);
						testWriter.WriteAttributeString("ref", agentName);
						testWriter.WriteEndElement();
					}

					final.Add(agentKey, w);
				}

				foreach (var m in item.Monitors)
				{
					w.WriteStartElement("Monitor", PeachElement.Namespace);
					w.WriteAttributeString("class", m.MonitorClass);
					if (!string.IsNullOrEmpty(m.Name))
						w.WriteAttributeString("name", m.Name);

					foreach (var p in m.Map)
					{
						// TODO: don't trust user input, use reflection as canonical source of metadata
						if (string.IsNullOrEmpty(p.Value) || p.Value == p.DefaultValue)
							continue;

						w.WriteStartElement("Param", PeachElement.Namespace);

						if (p.Name == "StartMode")
						{
							if (p.Value == "StartOnCall") // File fzzing
							{
								w.WriteAttributeString("name", "StartOnCall");
								w.WriteAttributeString("value", "ExitIterationEvent");
							}
							else if (p.Value == "RestartOnEachTest") // Network client
							{
								w.WriteAttributeString("name", "StartOnCall");
								w.WriteAttributeString("value", "StartIterationEvent");
								w.WriteEndElement();
								w.WriteStartElement("Param", PeachElement.Namespace);
								w.WriteAttributeString("name", "WaitForExitOnCall");
								w.WriteAttributeString("value", "ExitIterationEvent");
							}
							else // Network server
							{
								w.WriteAttributeString("name", "RestartOnEachTest");
								w.WriteAttributeString("value", "false");
							}
						}
						else
						{
							w.WriteAttributeString("name", p.Name);
							w.WriteAttributeString("value", p.Value);
						}

						w.WriteEndElement();
					}

					w.WriteEndElement();
				}
			}

			foreach (var a in final)
			{
				a.Value.Close();
			}

			var settings = new XmlWriterSettings
			{
				Indent = true,
				Encoding = Encoding.UTF8,
				IndentChars = "  ",
			};

			using (var writer = XmlWriter.Create(fileName, settings))
			{
				doc.WriteTo(writer);
			}
		}

		private static void SetPitAttr(XPathNavigator nav, string attribute, string value)
		{
			if (nav.MoveToAttribute(attribute, ""))
			{
				nav.SetValue(value);
				nav.MoveToParent();
			}
			else
			{
				nav.CreateAttribute(string.Empty, attribute, string.Empty, value);
			}
		}

		private void AddLibrary(string root, string subdir, string name, bool locked)
		{
			var path = Path.Combine(root, subdir);

			var guid = MakeGuid(name);

			var lib = new Library
			{
				LibraryUrl = LibraryServicePrefix + "/" + guid,
				Name = name,
				Description = name,
				Locked = locked,
				Versions = new List<LibraryVersion>(),
				Groups = new List<Group>(),
				User = Environment.UserName,
				Timestamp = string.IsNullOrEmpty(path) ? new DateTime(0) : Directory.GetCreationTime(path),
			};

			var ver = new LibraryVersion
			{
				Version = 1,
				Locked = lib.Locked,
				Pits = new List<LibraryPit>(),
			};

			var group = new Group
			{
				GroupUrl = "",
				Access = GroupAccess.Read,
			};

			if (!ver.Locked)
				group.Access |= GroupAccess.Write;

			lib.Versions.Add(ver);
			lib.Groups.Add(group);
			libraries.Add(lib.LibraryUrl, lib);
			roots.Add(lib.LibraryUrl, new LibraryRoot { PitLibraryPath = root, SubDir = subdir });

			if (!Directory.Exists(path))
				return;

			foreach (var dir in Directory.EnumerateDirectories(path))
			{
				if (Path.GetDirectoryName(dir) == "User")
					continue;

				foreach (var file in Directory.EnumerateFiles(dir, "*.xml"))
				{
					try
					{
						var item = AddEntry(ver, root, file);

						if (LoadEventHandler != null)
							LoadEventHandler(this, new LoadEventArgs(item, file));
					}
					catch (Exception ex)
					{
						if (ValidationEventHandler != null)
							ValidationEventHandler(this, new ValidationEventArgs(ex, file));
					}
				}
			}
		}

		public IEnumerable<Pit> Entries
		{
			get
			{
				return entries.Select(kv => kv.Value.Pit);
			}
		}

		public IEnumerable<Library> Libraries
		{
			get
			{
				return libraries.Values;
			}
		}

		private IEnumerable<NetworkInterface> Interfaces
		{
			get
			{
				// ReSharper disable once ConvertIfStatementToNullCoalescingExpression
				if (interfaces == null)
					interfaces = NetworkInterface.GetAllNetworkInterfaces()
						.Where(i => i.OperationalStatus == OperationalStatus.Up)
						.Where(i => i.NetworkInterfaceType == NetworkInterfaceType.Ethernet
							|| i.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
							|| i.NetworkInterfaceType == NetworkInterfaceType.Loopback)
						.ToList();

				return interfaces;
			}
		}

		public Pit GetPitById(string guid)
		{
			var detail = GetPitDetailById(guid);
			if (detail == null)
				return null;
			return detail.Pit;
		}

		public Pit GetPitByUrl(string url)
		{
			var detail = GetPitDetailByUrl(url);
			if (detail == null)
				return null;
			return detail.Pit;
		}

		public Library GetLibraryById(string guid)
		{
			return GetLibraryByUrl(LibraryServicePrefix + "/" + guid);
		}

		public Library GetLibraryByUrl(string url)
		{
			Library library;
			libraries.TryGetValue(url, out library);
			return library;
		}

		public List<Parameter> GetConfigById(string guid)
		{
			return GetConfigByUrl(PitServicePrefix + "/" + guid);
		}

		public List<Parameter> GetConfigByUrl(string url)
		{
			var pit = GetPitByUrl(url);
			if (pit == null)
				return null;

			var fileName = pit.Versions[0].Files[0].Name + ".config";

			var defs = File.Exists(fileName)
				? PitDefines.Parse(fileName)
				: new List<PitDefines.Define>();

			return MakeConfig(defs);
		}

		public List<Parameter> MakeConfig(List<PitDefines.Define> defines)
		{
			var ret = new List<Parameter>()
			{
				new Parameter()
				{
					Type = ParameterType.System,
					Key = "Peach.OS",
					Name = "Peach OS",
					Description = "The OS that Peach is locally running",
					Value = Platform.GetOS().ToString().ToLower(),
				},
				new Parameter()
				{
					Type = ParameterType.System,
					Key = "Peach.Pwd",
					Name = "Peach Installation Directory",
					Description = "Full path to Peach installation",
					Value = Utilities.ExecutionDirectory,
				},
				new Parameter()
				{
					Type = ParameterType.System,
					Key = "Peach.Cwd",
					Name = "Peach Working Directory",
					Description = "Full path to the current working directory",
					Value = Environment.CurrentDirectory,
				},
				new Parameter()
				{
					Type = ParameterType.System,
					Key = "Peach.LogRoot",
					Name = "Root Log Directory",
					Description = "Full path to the root log directory",
					Value = Configuration.LogRoot,
				},
				new Parameter()
				{
					Type = ParameterType.System,
					Key = "PitLibraryPath",
					Name = "Pit Library Path",
					Description = "Path to root of Pit Library",
					Value = pitLibraryPath,
				},
			};

			foreach (var d in defines)
			{
				// Don't present this to the user
				if (d.Key == "PitLibraryPath")
					continue;

				var item = new Parameter
				{
					Type = d.ConfigType,
					Key = d.Key,
					Value = d.Value,
					Name = d.Name,
					Description = d.Description,
					Optional = d.Optional,
					Options = d.Defaults.ToList(),
					Min = d.Min,
					Max = d.Max,
				};

				switch (item.Type)
				{
					case ParameterType.Hwaddr:
						item.Options.AddRange(
							Interfaces
								.Select(i => i.GetPhysicalAddress().GetAddressBytes())
								.Select(a => string.Join(":", a.Select(b => b.ToString("x2"))))
								.Where(s => !string.IsNullOrEmpty(s)));
						break;
					case ParameterType.Iface:
						item.Options.AddRange(Interfaces.Select(i => i.Name));
						break;
					case ParameterType.Ipv4:
						item.Options.AddRange(
							Interfaces
								.SelectMany(i => i.GetIPProperties().UnicastAddresses)
								.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
								.Select(a => a.Address.ToString()));
						break;
					case ParameterType.Ipv6:
						item.Options.AddRange(
							Interfaces
								.SelectMany(i => i.GetIPProperties().UnicastAddresses)
								.Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6)
								.Select(a => a.Address.ToString()));
						break;
					case ParameterType.Bool:
						item.Options.AddRange(new[] {"true", "false"});
						break;
				}

				ret.Add(item);
			}

			return ret;
		}

		internal Parameter ParameterAttrToModel(string monitorClass, ParameterAttribute attr)
		{
			var p = new Parameter
			{
				Name = attr.name,
				DefaultValue = attr.required ? null : attr.defaultValue,
				Optional = !attr.required,
				Description = attr.description
			};

			var key = attr.type.Name;
			if (attr.type.IsGenericType)
			{
				key = attr.type.GetGenericArguments().First().Name;
			}
			else if (attr.type.IsEnum)
			{
				key = "Enum";
			}

			switch (key)
			{
				case "String":
				case "String[]":
				case "Int32[]":
					p.Type = ParameterType.String;
					break;
				case "UInt16":
					p.Type = ParameterType.Range;
					p.Max = UInt16.MaxValue;
					p.Min = UInt16.MinValue;
					break;
				case "UInt32":
					p.Type = ParameterType.Range;
					p.Max = UInt32.MaxValue;
					p.Min = UInt32.MinValue;
					break;
				case "Int32":
					p.Type = ParameterType.Range;
					p.Max = Int32.MaxValue;
					p.Min = Int32.MinValue;
					break;
				case "Boolean":
					p.Type = ParameterType.Bool;
					p.Options = new List<string> { "true", "false" };
					break;
				case "Enum":
					p.Type = ParameterType.Enum;
					p.Options = Enum.GetNames(attr.type).ToList();
					break;
				case "IPAddress":
					p.Type = ParameterType.Ipv4;
					break;
				default:
					p.Type = ParameterType.String;

					var ex =
						new NotSupportedException("Monitor {0} has invalid parameter type {1}".Fmt(monitorClass, attr.type.FullName));
					if (ValidationEventHandler != null)
						ValidationEventHandler(this, new ValidationEventArgs(ex, ""));

					break;
			}

			if (attr.name.Contains("OnCall"))
				p.Type = ParameterType.Call;

			return p;
		}

		private PitDetail GetPitDetailById(string guid)
		{
			return GetPitDetailByUrl(PitServicePrefix + "/" + guid);	
		}

		private PitDetail GetPitDetailByUrl(string url)
		{
			PitDetail pit;
			entries.TryGetValue(url, out pit);
			return pit;
		}

		private static bool IsRequired(Parameter param)
		{
			return param.DefaultValue == null;
		}

		private static bool IsConfigured(PeachElement elem)
		{
			// Pit is 'configured' if there is a <Test> with an <Agent ref='xxx'/> child
			return elem.Children.OfType<PeachElement.TestElement>().Any(e => e.AgentRefs.Any());
		}

		private bool AnyFilesStale(PitDetail detail)
		{
			foreach (var version in detail.Pit.Versions)
			{
				foreach (var file in version.Files)
				{
					var fi = new System.IO.FileInfo(file.Name);
					if (!fi.Exists || file.Timestamp < fi.LastWriteTimeUtc)
						return true;
				}
			}
			return false;
		}

		private Pit AddEntry(LibraryVersion lib, string pitLibraryPath, string fileName)
		{
			var guid = MakeGuid(fileName);
			var url = PitServicePrefix + "/" + guid;
			var lastModified = File.GetLastWriteTimeUtc(fileName);

			PitDetail detail;
			lock (_cache)
			{
				if (!_cache.TryGetValue(url, out detail) ||
					detail.Pit.Timestamp < lastModified ||
					AnyFilesStale(detail))
				{
					detail = MakePitDetail(lib, pitLibraryPath, fileName, guid, lastModified);
					_cache[url] = detail;
				}
			}

			entries.Add(url, detail);

			lib.Pits.Add(new LibraryPit
			{
				Id = detail.Pit.Id,
				PitUrl = url,
				Name = detail.Pit.Name,
				Description = detail.Pit.Description,
				Tags = detail.Pit.Tags,
			});

			return detail.Pit;
		}

		private PitDetail MakePitDetail(
			LibraryVersion lib, 
			string pitLibraryPath, 
			string fileName,
			string guid,
			DateTime lastModified)
		{
			var contents = Parse(fileName);

			var value = new Pit
			{
				Id = guid,
				PitUrl = PitServicePrefix + "/" + guid,
				Name = Path.GetFileNameWithoutExtension(fileName),
				Description = contents.Description,
				Locked = lib.Locked,
				Tags = new List<Tag>(),
				Versions = new List<PitVersion>(),
				Peaches = new List<PeachVersion>(),
				User = contents.Author,
				Timestamp = lastModified,
			};

			var ver = new PitVersion
			{
				Version = 1,
				Configured = IsConfigured(contents),
				Locked = value.Locked,
				Files = new List<PitFile>(),
				User = Environment.UserName,
				Timestamp = lastModified
			};

			var detail = new PitDetail
			{
				Pit = value,
				StateModel =
					contents.Children.OfType<PeachElement.TestElement>()
						.SelectMany(t => t.StateModelRefs)
						.Select(s => s.Ref)
						.FirstOrDefault(),
				CallMethods = new List<string>(),
				Agents = contents.Children.OfType<PeachElement.AgentElement>().ToList(),
			};

			AddAllFiles(detail, ver.Files, pitLibraryPath, fileName, contents, "");

			value.Versions.Add(ver);

			var dir = (Path.GetDirectoryName(fileName) ?? "").Split(Path.DirectorySeparatorChar).Last();
			var tag = new Tag
			{
				Name = "Category." + dir,
				Values = new List<string>(new[] { "Category", dir }),
			};

			value.Tags.Add(tag);
			value.Peaches.Add(Version);

			return detail;
		}

		private static void AddAllFiles(
			PitDetail pit, 
			ICollection<PitFile> list, 
			string pitLibraryPath, 
			string fileName, 
			PeachElement contents, 
			string ns)
		{
			list.Add(new PitFile
			{
				Name = fileName,
				FileUrl = "",
				Timestamp = File.GetLastWriteTimeUtc(fileName),
			});

			foreach (var sm in contents.Children.OfType<PeachElement.StateModelElement>())
			{
				var name = string.IsNullOrEmpty(ns) ? sm.Name : ns + ":" + sm.Name;
				if (pit.StateModel == name)
				{
					pit.CallMethods.AddRange(
						sm.States.SelectMany(s => s.Actions)
							.Where(a => a.Type == "call" && a.Publisher == "Peach.Agent")
							.Select(a => a.Method));
				}
			}

			foreach (var inc in contents.Children.OfType<PeachElement.IncludeElement>())
			{
				var otherName = inc.Source;

				if (!otherName.StartsWith("file:"))
					continue;

				otherName = otherName.Replace("file:", "");
				otherName = otherName.Replace("##PitLibraryPath##", pitLibraryPath);

				// Normalize the path
				otherName = Path.Combine(Path.GetDirectoryName(otherName) ?? "", Path.GetFileName(otherName));

				var other = Parse(otherName);

				var newNs = string.IsNullOrEmpty(ns) ? inc.Ns : ns + ":" + inc.Ns;
				AddAllFiles(pit, list, pitLibraryPath, otherName, other, newNs);
			}
		}

		class LibraryRoot
		{
			public string PitLibraryPath { get; set; }
			public string SubDir { get; set; }
		}

		class PitDetail
		{
			public Pit Pit { get; set; }
			public string StateModel { get; set; }
			public List<string> CallMethods { get; set; }
			public List<PeachElement.AgentElement> Agents { get; set; }
		}

		private Dictionary<string, LibraryRoot> roots;
		private Dictionary<string, PitDetail> entries;
		private Dictionary<string, Library> libraries;
		private List<NetworkInterface> interfaces;
		private static Dictionary<string, PitDetail> _cache = new Dictionary<string, PitDetail>();
	}
}
