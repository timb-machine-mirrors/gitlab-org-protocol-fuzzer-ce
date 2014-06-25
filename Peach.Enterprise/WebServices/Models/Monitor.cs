using System;
using System.Collections.Generic;

namespace Peach.Enterprise.WebServices.Models
{
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
		public string Param { get; set; }

		/// <summary>
		/// The value of the parameter
		/// </summary>
		/// <example>
		/// "Local Area Connection"
		/// </example>
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
	}

	/// <summary>
	/// Represents an agent in a pit file.
	/// Contains agent the location and list of monitors.
	/// </summary>
	public class Agent
	{
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
