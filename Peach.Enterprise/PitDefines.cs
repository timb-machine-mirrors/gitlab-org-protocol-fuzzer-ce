using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Peach.Enterprise
{
	[XmlRoot("PitDefines")]
	public class PitDefines
	{
		public enum Type
		{
			[XmlEnum("string")]
			String,
			[XmlEnum("port")]
			Port,
			[XmlEnum("ipv4")]
			Ipv4,
			[XmlEnum("ipv6")]
			Ipv6,
			[XmlEnum("hwaddr")]
			Hwaddr,
			[XmlEnum("iface")]
			Iface,
			[XmlEnum("strategy")]
			Strategy,
			//[XmlEnum("enum")]
			//Enum
		}

		public class Define
		{
			[XmlAttribute("key")]
			public string Key { get; set; }

			[XmlAttribute("value")]
			public string Value { get; set; }

			[XmlAttribute("name")]
			[DefaultValue("")]
			public string Name { get; set; }

			[XmlAttribute("description")]
			[DefaultValue("")]
			public string Description { get; set; }

			[XmlAttribute("type")]
			[DefaultValue(Type.String)]
			public Type Type { get; set; }
		}

		public abstract class Collection
		{
			public Collection()
			{
				Defines = new List<Define>();
			}

			public abstract Peach.Core.Platform.OS Platform { get; }

			[XmlElement("Define")]
			public List<Define> Defines { get; set; }
		}

		public class None : Collection
		{
			public override Peach.Core.Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.None; }
			}
		}

		public class Windows : Collection
		{
			public override Peach.Core.Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.Windows; }
			}
		}

		public class OSX : Collection
		{
			public override Peach.Core.Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.OSX; }
			}
		}

		public class Linux : Collection
		{
			public override Peach.Core.Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.Linux; }
			}
		}

		public class Unix : Collection
		{
			public override Peach.Core.Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.Unix; }
			}
		}

		public class All : Collection
		{
			public override Peach.Core.Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.All; }
			}
		}

		public PitDefines()
		{
			Platforms = new List<Collection>();
		}

		[XmlElement("None", Type = typeof(None))]
		[XmlElement("OSX", Type = typeof(OSX))]
		[XmlElement("Windows", Type = typeof(Windows))]
		[XmlElement("Linux", Type = typeof(Linux))]
		[XmlElement("Unix", Type = typeof(Unix))]
		[XmlElement("All", Type = typeof(All))]
		public List<Collection> Platforms { get; set; }

		public static PitDefines Deserialize(string inputUri)
		{
			return Deserialize(XmlReader.Create(inputUri));
		}

		public static PitDefines Deserialize(System.IO.Stream stream)
		{
			return Deserialize(XmlReader.Create(stream));
		}

		public static PitDefines Deserialize(System.IO.TextReader textReader)
		{
			return Deserialize(XmlReader.Create(textReader));
		}

		public static PitDefines Deserialize(XmlReader xmlReader)
		{
			var s = new XmlSerializer(typeof(PitDefines));
			var o = s.Deserialize(xmlReader);
			var r = (PitDefines)o;

			return r;
		}

		public static List<Define> Parse(string inputUri)
		{
			return Parse(XmlReader.Create(inputUri));
		}

		public static List<Define> Parse(System.IO.Stream stream)
		{
			return Parse(XmlReader.Create(stream));
		}

		public static List<Define> Parse(System.IO.TextReader textReader)
		{
			return Parse(XmlReader.Create(textReader));
		}

		public static List<Define> Parse(XmlReader xmlReader)
		{
			var defs = Deserialize(xmlReader);

			return defs.Platforms
				.Where(a => a.Platform.HasFlag(Peach.Core.Platform.GetOS()))
				.SelectMany(a => a.Defines)
				.ToList();
		}
	}
}
