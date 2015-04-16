using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Peach.Pro.Core.Storage;

namespace Peach.Pro.Core.WebServices.Models
{
	public class FaultSummary
	{
		/// <summary>
		/// Unique ID for this fault.
		/// </summary>
		[Key]
		public long Id { get; set; }

		/// <summary>
		/// The URL to the FaultDetail for this fault.
		/// </summary>
		/// <example>
		/// "/p/faults/{id}"
		/// </example>
		[NotMapped]
		public string FaultUrl { get; set; }

		/// <summary>
		/// The URL to download a zip archive of the entire fault data
		/// </summary>
		[NotMapped]
		public string ArchiveUrl { get; set; }

		/// <summary>
		/// Was this fault reproducable.
		/// </summary>
		public bool Reproducable { get; set; }

		/// <summary>
		/// The iteration this fault was detected on.
		/// </summary>
		public long Iteration { get; set; }

		/// <summary>
		/// The time this fault was recorded at.
		/// </summary>
		public DateTime TimeStamp
		{
			get { return _timestamp; }
			set { _timestamp = value.MakeUtc(); }
		}
		private DateTime _timestamp;

		/// <summary>
		/// The fault bucket used to group similar faults.
		/// </summary>
		public string BucketName { get; set; }

		/// <summary>
		/// The monitor that generated this fault.
		/// </summary>
		public string Source { get; set; }

		/// <summary>
		/// An exploitablilty rating of this fault.
		/// </summary>
		public string Exploitability { get; set; }

		/// <summary>
		/// The major hash for this fault.
		/// </summary>
		public string MajorHash { get; set; }

		/// <summary>
		/// The minor hash for this fault.
		/// </summary>
		public string MinorHash { get; set; }
	}

	public class FaultDetail : FaultSummary
	{
		/// <summary>
		/// The URL to the node that reported this fault.
		/// </summary>
		/// <example>
		/// "/p/nodes/{id}"
		/// </example>
		[NotMapped]
		public string NodeUrl { get; set; }

		/// <summary>
		/// The URL of the target that this fault was detected against.
		/// </summary>
		/// <example>
		/// "/p/targets/{id}"
		/// </example>
		[NotMapped]
		public string TargetUrl { get; set; }

		/// <summary>
		/// The URL of the target configuration that this fault was detected against.
		/// </summary>
		/// <example>
		/// "/p/targets/{target_id}/config/{config_id}"
		/// </example>
		[NotMapped]
		public string TargetConfigUrl { get; set; }

		/// <summary>
		/// The URL of the specific version of the pit that this fault was detected against.
		/// TODO: Include version in the URL
		/// </summary>
		/// <example>
		/// "/p/pits/{id}"
		/// </example>
		[NotMapped]
		public string PitUrl { get; set; }

		/// <summary>
		/// The URL of the specific version of peach that this fault was detected against.
		/// TODO: Include version in the URL
		/// </summary>
		/// <example>
		/// "/p/peaches/{id}"
		/// </example>
		[NotMapped]
		public string PeachUrl { get; set; }

		/// <summary>
		/// The title of the fault.
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// The description of the fault.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// The seed used by peach when this fault was detected.
		/// </summary>
		public long Seed { get; set; }

		/// <summary>
		/// The start iteration used for reproducing this fault.
		/// </summary>
		public long IterationStart { get; set; }

		/// <summary>
		/// The end iteration used for reproducing this fault.
		/// </summary>
		public long IterationStop { get; set; }

		/// <summary>
		/// The list of files that are part of this fault.
		/// </summary>
		[NotMapped]
		public ICollection<FaultFile> Files { get; set; }

		[JsonIgnore]
		public string FaultPath { get; set; }
	}

	public class FaultFile
	{
		/// <summary>
		/// Unique ID of the file.
		/// </summary>
		[Key]
		public long Id { get; set; }

		/// <summary>
		/// Foreign key to FaultDetail table
		/// </summary>
		[JsonIgnore]
		[ForeignKey(typeof(FaultDetail))]
		public long FaultDetailId { get; set; }

		/// <summary>
		///  The name of the file.
		/// </summary>
		/// <example>
		/// "WinAgent.Monitor.WindowsDebugEngine.description.txt"
		/// </example>
		public string Name { get; set; }

		/// <summary>
		///  The full name of the file including path.
		/// </summary>
		/// <example>
		/// "Faults/PROBABLY_EXPLOITABLE_0x63103514_0x32621b6f/13/WinAgent.Monitor.WindowsDebugEngine.description.txt"
		/// </example>
		public string FullName { get; set; }

		/// <summary>
		/// The location to download the contents of the file.
		/// </summary>
		/// <example>
		/// "/p/files/{guid}"
		/// </example>
		[NotMapped]
		public string FileUrl { get; set; }

		/// <summary>
		/// The size of the contents of the file.
		/// </summary>
		/// <example>
		/// 1024
		/// </example>
		public long Size { get; set; }
	}
}
