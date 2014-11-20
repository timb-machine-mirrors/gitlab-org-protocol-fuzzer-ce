using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices.Models
{
	public enum ParameterType
	{
		String,
		Hex,
		Range,
		Ipv4,
		Ipv6,
		Hwaddr,
		Iface,
		Enum,
		Bool
	}

	/// <summary>
	/// A configuration parameter passed to a monitor.
	/// </summary>
	public class Parameter
	{
		/// <summary>
		/// The key of this param used by the wizard
		/// </summary>
		/// <example>
		/// "PcapDevice"
		/// </example>
		public string Key { get; set; }

		/// <summary>
		/// The name of the parameter given to the monitor
		/// </summary>
		/// <example>
		/// "Device"
		/// </example>
		/// TODO: Rename this to "Name"
		public string Param { get; set; }

		/// <summary>
		/// The value of the parameter
		/// </summary>
		/// <example>
		/// "Local Area Connection"
		/// </example>
		public string Value { get; set; }

		/// <summary>
		/// The type of the parameter
		/// </summary>
		/// <example>
		/// "string"
		/// </example>
		[Newtonsoft.Json.JsonConverter(typeof(CamelCaseStringEnumConverter))]
		public ParameterType Type { get; set; }

		/// <summary>
		/// List of values for enum types
		/// </summary>
		public List<string> Defaults { get; set; }

		/// <summary>
		/// Is this parameter required?
		/// </summary>
		public bool Required { get; set; }

		/// <summary>
		/// Description of the parameter
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public long? Min { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public ulong? Max { get; set; }

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

#if DISABLED
		The Path is disabled due to a bug deserializing integers in mono.
		Newtonsoft.Json tries to use BigInteger.Parse which throws
		a MissingMethodException on mono 2.10
		http://json.codeplex.com/workitem/24176

		/// <summary>
		/// The wizard path that resulted in this monitor
		/// </summary>
		public List<uint> Path { get; set; }
#endif

		/// <summary>
		/// The parameters to the monitor
		/// </summary>
		public List<Parameter> Map { get; set; }

		/// <summary>
		/// The description of the monitor instance
		/// </summary>
		/// <example>
		/// "Network capture on interface {PcapDevice} using {PcapFilter}"
		/// </example>
		public string Description { get; set; }

		/// <summary>
		/// User friendly name of the monitor instance
		/// </summary>
		public string Name { get; set; }
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
