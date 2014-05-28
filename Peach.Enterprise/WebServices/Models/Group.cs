using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Peach.Enterprise.WebServices.Models
{
	[Flags]
	public enum GroupAccess
	{
		Read,
		Write,
	}

	public class Group
	{
		public string GroupUrl { get; set; }

		[JsonConverter(typeof(CamelCaseStringEnumConverter))]
		public GroupAccess Access { get; set; }
	}
}
