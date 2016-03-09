using System;
using System.Collections.Generic;
using System.IO;

namespace Peach.Core
{
	/// <summary>
	/// Type of fault
	/// </summary>
	public enum FaultType
	{
		Unknown,
		// Actual fault
		Fault,
		// Data collection
		Data
	}

	/// <summary>
	/// Fault detected during fuzzing run
	/// </summary>
	[Serializable]
	public class Fault
	{
		/// <summary>
		/// Data contained in fault.
		/// </summary>
		[Serializable]
		public class Data
		{
			public Data()
			{
			}

			public Data(string key, byte[] value)
			{
				Key = key;
				Value = value;
			}

			public string Key { get; set; }
			public byte[] Value { get; set; }

			/// <summary>
			/// Set by FileLogger with the location on disk
			/// of this file.
			/// </summary>
			public string Path { get; set; }
		}

		[Serializable]
		public class Mutation
		{
			public string element { get; set; }
			public string mutator { get; set; }
		}

		[Serializable]
		public class Model
		{
			public string dataSet { get; set; }
			public string parameter { get; set; }
			public string name { get; set; }
			public List<Mutation> mutations { get; set; }
		}

		[Serializable]
		public class Action
		{
			public string name;
			public string type;
			public List<Model> models { get; set; }
		}

		[Serializable]
		public class State
		{
			public string name { get; set; }
			public List<Action> actions { get; set; }
		}

		public bool mustStop = false;

		/// <summary>
		/// Iteration fault was detected on
		/// </summary>
		public uint iteration = 0;

		/// <summary>
		/// Start iteration of search when fault was detected
		/// </summary>
		public uint iterationStart = 0;

		/// <summary>
		/// End iteration of search when fault was detected
		/// </summary>
		public uint iterationStop = 0;

		/// <summary>
		/// Is this a control iteration.
		/// </summary>
		public bool controlIteration = false;

		/// <summary>
		/// Is this control operation also a recording iteration?
		/// </summary>
		public bool controlRecordingIteration = false;

		/// <summary>
		/// Type of fault
		/// </summary>
		public FaultType type = FaultType.Unknown;

		/// <summary>
		/// Who detected this fault?
		/// </summary>
		/// <remarks>
		/// Example: "PageHeap Monitor"
		/// Example: "Name (PageHeap Monitor)"
		/// </remarks>
		public string detectionSource = null;

		/// <summary>
		/// Name of monitor instance that created this fault
		/// </summary>
		/// <remarks>
		/// Set by the agent
		/// </remarks>
		public string monitorName = null;

		/// <summary>
		/// Agent this fault came from
		/// </summary>
		/// <remarks>
		/// Set by the AgentManager
		/// </remarks>
		public string agentName = null;

		/// <summary>
		/// Title of finding
		/// </summary>
		public string title = null;

		/// <summary>
		/// Multiline description and collection of information.
		/// </summary>
		public string description = null;

		/// <summary>
		/// Major hash of fault used for bucketting.
		/// </summary>
		public string majorHash = null;

		/// <summary>
		/// Minor hash of fault used for bucketting.
		/// </summary>
		public string minorHash = null;

		/// <summary>
		/// Exploitability of fault, used for bucketting.
		/// </summary>
		public string exploitability = null;

		/// <summary>
		/// Folder for fault to be collected under.  Only used when
		/// major/minor hashes and exploitability are not defined.
		/// </summary>
		public string folderName = null;

		/// <summary>
		/// Binary data collected about fault.  Key is filename, value is content.
		/// </summary>
		public List<Data> collectedData = new List<Data>();
		// Note: We can't use a Dictionary<> since it won't remote between mono and .net correctly

		/// <summary>
		/// List of all states run when fault was detected.
		/// </summary>
		public ICollection<State> states = new List<State>();
	}
}
