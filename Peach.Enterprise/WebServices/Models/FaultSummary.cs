using System;

namespace Peach.Enterprise.WebServices.Models
{
	public class FaultSummary
	{
		/// <summary>
		/// The URL to the FaultDetail for this fault.
		/// </summary>
		/// <example>
		/// "/p/faults/{id}"
		/// </example>
		public string FaultUrl { get; set; }

		/// <summary>
		/// Was this fault reproducable.
		/// </summary>
		public bool Reproducable { get; set; }

		/// <summary>
		/// The iteration this fault was detected on.
		/// </summary>
		public uint Iteration { get; set; }

		/// <summary>
		/// The time this fault was recorded at.
		/// </summary>
		public DateTime TimeStamp { get; set; }

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
}
