using Peach.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

			public abstract WebServices.Models.ParameterType ConfigType
			{
				get;
			}

			public virtual string[] Defaults
			{
				get { return new string[0]; }
			}

			public virtual int? Min
			{
				get { return null; }
			}

			public virtual uint? Max
			{
				get { return null; }
			}
		}

		/// <summary>
		/// Free form string
		/// </summary>
		public class StringDefine : Define
		{
			public override WebServices.Models.ParameterType ConfigType
			{
				get { return WebServices.Models.ParameterType.String; }
			}
		}

		public class HexDefine : Define
		{
			public override WebServices.Models.ParameterType ConfigType
			{
				get { return WebServices.Models.ParameterType.Hex; }
			}
		}

		public class RangeDefine : Define
		{
			[XmlAttribute("min")]
			public int MinValue { get; set; }

			[XmlAttribute("max")]
			public uint MaxValue { get; set; }

			public override WebServices.Models.ParameterType ConfigType
			{
				get { return WebServices.Models.ParameterType.Range; }
			}

			public override int? Min
			{
				get { return MinValue; }
			}

			public override uint? Max
			{
				get { return MaxValue; }
			}
		}

		public class Ipv4Define : Define
		{
			public override WebServices.Models.ParameterType ConfigType
			{
				get { return WebServices.Models.ParameterType.Ipv4; }
			}
		}

		public class Ipv6Define : Define
		{
			public override WebServices.Models.ParameterType ConfigType
			{
				get { return WebServices.Models.ParameterType.Ipv6; }
			}
		}

		public class HwaddrDefine : Define
		{
			public override WebServices.Models.ParameterType ConfigType
			{
				get { return WebServices.Models.ParameterType.Hwaddr; }
			}
		}

		public class IfaceDefine : Define
		{
			public override WebServices.Models.ParameterType ConfigType
			{
				get { return WebServices.Models.ParameterType.Iface; }
			}
		}

		public class StrategyDefine : Define
		{
			public override WebServices.Models.ParameterType ConfigType
			{
				get { return WebServices.Models.ParameterType.Enum; }
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

			public override WebServices.Models.ParameterType ConfigType
			{
				get { return WebServices.Models.ParameterType.Enum; }
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

			[XmlIgnore]
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
			[XmlIgnore]
			public override Peach.Core.Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.None; }
			}
		}

		public class Windows : Collection
		{
			[XmlIgnore]
			public override Peach.Core.Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.Windows; }
			}
		}

		public class OSX : Collection
		{
			[XmlIgnore]
			public override Peach.Core.Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.OSX; }
			}
		}

		public class Linux : Collection
		{
			[XmlIgnore]
			public override Peach.Core.Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.Linux; }
			}
		}

		public class Unix : Collection
		{
			[XmlIgnore]
			public override Peach.Core.Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.Unix; }
			}
		}

		public class All : Collection
		{
			[XmlIgnore]
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

			for (var i = 0; i < ret.Count;)
			{
				var oldVal = ret[i].Value;
				var newVal = re.Replace(oldVal, evaluator);

				if (oldVal != newVal)
					ret[i] = new KeyValuePair<string,string>(ret[i].Key, newVal);
				else
					++i;
			}

			return ret;
		}

		#endregion
	}
}
