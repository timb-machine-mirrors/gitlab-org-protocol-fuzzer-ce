using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using Peach.Core;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Peach.Enterprise.WebServices
{
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
			[XmlAttribute]
			public string name { get; set; }
		}

		public class IncludeElement : ChildElement
		{
			[XmlAttribute]
			public string ns { get; set; }

			[XmlAttribute]
			public string src { get; set; }
		}

		[XmlAttribute]
		[DefaultValue("")]
		public string author { get; set; }

		[XmlAttribute]
		[DefaultValue("")]
		public string description { get; set; }

		[XmlAttribute]
		[DefaultValue("")]
		public string version { get; set; }

		[XmlElement("Include", typeof(IncludeElement))]
		[XmlElement("Test", typeof(TestElement))]
		public List<ChildElement> Children { get; set; }
	}

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
		public LoadEventArgs(Models.Pit pit, string fileName)
		{
			Pit = pit;
			FileName = fileName;
		}

		public Models.Pit Pit { get; private set; }

		public string FileName { get; private set; }
	}

	public class PitDatabase
	{
		private static PeachElement Parse(string fileName)
		{
			var s = new XmlSerializer(typeof(PeachElement));
			using (var rdr = XmlReader.Create(fileName))
			{
				var elem = (PeachElement)s.Deserialize(rdr);
				return elem;
			}
		}

		private static MD5 md5 = new MD5CryptoServiceProvider();

		private static string MakeGuid(string value)
		{
			var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));
			var sb = new StringBuilder();
			for (int i = 0; i < bytes.Length; ++i)
				sb.Append(bytes[i].ToString("x2"));
			return sb.ToString();
		}

		private static Models.PeachVersion peachVer = MakePeachVer();

		private static Models.PeachVersion MakePeachVer()
		{
			var ver = Assembly.GetExecutingAssembly().GetName().Version;

			return new Models.PeachVersion()
			{
				PeachUrl = "",
				Major = ver.Major.ToString(),
				Minor = ver.Minor.ToString(),
				Build = ver.Build.ToString(),
				Revision = ver.Revision.ToString(),
			};
		}

		public PitDatabase()
		{
			entries = new Dictionary<string, Models.Pit>();
			libraries = new Dictionary<string, Models.Library>();
			interfaces = null;
		}

		public PitDatabase(string libraryPath)
		{
			Load(libraryPath);
		}

		public event EventHandler<ValidationEventArgs> ValidationEventHandler;
		public event EventHandler<LoadEventArgs> LoadEventHandler;

		public void Load(string path)
		{
			entries = new Dictionary<string, Models.Pit>();
			libraries = new Dictionary<string, Models.Library>();
			interfaces = null;

			var name = "Peach Pro Library 2014 Q2";
			var guid = MakeGuid(name);

			var lib = new Models.Library()
			{
				LibraryUrl = LibraryService.Prefix + "/" + guid,
				Name = name,
				Description = name,
				Versions = new List<Models.LibraryVersion>(),
				Groups = new List<Models.Group>(),
				User = Environment.UserName,
				Timestamp = Directory.GetCreationTime(path),
			};

			var ver = new Models.LibraryVersion()
			{
				Version = 1,
				Locked = true,
				Pits = new List<Models.LibraryPit>(),
			};

			var group = new Models.Group()
			{
				GroupUrl = "",
				Access = Models.GroupAccess.Read,
			};

			lib.Versions.Add(ver);
			lib.Groups.Add(group);
			libraries.Add(lib.LibraryUrl, lib);

			foreach (var dir in Directory.EnumerateDirectories(path))
			{
				foreach (var file in Directory.EnumerateFiles(dir, "*.xml"))
				{
					try
					{
						var item = AddEntry(ver, path, file);

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

		public IEnumerable<Models.Pit> Entries
		{
			get
			{
				return entries.Values;
			}
		}

		public IEnumerable<Models.Library> Libraries
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

		public Models.Pit GetPitById(string guid)
		{
			return GetPitByUrl(PitService.Prefix + "/" + guid);
		}

		public Models.Pit GetPitByUrl(string url)
		{
			Models.Pit pit;
			entries.TryGetValue(url, out pit);
			return pit;
		}

		public Models.Library GetLibraryById(string guid)
		{
			return GetLibraryByUrl(LibraryService.Prefix + "/" + guid);
		}

		public Models.Library GetLibraryByUrl(string url)
		{
			Models.Library library;
			libraries.TryGetValue(url, out library);
			return library;
		}

		public Models.PitConfig GetConfigById(string guid)
		{
			return GetConfigByUrl(PitService.Prefix + "/" + guid);
		}

		public Models.PitConfig GetConfigByUrl(string url)
		{
			var pit = GetPitByUrl(url);
			if (pit == null)
				return null;

			var fileName = pit.Versions[0].Files[0].Name + ".config";

			var defines = PitDefines.Parse(fileName);

			var ret = new Models.PitConfig()
			{
				PitUrl = pit.PitUrl,
				Config = MakeConfig(defines),
			};

			return ret;
		}

		public List<Models.ConfigItem> MakeConfig(List<PitDefines.Define> defines)
		{
			var ret = new List<Models.ConfigItem>();

			foreach (var d in defines)
			{
				var item = new Models.ConfigItem()
				{
					Type = d.ConfigType,
					Key = d.Key,
					Value = d.Value,
					Name = d.Name,
					Description = d.Description,
					Defaults = new List<string>(d.Defaults),
					Min = d.Min,
					Max = d.Max,
				};

				switch (item.Type)
				{
					case Models.ConfigType.Hwaddr:
						item.Defaults.AddRange(
							Interfaces
								.Select(i => i.GetPhysicalAddress().GetAddressBytes())
								.Select(a => string.Join(":", a.Select(b => b.ToString("x2"))))
								.Where(s => !string.IsNullOrEmpty(s)));
						break;
					case Models.ConfigType.Iface:
						item.Defaults.AddRange(Interfaces.Select(i => i.Name));
						break;
					case Models.ConfigType.Ipv4:
						item.Defaults.AddRange(
							Interfaces
								.SelectMany(i => i.GetIPProperties().UnicastAddresses)
								.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
								.Select(a => a.Address.ToString()));
						break;
					case Models.ConfigType.Ipv6:
						item.Defaults.AddRange(
							Interfaces
								.SelectMany(i => i.GetIPProperties().UnicastAddresses)
								.Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6)
								.Select(a => a.Address.ToString()));
						break;
				}

				ret.Add(item);
			}

			return ret;
		}

		private Models.Pit AddEntry(Models.LibraryVersion lib, string pitLibraryPath, string fileName)
		{
			var contents = Parse(fileName);
			var guid = MakeGuid(fileName);
			var value = new Models.Pit()
			{
				PitUrl = PitService.Prefix + "/" + guid,
				Name = Path.GetFileNameWithoutExtension(fileName),
				Description = contents.description,
				Locked = lib.Locked,
				Tags = new List<Models.Tag>(),
				Versions = new List<Models.PitVersion>(),
				Peaches = new List<Models.PeachVersion>(),
				User = contents.author,
				Timestamp = File.GetLastWriteTime(fileName),
			};

			var ver = new Models.PitVersion()
			{
				Version = 1,
				Locked = value.Locked,
				Files = new List<Models.PitFile>(),
				User = Environment.UserName,
				Timestamp = value.Timestamp
			};

			AddAllFiles(ver.Files, pitLibraryPath, fileName, contents);

			value.Versions.Add(ver);

			var dir = Path.GetDirectoryName(fileName).Split(Path.DirectorySeparatorChar).Last();
			var tag = new Models.Tag()
			{
				Name = "Category." + dir,
				Values = new List<string>(new[] { "Category", dir }),
			};

			value.Tags.Add(tag);
			value.Peaches.Add(peachVer);

			entries.Add(value.PitUrl, value);

			lib.Pits.Add(new Models.LibraryPit()
			{
				PitUrl = value.PitUrl,
				Name = value.Name,
				Description = value.Description,
				Tags = value.Tags,
			});

			return value;
		}

		private void AddAllFiles(List<Models.PitFile> list, string pitLibraryPath, string fileName, PeachElement contents)
		{
			list.Add(new Models.PitFile()
			{
				Name = fileName,
				FileUrl = "",
			});

			foreach (var child in contents.Children)
			{
				var inc = child as PeachElement.IncludeElement;
				if (inc != null)
				{
					var otherName = inc.src;

					otherName = otherName.Replace("file:", "");
					otherName = otherName.Replace("##PitLibraryPath##", pitLibraryPath);

					// Normalize the path
					otherName = Path.Combine(Path.GetDirectoryName(otherName), Path.GetFileName(otherName));

					var other = Parse(otherName);

					AddAllFiles(list, pitLibraryPath, otherName, other);
				}
			}
		}

		private Dictionary<string, Models.Pit> entries;
		private Dictionary<string, Models.Library> libraries;
		private List<NetworkInterface> interfaces = null;
	}
}
