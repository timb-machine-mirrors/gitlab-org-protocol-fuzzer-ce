using System.Collections.Generic;

namespace Peach.Core.Agent
{
	/// <summary>
	/// The information recorded by a monitor.
	/// This can be a fault or just an arbitrary collection of data.
	/// </summary>
	public class MonitorData
	{
		/// <summary>
		/// The information about a fault detected by a monitor.
		/// </summary>
		public class Info
		{
			/// <summary>
			/// A one line title of the fault.
			/// </summary>
			public string Title { get; set; }

			/// <summary>
			/// A multi line description of the fault.
			/// </summary>
			public string Description { get; set; }

			/// <summary>
			/// The major hash bucket for this fault.
			/// </summary>
			public string MajorHash { get; set; }

			/// <summary>
			/// The minor hash bucket for this fault.
			/// </summary>
			public string MinorHash { get; set; }

			/// <summary>
			/// The risk assesment this fault.
			/// </summary>
			public string Risk { get; set; }

			/// <summary>
			/// Does the engine need to stop all fuzzing in response to this fault.
			/// </summary>
			public bool MustStop { get; set; }
		}

		public MonitorData()
		{
			Data = new Dictionary<string, byte[]>();
		}

		/// <summary>
		/// The name of the agent that hosts the monitor
		/// that produced this data.
		/// </summary>
		/// <remarks>
		/// This field is automatically set by the agent client.
		/// </remarks>
		public string AgentName { get; set; }

		/// <summary>
		/// The name of the monitor that produced this data.
		/// </summary>
		/// <remarks>
		/// This field is automatically set by the agent server.
		/// </remarks>
		public string MonitorName { get; set; }

		/// <summary>
		/// The type of monitor that produced this data.
		/// </summary>
		/// <remarks>
		/// This field does not need to be set by the monitor.
		/// The agent server will set the detection source
		/// to the monitor's class name automatically.
		/// If a single monitor class has multiple internal
		/// detection sources, this field can be used to
		/// distinguish between them.
		/// </remarks>
		public string DetectionSource { get; set; }

		/// <summary>
		/// If the monitor data is a fault, this field should
		/// be set with the appropriate information.
		/// </summary>
		public Info Fault { get; set; }

		/// <summary>
		/// A collection of arbitrary data recorded by the monitor.
		/// </summary>
		public Dictionary<string, byte[]> Data { get; set; }
	}
}
