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
	[XmlRoot("Peach", Namespace = "http://peachfuzzer.com/2012/Peach")]
	[Serializable]
	public class PeachElement
	{
		[XmlAttribute]
		[DefaultValue("")]
		public string author { get; set; }

		[XmlAttribute]
		[DefaultValue("")]
		public string description { get; set; }

		[XmlAttribute]
		[DefaultValue("")]
		public string version { get; set; }
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
		}

		public event EventHandler<ValidationEventArgs> ValidationEventHandler;

		public void Load(string path)
		{
			var name = "Peach Pro Library 2014 Q2";
			var guid = MakeGuid(name);

			var lib = new Models.Library()
			{
				LibraryUrl = "/p/libraries/" + guid,
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
			libraries.Add(guid, lib);

			foreach (var dir in Directory.EnumerateDirectories(path))
			{
				foreach (var file in Directory.EnumerateFiles(dir, "*.xml"))
				{
					try
					{
						AddEntry(ver, file);
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

		public Models.Pit GetPit(string guid)
		{
			Models.Pit pit;
			entries.TryGetValue(guid, out pit);
			return pit;
		}

		public Models.Library GetLibrary(string guid)
		{
			Models.Library library;
			libraries.TryGetValue(guid, out library);
			return library;
		}

		public Models.PitConfig GetConfig(string guid)
		{
			var pit = GetPit(guid);
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

		private void AddEntry(Models.LibraryVersion lib, string fileName)
		{
			var contents = Parse(fileName);
			var guid = MakeGuid(fileName);
			var value = new Models.Pit()
			{
				PitUrl = "/p/pits/" + guid,
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

			var file = new Models.PitFile()
			{
				Name = fileName,
				FileUrl = "",
			};

			ver.Files.Add(file);

			value.Versions.Add(ver);

			var dir = Path.GetDirectoryName(fileName).Split(Path.DirectorySeparatorChar).Last();
			var tag = new Models.Tag()
			{
				Name = "Category." + dir,
				Values = new List<string>(new[] { "Category", dir }),
			};

			value.Tags.Add(tag);
			value.Peaches.Add(peachVer);

			entries.Add(guid, value);

			lib.Pits.Add(new Models.LibraryPit()
			{
				PitUrl = value.PitUrl,
				Name = value.Name,
				Description = value.Description,
				Tags = value.Tags,
			});
		}

		private Dictionary<string, Models.Pit> entries = new Dictionary<string, Models.Pit>();
		private Dictionary<string, Models.Library> libraries = new Dictionary<string, Models.Library>();

		private List<NetworkInterface> interfaces = null;
	}
}
