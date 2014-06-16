using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Peach.Enterprise.WebServices.Models
{
	public enum ConfigType
	{
		Define,
		String,
		Hex,
		Range,
		Ipv4,
		Ipv6,
		Hwaddr,
		Iface,
		Enum,
	}

	public class ConfigItem
	{
		public string Key { get; set; }
		public string Value { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		[JsonConverter(typeof(CamelCaseStringEnumConverter))]
		public ConfigType Type { get; set; }
		public List<string> Defaults { get; set; }
		public long? Min { get; set; }
		public ulong? Max { get; set; }
	}
}
