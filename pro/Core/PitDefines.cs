using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Peach.Core;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core
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

			public abstract ParameterType ConfigType { get; }

			public virtual string[] Defaults
			{
				get { return new string[0]; }
			}

			// only used by RangeDefine
			public virtual long? Min
			{
				get { return null; }
			}

			// only used by RangeDefine
			public virtual ulong? Max
			{
				get { return null; }
			}
		}

		public class UserDefine : Define
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.User; }
			}
		}

		/// <summary>
		/// Free form string
		/// </summary>
		public class StringDefine : Define
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.String; }
			}
		}

		public class HexDefine : Define
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.Hex; }
			}
		}

		public class RangeDefine : Define
		{
			[XmlAttribute("min")]
			public long MinValue { get; set; }

			[XmlAttribute("max")]
			public ulong MaxValue { get; set; }

			public override ParameterType ConfigType
			{
				get { return ParameterType.Range; }
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
			public override ParameterType ConfigType
			{
				get { return ParameterType.Ipv4; }
			}
		}

		public class Ipv6Define : Define
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.Ipv6; }
			}
		}

		public class HwaddrDefine : Define
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.Hwaddr; }
			}
		}

		public class IfaceDefine : Define
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.Iface; }
			}
		}

		public class StrategyDefine : Define
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.Enum; }
			}

			public override string[] Defaults
			{
				get
				{
					return ClassLoader
						.GetAllByAttribute<MutationStrategyAttribute>(null)
						.Where(kv => kv.Key.IsDefault && !kv.Key.Internal)
						.Select(kv => kv.Key.Name)
						.ToArray();
				}
			}
		}

		public class EnumDefine : Define
		{
			[XmlIgnore]
			public Type EnumType { get; protected set; }
			
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

			public override ParameterType ConfigType
			{
				get { return ParameterType.Enum; }
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
			protected Collection()
			{
				Defines = new List<Define>();
			}

			[XmlIgnore]
			public abstract Platform.OS Platform { get; }

			[XmlElement("String", Type = typeof(StringDefine))]
			[XmlElement("Hex", Type = typeof(HexDefine))]
			[XmlElement("Range", Type = typeof(RangeDefine))]
			[XmlElement("Ipv4", Type = typeof(Ipv4Define))]
			[XmlElement("Ipv6", Type = typeof(Ipv6Define))]
			[XmlElement("Hwaddr", Type = typeof(HwaddrDefine))]
			[XmlElement("Iface", Type = typeof(IfaceDefine))]
			[XmlElement("Strategy", Type = typeof(StrategyDefine))]
			[XmlElement("Enum", Type = typeof(EnumDefine))]
			[XmlElement("Define", Type = typeof(UserDefine))]
			public List<Define> Defines { get; set; }
		}

		public class None : Collection
		{
			[XmlIgnore]
			public override Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.None; }
			}
		}

		public class Windows : Collection
		{
			[XmlIgnore]
			public override Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.Windows; }
			}
		}

		public class OSX : Collection
		{
			[XmlIgnore]
			public override Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.OSX; }
			}
		}

		public class Linux : Collection
		{
			[XmlIgnore]
			public override Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.Linux; }
			}
		}

		public class Unix : Collection
		{
			[XmlIgnore]
			public override Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.Unix; }
			}
		}

		public class All : Collection
		{
			[XmlIgnore]
			public override Platform.OS Platform
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

		public static List<Define> Parse(Stream stream)
		{
			return Parse(XmlTools.Deserialize<PitDefines>(stream));
		}

		public static List<Define> Parse(TextReader textReader)
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

		#region Evaluate

		public static List<KeyValuePair<string, string>> Evaluate(List<KeyValuePair<string, string>> defs)
		{
			var ret = new List<KeyValuePair<string, string>>(defs);

			var re = new Regex("##(\\w+?)##");

			var evaluator = new MatchEvaluator(delegate(Match m)
			{
				var key = m.Groups[1].Value;
				var val = ret.Where(_ => _.Key == key).Select(_ => _.Value).FirstOrDefault();

				return val ?? m.Groups[0].Value;
			});

			for (var i = 0; i < ret.Count; )
			{
				var oldVal = ret[i].Value;
				if (oldVal == null)
					throw new PeachException("Undefined PitDefine: \"{0}\"".Fmt(ret[i].Key));
				
				var newVal = re.Replace(oldVal, evaluator);

				if (oldVal != newVal)
					ret[i] = new KeyValuePair<string, string>(ret[i].Key, newVal);
				else
					++i;
			}

			return ret;
		}

		#endregion
	}
}
