using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices.Models
{
	public enum ParameterType
	{
		None,
		String,
		Hex,
		Range,
		Ipv4,
		Ipv6,
		Hwaddr,
		Iface,
		Enum,
		Bool,
		User,
		System,
		Call,
		Group,
		Space,
		Monitor,
	}

	/// <summary>
	/// A configuration parameter passed to a monitor.
	/// Parameter is a more verbose param.
	/// </summary>
	public class ParamDetail : Param
	{
		/// <summary>
		/// The type of the parameter
		/// </summary>
		/// <example>
		/// "string"
		/// </example>
		public ParameterType Type { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public List<ParamDetail> Items { get; set; }

		/// <summary>
		/// List of values for enum types
		/// </summary>
		public List<string> Options { get; set; }

		/// <summary>
		/// Is this parameter required?
		/// </summary>
		public string DefaultValue { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public long? Min { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public ulong? Max { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public bool Optional { get; set; }

		/// <summary>
		/// The set of operating systems that this monitor supports.
		/// </summary>
		/// <remarks>
		/// Only used with ParamterType.Monitor
		/// </remarks>
		public string OS { get; set; }
	}

	/// <summary>
	/// The most basic key/value pair used for all parameters.
	/// </summary>
	public class Param
	{
		/// <summary>
		/// Machine name used by peach.
		/// This wil not include spaces.
		/// </summary>
		/// <example>
		/// "Peach.Cwd" or GdbPath"
		/// </example>
		public string Key { get; set; }

		/// <summary>
		/// The human name of this parameter.
		/// </summary>
		/// <example>
		/// "Pcap Device" or "Peach Installation Directory"
		/// </example>
		public string Name { get; set; }

		/// <summary>
		/// Description of the parameter
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Parameter value.
		/// </summary>
		public string Value { get; set; }
	}

	/// <summary>
	/// Represents a single monitor instance.
	/// </summary>
	public class Monitor
	{
		/// <summary>
		/// The class of the monitor
		/// </summary>
		/// <example>
		/// "Pcap"
		/// </example>
		public string MonitorClass { get; set; }

		/// <summary>
		/// User friendly name of the monitor instance
		/// </summary>
		public string Name { get; set; }

		///// <summary>
		///// The parameters to the monitor
		///// </summary>
		public List<Param> Map { get; set; }
	}

	/// <summary>
	/// Represents an agent in a pit file.
	/// Contains agent the location and list of monitors.
	/// </summary>
	public class Agent
	{
		/// <summary>
		/// Name of the agent for reference in tests
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The agent location including the agent channel
		/// </summary>
		/// <example>
		/// "tcp://1.1.1.1"
		/// "local://localhost"
		/// </example>
		public string AgentUrl { get; set; }

		/// <summary>
		/// The list of monitors associated with the agent
		/// </summary>
		public List<Monitor> Monitors { get; set; }
	}
}
