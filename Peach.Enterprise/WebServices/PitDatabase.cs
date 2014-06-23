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
using System.Xml.XPath;

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
			roots = new Dictionary<string, LibraryRoot>();
			entries = new Dictionary<string, Models.Pit>();
			libraries = new Dictionary<string, Models.Library>();
			interfaces = null;

			AddLibrary(path, "", "Peach Pro Library 2014 Q2", true);
			AddLibrary(path, "User", "User Library", false);
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
				throw new ArgumentException("A valid pit name is required.", "name");

			var dstLib = GetLibraryByUrl(libraryUrl);
			if (dstLib == null)
				throw new KeyNotFoundException("The destination pit library could not be found.");
			if (dstLib.Locked)
				throw new UnauthorizedAccessException("The destination pit library is locked.");

			// Only support a single user library for now
			System.Diagnostics.Debug.Assert(dstLib.Name == "User Library");

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

			var nsMgr = new XmlNamespaceManager(nav.NameTable);
			nsMgr.AddNamespace("p", PeachElement.Namespace);

			var it = nav.SelectSingleNode("/p:Peach", nsMgr);

			SetPitAttr(it, "author", Environment.UserName);
			SetPitAttr(it, "description", description);

			try
			{
				var settings = new XmlWriterSettings()
				{
					Indent = true,
					Encoding = System.Text.Encoding.UTF8,
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
				catch
				{
				}

				try
				{
					if (File.Exists(srcFile + ".config"))
						File.Delete(dstFile + ".config");
				}
				catch
				{
				}

				throw;
			}

			var item = AddEntry(dstLib.Versions[0], dstRoot.PitLibraryPath, dstFile);

			return item.PitUrl;
		}

		public static void SaveConfig(Models.Pit pit, List<Models.ConfigItem> config)
		{
			var fileName = pit.Versions[0].Files[0].Name + ".config";
			var defines = PitDefines.Parse(fileName);

			// For now, read in the current defines off disk and
			// apply any applicable changes. We don't currently expect
			// this api to add new defines or delete old defines.
			foreach (var item in config)
			{
				foreach (var def in defines.Where(d => d.Key == item.Key))
					def.Value = item.Value;
			}

			var final = new PitDefines()
			{
				Platforms = new List<PitDefines.Collection>(new[] {
					new PitDefines.All()
					{
						Defines = defines,
					}
				}),
			};

			XmlTools.Serialize(fileName, final);
		}

		public static void SaveMonitors(Models.Pit pit, List<Models.Agent> monitors)
		{
			var fileName = pit.Versions[0].Files[0].Name;
			var doc = new XmlDocument();

			using (var rdr = XmlReader.Create(fileName))
			{
				doc.Load(rdr);
			}

			var nav = doc.CreateNavigator();

			var nsMgr = new XmlNamespaceManager(nav.NameTable);
			nsMgr.AddNamespace("p", PeachElement.Namespace);

			var oldAgents = nav.Select("//p:Agent", nsMgr).OfType<XPathNavigator>().ToList();

			foreach (var a in oldAgents)
				a.DeleteSelf();

			var test = nav.SelectSingleNode("/p:Peach/p:Test", nsMgr);

			var agents = new OrderedDictionary<string, XmlWriter>();

			foreach (var item in monitors)
			{
				XmlWriter w;
				if (!agents.TryGetValue(item.AgentUrl, out w))
				{
					var agentName = "Agent" + agents.Count.ToString();

					w = test.InsertBefore();
					w.WriteStartElement("Agent");
					w.WriteAttributeString("name", agentName);
					w.WriteAttributeString("location", item.AgentUrl);

					// AppendChild so the agents stay in order
					using (var testWriter = test.AppendChild())
					{
						testWriter.WriteStartElement("Agent");
						testWriter.WriteAttributeString("ref", agentName);
						testWriter.WriteEndElement();
					}

					agents.Add(item.AgentUrl, w);
				}

				foreach (var m in item.Monitors)
				{
					w.WriteStartElement("Monitor");
					w.WriteAttributeString("class", m.MonitorClass);

					foreach (var p in m.Map)
					{
						w.WriteStartElement("Param");
						w.WriteAttributeString("name", p.Key);
						w.WriteAttributeString("value", p.Value);
						w.WriteEndElement();
					}

					w.WriteEndElement();
				}
			}

			foreach (var a in agents)
			{
				a.Value.Close();
			}

			var settings = new XmlWriterSettings()
			{
				Indent = true,
				Encoding = System.Text.Encoding.UTF8,
				IndentChars = "  ",
			};

			using (var writer = XmlWriter.Create(fileName, settings))
			{
				doc.WriteTo(writer);
			}
		}

		private void SetPitAttr(XPathNavigator nav, string attribute, string value)
		{
			if (nav.MoveToAttribute(attribute, ""))
			{
				nav.SetValue(value);
				nav.MoveToParent();
			}
			else
			{
				nav.CreateAttribute(null, attribute, null, value);
			}
		}

		private void AddLibrary(string root, string subdir, string name, bool locked)
		{
			var path = Path.Combine(root, subdir);

			var guid = MakeGuid(name);

			var lib = new Models.Library()
			{
				LibraryUrl = LibraryService.Prefix + "/" + guid,
				Name = name,
				Description = name,
				Locked = locked,
				Versions = new List<Models.LibraryVersion>(),
				Groups = new List<Models.Group>(),
				User = Environment.UserName,
				Timestamp = Directory.GetCreationTime(path),
			};

			var ver = new Models.LibraryVersion()
			{
				Version = 1,
				Locked = lib.Locked,
				Pits = new List<Models.LibraryPit>(),
			};

			var group = new Models.Group()
			{
				GroupUrl = "",
				Access = Models.GroupAccess.Read,
			};

			if (!ver.Locked)
				group.Access |= Models.GroupAccess.Write;

			lib.Versions.Add(ver);
			lib.Groups.Add(group);
			libraries.Add(lib.LibraryUrl, lib);
			roots.Add(lib.LibraryUrl, new LibraryRoot() { PitLibraryPath = root, SubDir = subdir });

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

			var ret = new Models.PitConfig()
			{
				PitUrl = pit.PitUrl,
			};

			if (!File.Exists(fileName))
			{
				ret.Config = new List<Models.ConfigItem>();
			}
			else
			{
				ret.Config = MakeConfig(PitDefines.Parse(fileName));
			}

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

		class LibraryRoot
		{
			public string PitLibraryPath { get; set; }
			public string SubDir { get; set; }
		}

		private Dictionary<string, LibraryRoot> roots;
		private Dictionary<string, Models.Pit> entries;
		private Dictionary<string, Models.Library> libraries;
		private List<NetworkInterface> interfaces = null;
	}
}
