using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Xml;
using Peach.Core;
using Peach.Pro.Core.WebServices.Models;
using Encoding = System.Text.Encoding;
using File = System.IO.File;
using Newtonsoft.Json;

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

	public class PitDetail : INamed
	{
		public string Path { get; set; }
		public Pit Pit { get; set; }

		[Obsolete]
		string INamed.name
		{
			get { return Pit.PitUrl; }
		}

		string INamed.Name
		{
			get { return Pit.PitUrl; }
		}
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

				using (var rdr = XmlReader.Create(fileName, settingsRdr, parserCtx))
				{
					var s = XmlTools.GetSerializer(typeof(PeachElement));
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

			return new PeachVersion {
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
			_entries = new NamedCollection<PitDetail>();
			_libraries = new NamedCollection<LibraryDetail>();
		}

		public PitDatabase(string libraryPath)
		{
			Load(libraryPath);
		}

		internal static readonly string LegacyDir = "User";
		internal static readonly string ConfigsDir = "Configs";

		private string _pitLibraryPath;

		private NamedCollection<PitDetail> _entries;
		private NamedCollection<LibraryDetail> _libraries;
		LibraryDetail _configsLib;

		public event EventHandler<ValidationEventArgs> ValidationEventHandler;
		public event EventHandler<PitDetail> LoadEventHandler;

		class LibraryDetail : INamed
		{
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
			_pitLibraryPath = path;

			_entries = new NamedCollection<PitDetail>();
			_libraries = new NamedCollection<LibraryDetail>();

			AddLibrary("", "Pits", true, false);
			_configsLib = AddLibrary(ConfigsDir, "Configurations", false, false);
			AddLibrary(LegacyDir, "Legacy", true, true);
		}

		private LibraryDetail AddLibrary(string subdir, string name, bool locked, bool legacy)
		{
			var path = Path.Combine(_pitLibraryPath, subdir);

			var guid = MakeGuid(name);

			var lib = new LibraryDetail
			{
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
							Version = legacy ? 1 : 2,
							Locked = locked,
							Pits = new List<LibraryPit>()
						}
					}
				}
			};

			_libraries.Add(lib);

			if (Directory.Exists(path))
			{
				lib.Library.Timestamp = Directory.GetCreationTime(path);

				foreach (var dir in Directory.EnumerateDirectories(path))
				{
					var dirName = Path.GetDirectoryName(dir);
					if (locked && (dirName == LegacyDir || dirName == ConfigsDir))
						continue;

					var searchPattern = locked ? "*.xml" : "*.peach";
					foreach (var file in Directory.EnumerateFiles(dir, searchPattern))
					{
						try
						{
							var item = AddEntry(lib, file);
							if (LoadEventHandler != null)
								LoadEventHandler(this, item);
						}
						catch (Exception ex)
						{
							if (ValidationEventHandler != null)
								ValidationEventHandler(this, new ValidationEventArgs(ex, file));
						}
					}
				}
			}

			return lib;
		}

		private PitDetail AddEntry(LibraryDetail lib, string fileName)
		{
			var detail = lib.Library.Locked ? 
				MakePitDetail(fileName) : 
				LoadPitDetail(fileName);
			_entries.Add(detail);

			lib.Library.Versions[0].Pits.Add(new LibraryPit {
				Id = detail.Pit.Id,
				PitUrl = detail.Pit.PitUrl,
				Name = detail.Pit.Name,
				Description = detail.Pit.Description,
				Tags = detail.Pit.Tags,
			});

			return detail;
		}

		private string GetCategory(string path)
		{
			return (Path.GetDirectoryName(path) ?? "").Split(Path.DirectorySeparatorChar).Last();
		}

		private string GetRelativePath(string path)
		{
			var len = _pitLibraryPath.Length;
			if (!_pitLibraryPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
				len++;
			return path.Substring(len);
		}

		private PitDetail MakePitDetail(string fileName)
		{
			var relativePath = GetRelativePath(fileName);
			var guid = MakeGuid(relativePath);
			var lastModified = File.GetLastWriteTimeUtc(fileName);
			var dir = GetCategory(fileName);
			var tag = new Tag {
				Name = "Category." + dir,
				Values = new List<string> { "Category", dir },
			};

			var pit = new Pit {
				OriginalPit = relativePath,
				Id = guid,
				PitUrl = PitServicePrefix + "/" + guid,
				Name = Path.GetFileNameWithoutExtension(fileName),
				Description = "", // TODO: get actual description
				Locked = true,
				Tags = new List<Tag> { tag },
				Timestamp = lastModified,
				User = Environment.UserName,
				Peaches = new List<PeachVersion> { Version },
			};

			return new PitDetail {
				Path = fileName,
				Pit = pit,
			};
		}

		private PitDetail LoadPitDetail(string fileName)
		{
			var lastModified = File.GetLastWriteTimeUtc(fileName);

			var pit = LoadPit(fileName);

			pit.User = Environment.UserName;
			pit.Timestamp = lastModified;

			return new PitDetail {
				Path = fileName,
				Pit = pit,
			};
		}

		public IEnumerable<PitDetail> PitDetails
		{
			get { return _entries; }
		}

		public IEnumerable<LibraryPit> Entries
		{
			get { return _entries.Select(item => item.Pit); }
		}

		public IEnumerable<Library> Libraries
		{
			get { return _libraries.Select(item => item.Library); }
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

		private Library GetLibraryByUrl(string url)
		{
			var detail = GetLibraryDetailByUrl(url);
			if (detail == null)
				return null;

			return detail.Library;
		}

		private LibraryDetail GetLibraryDetailByUrl(string url)
		{
			LibraryDetail detail;
			_libraries.TryGetValue(url, out detail);
			return detail;
		}

		/// <summary>
		/// 
		/// Throws:
		///   UnauthorizedAccessException if the destination library is locked.
		///   KeyNotFoundException if libraryUrl/pitUtl is not valid.
		///   ArgumentException if a pit with the specified name already exists.
		/// </summary>
		/// <param name="pitUrl">The url of the source pit to copy.</param>
		/// <param name="name">The name of the newly copied pit.</param>
		/// <param name="description">The description of the newly copied pit.</param>
		/// <returns>The newly copied pit.</returns>
		public PitDetail CopyPit(string pitUrl, string name, string description)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("A non-empty pit name is required.", "name");

			if (Path.GetFileName(name) != name)
				throw new ArgumentException("A valid pit name is required.", "name");

			var srcPit = GetPitDetailByUrl(pitUrl);
			if (srcPit == null)
				throw new KeyNotFoundException("The original pit could not be found.");

			var srcFile = srcPit.Path;
			var srcCat = GetCategory(srcFile);

			var dstDir = Path.Combine(_pitLibraryPath, ConfigsDir, srcCat);
			if (!Directory.Exists(dstDir))
				Directory.CreateDirectory(dstDir);

			var dstFile = Path.Combine(dstDir, name + ".peach");
			if (File.Exists(dstFile))
				throw new ArgumentException("A pit already exists with the specified name.");

			var guid = MakeGuid(GetRelativePath(dstFile));
			var pit = new Pit {
				OriginalPit = srcPit.Pit.OriginalPit,
				Id = guid,
				PitUrl = PitServicePrefix + "/" + guid,
				Name = name,
				Description = description,
				Locked = false,
				Tags = srcPit.Pit.Tags,
				Peaches = new List<PeachVersion> { Version },
				Config = new List<Param>(),
				Agents = new List<Models.Agent>(),
				Weights = new List<PitWeight>(),
			};

			SavePit(dstFile, pit);

			var detail = AddEntry(_configsLib, dstFile);

			PopulatePit(detail);

			return detail;
		}

		private string MakeUniquePath(string dir, string name, string ext)
		{
			var unique = "";
			var counter = 1;
			var path = Path.Combine(dir, name + unique + ext);
			while (File.Exists(path))
			{
				unique = "-Legacy-{0}".Fmt(counter++);
				path = Path.Combine(dir, name + unique + ext);
			}
			return path;
		}

		public PitDetail MigratePit(string legacyPitUrl, string pitUrl)
		{
			var legacyPit = GetPitDetailByUrl(legacyPitUrl);
			if (legacyPit == null)
				throw new KeyNotFoundException("The legacy pit could not be found.");

			var legacyFile = legacyPit.Path;
			var legacyConfigFile = legacyFile + ".config";
			var legacyName = Path.GetFileNameWithoutExtension(legacyFile);
			var legacyCat = GetCategory(legacyFile);

			var cfgDir = Path.Combine(_pitLibraryPath, ConfigsDir, legacyCat);
			if (!Directory.Exists(cfgDir))
				Directory.CreateDirectory(cfgDir);

			var xmlDir = (legacyPitUrl == pitUrl) ? Path.Combine(_pitLibraryPath, legacyCat) : cfgDir;
			if (!Directory.Exists(xmlDir))
				Directory.CreateDirectory(xmlDir);

			var cfgFile = MakeUniquePath(cfgDir, legacyName, ".peach");
			var xmlFile = MakeUniquePath(xmlDir, legacyName, ".xml");
			var xmlConfigFile = MakeUniquePath(xmlDir, legacyName, ".xml.config");

			var originalPit = GetPitDetailByUrl(pitUrl);
			if (originalPit == null)
				throw new KeyNotFoundException("The original pit could not be found.");

			var originalPitPath = (legacyPitUrl == pitUrl) ? GetRelativePath(xmlFile) : originalPit.Pit.OriginalPit;

			// 1. Parse legacyPit.xml.config
			var defs = PitDefines.ParseFile(legacyConfigFile, _pitLibraryPath, false);

			// 2. Extract Configs
			var cfg = defs.Flatten()
				.Where(def => def.Key != "PitLibraryPath")
				.Select(def => new Param { Key = def.Key, Value = def.Value })
				.ToList();

			// 3. Parse legacyPit.xml
			var contents = Parse(legacyFile);

			// 4. Extract Agents
			var agents = contents.Children.OfType<PeachElement.AgentElement>();

			// 5. Write new .peach
			var guid = MakeGuid(GetRelativePath(cfgFile));
			var pit = new Pit {
				OriginalPit = originalPitPath,
				Id = guid,
				PitUrl = PitServicePrefix + "/" + guid,
				Name = legacyName,
				Description = contents.Description,
				Locked = false,
				Tags = legacyPit.Pit.Tags,
				Peaches = new List<PeachVersion> { Version },
				Config = cfg,
				Agents = agents.ToWeb(),
				Weights = new List<PitWeight>(),
			};
			SavePit(cfgFile, pit);

			// 6. Move legacyPit.xml to target dir
			// TODO: strip <Agent></Agent> and <Test><Agent ref='XXX'/></Test>
			File.Move(legacyFile, xmlFile);

			// 7. Move legacyPit.xml.config to target dir
			if (File.Exists(legacyConfigFile))
				File.Move(legacyConfigFile, xmlConfigFile);

			var detail = AddEntry(_configsLib, cfgFile);

			PopulatePit(detail);

			return detail;
		}

		public Pit UpdatePitById(string guid, PitConfig data)
		{
			var detail = GetPitDetailById(guid);
			if (detail == null)
				throw new KeyNotFoundException();

			if (detail.Pit.Locked)
				throw new UnauthorizedAccessException();

			detail.Pit.Config = data.Config; // TODO: defines.ApplyWeb(config);
			detail.Pit.Agents = data.Agents.FromWeb();
			detail.Pit.Weights = data.Weights;

			SavePit(detail.Path, detail.Pit);

			return PopulatePit(detail);
		}

		public Pit UpdatePitByUrl(string url, PitConfig data)
		{
			PitDetail pit;
			_entries.TryGetValue(url, out pit);
			return UpdatePitById(pit.Pit.Id, data);
		}

		private PitDetail GetPitDetailById(string guid)
		{
			return GetPitDetailByUrl(PitServicePrefix + "/" + guid);	
		}

		public PitDetail GetPitDetailByUrl(string url)
		{
			PitDetail pit;
			_entries.TryGetValue(url, out pit);
			return pit;
		}

		public static Pit LoadPit(string path)
		{
			var serializer = new JsonSerializer();
			using (var stream = new StreamReader(path))
			using (var reader = new JsonTextReader(stream))
				return serializer.Deserialize<Pit>(reader);
		}

		public static void SavePit(string path, Pit pit)
		{
			var serializer = new JsonSerializer {
				Formatting = Newtonsoft.Json.Formatting.Indented
			};
			using (var stream = new StreamWriter(path))
			using (var writer = new JsonTextWriter(stream))
				serializer.Serialize(writer, pit);
		}

		#region Pit Config/Agents/Metadata

		private Pit PopulatePit(PitDetail detail)
		{
			var pitXml = Path.Combine(_pitLibraryPath, detail.Pit.OriginalPit);
			var pitConfig = pitXml + ".config";
			var defs = PitDefines.ParseFile(pitConfig, _pitLibraryPath);

			var pit = detail.Pit;
			var metadata = PitCompiler.LoadMetadata(pitXml);

			var calls = new List<string>(); // TODO: get actual calls
			pit.Metadata = new PitMetadata
			{
				Defines = defs.ToWeb(),
				Monitors = MonitorMetadata.Generate(calls),
				Fields = metadata != null ? metadata.Fields : null,
			};

			return pit;
		}

		#endregion
	}
}
