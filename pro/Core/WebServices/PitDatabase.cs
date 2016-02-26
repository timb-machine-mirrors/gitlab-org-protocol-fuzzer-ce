using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Peach.Core;
using Peach.Pro.Core.WebServices.Models;
using Encoding = System.Text.Encoding;
using File = System.IO.File;
using Newtonsoft.Json;
using System.Reflection;
using System.Globalization;

namespace Peach.Pro.Core.WebServices
{
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
			entries = new NamedCollection<PitDetail>();
			libraries = new NamedCollection<LibraryDetail>();
		}

		public PitDatabase(string libraryPath)
		{
			Load(libraryPath);
		}

		private string pitLibraryPath;
		private LibraryDetail userLibrary;

		private NamedCollection<PitDetail> entries;
		private NamedCollection<LibraryDetail> libraries;

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

			entries = new NamedCollection<PitDetail>();
			libraries = new NamedCollection<LibraryDetail>();

			AddLibrary("", "Pits", true);
			userLibrary = AddLibrary("User", "Configurations", false);
		}

		private LibraryDetail AddLibrary(string subdir, string name, bool locked)
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

			if (Directory.Exists(path))
			{
				lib.Library.Timestamp = Directory.GetCreationTime(path);

				foreach (var dir in Directory.EnumerateDirectories(path))
				{
					if (Path.GetDirectoryName(dir) == "User")
						continue;

					string searchPattern = locked ? "*.xml" : "*.pit";
					foreach (var file in Directory.EnumerateFiles(dir, searchPattern))
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

			return lib;
		}

		private Pit AddEntry(LibraryDetail lib, string fileName)
		{
			PitDetail detail;
			if (lib.Library.Locked)
				detail = MakePitDetail(fileName);
			else
				detail = LoadPitDetail(fileName);
			entries.Add(detail);

			lib.Library.Versions[0].Pits.Add(new LibraryPit {
				Id = detail.Pit.Id,
				PitUrl = detail.Pit.PitUrl,
				Name = detail.Pit.Name,
				Description = detail.Pit.Description,
				Tags = detail.Pit.Tags,
			});

			return detail.Pit;
		}

		private string GetCategory(string path)
		{
			return (Path.GetDirectoryName(path) ?? "").Split(Path.DirectorySeparatorChar).Last();
		}

		private string GetOriginalPit(string path)
		{
			var len = pitLibraryPath.Length;
			if (!pitLibraryPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
				len++;
			return path.Substring(len);
		}

		private PitDetail MakePitDetail(string fileName)
		{
			var guid = MakeGuid(fileName);
			var lastModified = File.GetLastWriteTimeUtc(fileName);
			var dir = GetCategory(fileName);
			var tag = new Tag {
				Name = "Category." + dir,
				Values = new List<string> { "Category", dir },
			};

			var pit = new Pit {
				OriginalPit = GetOriginalPit(fileName),
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

			Pit pit = LoadPit(fileName);

			pit.User = Environment.UserName;
			pit.Timestamp = lastModified;

			return new PitDetail {
				Path = fileName,
				Pit = pit,
			};
		}

		public IEnumerable<PitDetail> PitDetails
		{
			get { return entries; }
		}

		public IEnumerable<LibraryPit> Entries
		{
			get { return entries.Select(item => item.Pit); }
		}

		public IEnumerable<Library> Libraries
		{
			get { return libraries.Select(item => item.Library); }
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
			libraries.TryGetValue(url, out detail);
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
		/// <returns>The url of the newly copied pit.</returns>
		public string CopyPit(string pitUrl, string name, string description)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("A non-empty pit name is required.", "name");

			if (Path.GetFileName(name) != name)
				throw new ArgumentException("A valid pit name is required.", "name");

			var srcPit = GetPitDetailByUrl(pitUrl);
			if (srcPit == null)
				throw new KeyNotFoundException("The source pit could not be found.");

			var srcFile = srcPit.Path;
			var srcCat = GetCategory(srcFile);

			var dstDir = Path.Combine(pitLibraryPath, userLibrary.SubDir, srcCat);
			if (!Directory.Exists(dstDir))
				Directory.CreateDirectory(dstDir);

			var dstFile = Path.Combine(dstDir, name + ".pit");
			if (File.Exists(dstFile))
				throw new ArgumentException("A pit already exists with the specified name.");

			var guid = MakeGuid(dstFile);
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

			var item = AddEntry(userLibrary, dstFile);

			return item.PitUrl;
		}

		public Pit UpdatePitById(string guid, PitConfig data)
		{
			var detail = GetPitDetailById(guid);
			if (detail == null)
				throw new KeyNotFoundException();

			if (detail.Pit.Locked)
				throw new UnauthorizedAccessException();

			detail.Pit.Config = data.Config; // TODO: defines.ApplyWeb(config);
			detail.Pit.Agents = data.Agents;
			detail.Pit.Weights = data.Weights;

			SavePit(detail.Path, detail.Pit);

			return PopulatePit(detail);
		}

		public Pit UpdatePitByUrl(string url, PitConfig data)
		{
			PitDetail pit;
			entries.TryGetValue(url, out pit);
			return UpdatePitById(pit.Pit.Id, data);
		}

		private PitDetail GetPitDetailById(string guid)
		{
			return GetPitDetailByUrl(PitServicePrefix + "/" + guid);	
		}

		public PitDetail GetPitDetailByUrl(string url)
		{
			PitDetail pit;
			entries.TryGetValue(url, out pit);
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
				Formatting = Formatting.Indented
			};
			using (var stream = new StreamWriter(path))
			using (var writer = new JsonTextWriter(stream))
				serializer.Serialize(writer, pit);
		}

		#region Pit Config/Agents/Metadata

		private Pit PopulatePit(PitDetail detail)
		{
			var pitXml = Path.Combine(pitLibraryPath, detail.Pit.OriginalPit);
			var pitConfig = pitXml + ".config";
			var defs = PitDefines.ParseFile(pitConfig, pitLibraryPath);

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
