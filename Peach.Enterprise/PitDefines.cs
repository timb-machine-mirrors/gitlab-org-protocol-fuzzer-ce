using Peach.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Peach.Enterprise
{
	[XmlRoot("PitDefines", IsNullable = false, Namespace = "http://peachfuzzer.com/2012/PitDefines")]
	public class PitDefines
	{
		#region Defines

		public abstract class Define
		{
			[XmlAttribute("key")]
			public string Key { get; set; }

			[XmlAttribute("value")]
			public string Value { get; set; }

			[XmlAttribute("name")]
			public string Name { get; set; }

			[XmlAttribute("description")]
			[DefaultValue("")]
			public string Description { get; set; }

			public abstract WebServices.Models.ConfigType ConfigType
			{
				get;
			}

			public virtual string[] Defaults
			{
				get { return new string[0]; }
			}

			public virtual long? Min
			{
				get { return null; }
			}

			public virtual ulong? Max
			{
				get { return null; }
			}
		}

		/// <summary>
		/// Free form string
		/// </summary>
		public class StringDefine : Define
		{
			public override WebServices.Models.ConfigType ConfigType
			{
				get { return WebServices.Models.ConfigType.String; }
			}
		}

		public class HexDefine : Define
		{
			public override WebServices.Models.ConfigType ConfigType
			{
				get { return WebServices.Models.ConfigType.Hex; }
			}
		}

		public class RangeDefine : Define
		{
			[XmlAttribute("min")]
			public long MinValue { get; set; }

			[XmlAttribute("max")]
			public ulong MaxValue { get; set; }

			public override WebServices.Models.ConfigType ConfigType
			{
				get { return WebServices.Models.ConfigType.Range; }
			}

			public override long? Min
			{
				get { return MinValue; }
			}

			public override ulong? Max
			{
				get { return MaxValue; }
			}
		}

		public class Ipv4Define : Define
		{
			public override WebServices.Models.ConfigType ConfigType
			{
				get { return WebServices.Models.ConfigType.Ipv4; }
			}
		}

		public class Ipv6Define : Define
		{
			public override WebServices.Models.ConfigType ConfigType
			{
				get { return WebServices.Models.ConfigType.Ipv6; }
			}
		}

		public class HwaddrDefine : Define
		{
			public override WebServices.Models.ConfigType ConfigType
			{
				get { return WebServices.Models.ConfigType.Hwaddr; }
			}
		}

		public class IfaceDefine : Define
		{
			public override WebServices.Models.ConfigType ConfigType
			{
				get { return WebServices.Models.ConfigType.Iface; }
			}
		}

		public class StrategyDefine : Define
		{
			public override WebServices.Models.ConfigType ConfigType
			{
				get { return WebServices.Models.ConfigType.Enum; }
			}

			public override string[] Defaults
			{
				get
				{
					return ClassLoader
						.GetAllByAttribute<MutationStrategyAttribute>(null)
						.Select(kv => kv.Key)
						.Where(k => k.IsDefault)
						.Select(k => k.Name)
						.ToArray();
				}
			}
		}

		public class EnumDefine : Define
		{
			[XmlIgnore]
			public Type EnumType { get; private set; }

			[XmlAttribute("enumType")]
			public string EnumTypeName
			{
				get
				{
					return EnumType != null ? EnumType.FullName : null;
				}
				set
				{
					if (value == null)
					{
						EnumType = null;
						return;
					}

					foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
					{
						EnumType = asm.GetType(value);
						if (EnumType != null)
							return;
					}

					throw new ArgumentException();
				}
			}

			public override WebServices.Models.ConfigType ConfigType
			{
				get { return WebServices.Models.ConfigType.Enum; }
			}

			public override string[] Defaults
			{
				get
				{
					return Enum.GetNames(EnumType);
				}
			}
		}

		#endregion

		#region Platforms

		public abstract class Collection
		{
			public Collection()
			{
				Defines = new List<Define>();
			}

			public abstract Peach.Core.Platform.OS Platform { get; }

			[XmlElement("String", Type = typeof(StringDefine))]
			[XmlElement("Hex", Type = typeof(HexDefine))]
			[XmlElement("Range", Type = typeof(RangeDefine))]
			[XmlElement("Ipv4", Type = typeof(Ipv4Define))]
			[XmlElement("Ipv6", Type = typeof(Ipv6Define))]
			[XmlElement("Hwaddr", Type = typeof(HwaddrDefine))]
			[XmlElement("Iface", Type = typeof(IfaceDefine))]
			[XmlElement("Strategy", Type = typeof(StrategyDefine))]
			[XmlElement("Enum", Type = typeof(EnumDefine))]
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

		#endregion

		#region Public Members

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

		#endregion

		#region Parse

		public static List<Define> Parse(string inputUri)
		{
			return Parse(XmlTools.Deserialize<PitDefines>(inputUri));
		}

		public static List<Define> Parse(System.IO.Stream stream)
		{
			return Parse(XmlTools.Deserialize<PitDefines>(stream));
		}

		public static List<Define> Parse(System.IO.TextReader textReader)
		{
			return Parse(XmlTools.Deserialize<PitDefines>(textReader));
		}

		private static List<Define> Parse(PitDefines defs)
		{
			var os = Platform.GetOS();

			return defs.Platforms
				.Where(a => a.Platform.HasFlag(os))
				.SelectMany(a => a.Defines)
				.ToList();
		}

		#endregion
	}
}
