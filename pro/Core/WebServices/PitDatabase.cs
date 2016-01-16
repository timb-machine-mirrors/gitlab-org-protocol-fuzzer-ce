using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using Peach.Core;
using Peach.Pro.Core.WebServices.Models;
using Encoding = System.Text.Encoding;
using File = System.IO.File;

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

		[XmlRoot("Test", Namespace = Namespace)]
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

		[XmlRoot("Agent", Namespace = Namespace)]
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
			[DefaultValue("")]
			public string Location { get; set; }

			[XmlElement("Monitor", typeof(MonitorElement))]
			public List<MonitorElement> Monitors { get; set; }
		}

		[XmlRoot("Param", Namespace = Namespace)]
		public class ParamElement : ChildElement
		{
			[XmlAttribute("name")]
			public string Name { get; set; }

			[XmlAttribute("value")]
			public string Value { get; set; }
		}

		[XmlRoot("Include", Namespace = Namespace)]
		public class IncludeElement : ChildElement
		{
			[XmlAttribute("ns")]
			public string Ns { get; set; }

			[XmlAttribute("src")]
			public string Source { get; set; }
		}

		[XmlRoot("StateModel", Namespace = Namespace)]
		public class StateModelElement : ChildElement
		{
			[XmlRoot("State", Namespace = Namespace)]
			public class StateElement
			{
				[XmlRoot("Action", Namespace = Namespace)]
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

		#region Static Helpers

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

		#endregion

		public PitDatabase()
		{
			entries = new Dictionary<string, PitDetail>();
			libraries = new NamedCollection<LibraryDetail>();
		}

		public PitDatabase(string libraryPath)
		{
			Load(libraryPath);
		}

		private string pitLibraryPath;

		public event EventHandler<ValidationEventArgs> ValidationEventHandler;
		public event EventHandler<LoadEventArgs> LoadEventHandler;

		class LibraryDetail : INamed
		{
			public string SubDir { get; set; }

			public Library Library { get; set; }

			[Obsolete]
			string INamed.name
			{
				get { return Library.LibraryUrl; }
			}

			string INamed.Name
			{
				get { return Library.LibraryUrl; }
			}
		}

		public void Load(string path)
		{
			pitLibraryPath = path;
			entries = new Dictionary<string, PitDetail>();
			libraries = new NamedCollection<LibraryDetail>();

			HashSet<string> old;
			lock (_cache)
			{
				old = new HashSet<string>(_cache.Keys);
			}

			AddLibrary("", "Pits", true);
			AddLibrary("User", "Configurations", false);

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

			var dstLib = GetLibraryDetailByUrl(libraryUrl);
			if (dstLib == null)
				throw new KeyNotFoundException("The destination pit library could not be found.");
			if (dstLib.Library.Locked)
				throw new UnauthorizedAccessException("The destination pit library is locked.");

			// Only support a single user library for now
			Debug.Assert(dstLib.Library.Name == "Configurations");

			var srcPit = GetPitByUrl(pitUrl);
			if (srcPit == null)
				throw new KeyNotFoundException("The source pit could not be found.");

			var srcFile = srcPit.Versions[0].Files[0].Name;
			var srcCat = srcPit.Tags[0].Values[1];

			var dstDir = Path.Combine(pitLibraryPath, dstLib.SubDir, srcCat);

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

			var item = AddEntry(dstLib, dstFile);

			return item.PitUrl;
		}

		private void SaveConfig(PitDetail detail, List<Param> config)
		{
			var defines = PitDefines.ParseFile(detail.ConfigFileName, pitLibraryPath);

			defines.ApplyWeb(config);

			XmlTools.Serialize(detail.ConfigFileName, defines);
		}

		private void SaveAgents(PitDetail detail, List<Models.Agent> agents)
		{
			detail.Agents = agents.FromWeb();

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

			using (var rdr = XmlReader.Create(detail.FileName, settingsRdr, parserCtx))
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
				throw new PeachException("Could not find a <Test> element in the pit '" + detail.FileName + "'.");

			using (var writer = test.InsertBefore())
			{
				var s = new XmlSerializer(typeof(PeachElement.AgentElement));
				var fragWriter = new XmlFragmentWriter(writer);

				foreach (var a in detail.Agents)
				{
					// Serialize <Agent> element
					s.Serialize(fragWriter, a);

					// Add <Agent ref='xxx' /> to the <Test> element
					// Use AppendChild so the agents stay in order
					using (var testWriter = test.AppendChild())
					{
						testWriter.WriteStartElement("Agent", PeachElement.Namespace);
						testWriter.WriteAttributeString("ref", a.Name);
						testWriter.WriteEndElement();
					}	
				}
			}

			var settings = new XmlWriterSettings
			{
				Indent = true,
				Encoding = Encoding.UTF8,
				IndentChars = "  ",
			};

			using (var writer = XmlWriter.Create(detail.FileName, settings))
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

		private void AddLibrary(string subdir, string name, bool locked)
		{
			var path = Path.Combine(pitLibraryPath, subdir);

			var guid = MakeGuid(name);

			var lib = new LibraryDetail
			{
				SubDir = subdir,

				Library = new Library
				{
					LibraryUrl = LibraryServicePrefix + "/" + guid,
					Name = name,
					Description = name,
					Locked = locked,
					Groups = new List<Group>
					{
						new Group
						{
							GroupUrl = "",
							Access = locked ? GroupAccess.Read : GroupAccess.Read | GroupAccess.Write
						}
					},
					User = Environment.UserName,
					Versions = new List<LibraryVersion>
					{
						new LibraryVersion
						{
							Version = 1,
							Locked = locked,
							Pits = new List<LibraryPit>()
						}
					}
				}
			};

			libraries.Add(lib);

			if (!Directory.Exists(path))
				return;

			lib.Library.Timestamp = Directory.GetCreationTime(path);

			foreach (var dir in Directory.EnumerateDirectories(path))
			{
				if (Path.GetDirectoryName(dir) == "User")
					continue;

				foreach (var file in Directory.EnumerateFiles(dir, "*.xml"))
				{
					try
					{
						var item = AddEntry(lib, file);

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

		public IEnumerable<LibraryPit> Entries
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
				return libraries.Select(l => l.Library);
			}
		}

		public Pit GetPitById(string guid)
		{
			var detail = GetPitDetailById(guid);
			if (detail == null)
				return null;

			return PopulatePit(detail);
		}

		public Pit GetPitByUrl(string url)
		{
			var detail = GetPitDetailByUrl(url);
			if (detail == null)
				return null;

			return PopulatePit(detail);
		}

		public Library GetLibraryById(string guid)
		{
			return GetLibraryByUrl(LibraryServicePrefix + "/" + guid);
		}

		public Library GetLibraryByUrl(string url)
		{
			var detail = GetLibraryDetailByUrl(url);
			if (detail == null)
				return null;

			return detail.Library;
		}

		private LibraryDetail GetLibraryDetailByUrl(string url)
		{
			LibraryDetail detail;
			libraries.TryGetValue(url, out detail);
			return detail;
		}

		public Pit UpdatePitById(string guid, Pit data)
		{
			var cfg = new PitConfig
			{
				Id = data.Id,
				Name = data.Name,
				Agents = data.Agents,
				Config = data.Config
			};

			return UpdatePitById(guid, cfg);
		}

		public Pit UpdatePitById(string guid, PitConfig data)
		{
			var detail = GetPitDetailById(guid);
			if (detail == null)
				throw new KeyNotFoundException();

			//if (detail.Pit.Locked)
			//	throw new UnauthorizedAccessException();

			if (data.Config != null)
				SaveConfig(detail, data.Config);

			if (data.Agents != null)
				SaveAgents(detail, data.Agents);

			//var fileName = detail.Pit.Versions[0].Files[0].Name;
			//var lastModified = File.GetLastWriteTimeUtc(fileName);

			//detail = MakePitDetail(fileName, detail.Pit.Id, detail.Pit.Locked, lastModified);

			//entries[detail.Pit.PitUrl] = detail;

			return PopulatePit(detail);
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

		private bool AnyFilesStale(PitDetail detail)
		{
			foreach (var file in detail.Pit.Versions.SelectMany(v => v.Files))
			{
				var fi = new System.IO.FileInfo(file.Name);
				if (!fi.Exists || file.Timestamp < fi.LastWriteTimeUtc)
					return true;
			}
			return false;
		}

		private Pit AddEntry(LibraryDetail lib, string fileName)
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
					detail = MakePitDetail(fileName, guid, lib.Library.Locked, lastModified);
					_cache[url] = detail;
				}
			}

			entries.Add(url, detail);

			lib.Library.Versions[0].Pits.Add(new LibraryPit
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
			string fileName,
			string guid,
			bool locked,
			DateTime lastModified)
		{
			var contents = Parse(fileName);

			var value = new Pit
			{
				Id = guid,
				PitUrl = PitServicePrefix + "/" + guid,
				Name = Path.GetFileNameWithoutExtension(fileName),
				Description = contents.Description,
				Locked = locked,
				Tags = new List<Tag>(),
				Versions = new List<PitVersion>(),
				Peaches = new List<PeachVersion>(),
				User = contents.Author,
				Timestamp = lastModified,
			};

			var ver = new PitVersion
			{
				Version = 1,
				Locked = value.Locked,
				Files = new List<PitFile>(),
				User = Environment.UserName,
				Timestamp = lastModified
			};

			var detail = new PitDetail(fileName)
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

				otherName = otherName.Replace("file:", "");
				otherName = otherName.Replace("##PitLibraryPath##", pitLibraryPath);

				// Normalize the path
				otherName = Path.Combine(Path.GetDirectoryName(otherName) ?? "", Path.GetFileName(otherName));

				var other = Parse(otherName);

				var newNs = string.IsNullOrEmpty(ns) ? inc.Ns : ns + ":" + inc.Ns;
				AddAllFiles(pit, list, pitLibraryPath, otherName, other, newNs);
			}
		}

		private class PitDetail
		{
			public PitDetail(string fileName)
			{
				FileName = fileName;
				ConfigFileName = fileName + ".config";
			}

			public string FileName { get; private set; }
			public string ConfigFileName { get; private set; }

			public Pit Pit { get; set; }
			public string StateModel { get; set; }
			public List<string> CallMethods { get; set; }
			public List<PeachElement.AgentElement> Agents { get; set; }
		}

		private Dictionary<string, PitDetail> entries;
		private NamedCollection<LibraryDetail> libraries;

		private static Dictionary<string, PitDetail> _cache = new Dictionary<string, PitDetail>();

		#region Pit Config/Agents/Metadata

		private Pit PopulatePit(PitDetail detail)
		{
			var defs = PitDefines.ParseFile(detail.ConfigFileName, pitLibraryPath);

			var pit = detail.Pit;

			pit.Config = defs.Flatten().Select(d => new Param { Key = d.Key, Value = d.Value }).ToList();
			pit.Agents = detail.Agents.ToWeb();
			pit.Metadata = new PitMetadata
			{
				Defines = defs.ToWeb(),
				Monitors = MonitorMetadata.Generate(detail.CallMethods)
			};

			return pit;
		}

		#endregion
	}
}
