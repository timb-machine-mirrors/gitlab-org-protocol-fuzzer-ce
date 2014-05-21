using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Peach.Enterprise.WebServices.Models
{
	public enum ConfigType
	{
		String,
		Hex,
		Int,
		Ipv4,
		Ipv6,
		Hwaddr,
		Interface,
		Enum, // enumType
		//Range (min,max)
		//OnCall,
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
	}
}
