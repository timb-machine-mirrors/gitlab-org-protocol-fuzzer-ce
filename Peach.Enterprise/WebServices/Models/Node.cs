using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Peach.Enterprise.WebServices.Models
{
	public enum NodeStatus
	{
		Alive,
		Late,
		Running,
	}

	public class Node
	{
		/// <summary>
		/// The URL of this node
		/// </summary>
		/// <example>
		/// "/p/nodes/{id}"
		/// </example>
		public string NodeUrl { get; set; }

		public string Name { get; set; }

		public string Mac { get; set; }

		public string Ip { get; set; }

		public List<Tag> Tags { get; set; }

		[JsonConverter(typeof(CamelCaseStringEnumConverter))]
		public NodeStatus Status { get; set; }

		public string Version { get; set; }

		public DateTime Timestamp { get; set; }

		public string JobUrl { get; set; }
	}
}
