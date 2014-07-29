using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Peach.Enterprise.WebServices.Models
{
	[Flags]
	public enum GroupAccess
	{
		None  = 0x0,
		Read  = 0x1,
		Write = 0x2,
	}

	public class Group
	{
		public string GroupUrl { get; set; }

		[JsonConverter(typeof(CamelCaseStringEnumConverter))]
		public GroupAccess Access { get; set; }
	}
}
