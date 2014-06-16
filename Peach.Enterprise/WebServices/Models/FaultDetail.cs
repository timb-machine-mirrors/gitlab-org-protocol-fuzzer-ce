using System;
using System.Collections.Generic;

namespace Peach.Enterprise.WebServices.Models
{
	public class FaultDetail : FaultSummary
	{
		/// <summary>
		/// The URL to the node that reported this fault.
		/// </summary>
		/// <example>
		/// "/p/nodes/{id}"
		/// </example>
		public string NodeUrl { get; set; }

		/// <summary>
		/// The URL of the target that this fault was detected against.
		/// </summary>
		/// <example>
		/// "/p/targets/{id}"
		/// </example>
		public string TargetUrl { get; set; }

		/// <summary>
		/// The URL of the target configuration that this fault was detected against.
		/// </summary>
		/// <example>
		/// "/p/targets/{target_id}/config/{config_id}"
		/// </example>
		public string TargetConfigUrl { get; set; }

		/// <summary>
		/// The URL of the specific version of the pit that this fault was detected against.
		/// TODO: Include version in the URL
		/// </summary>
		/// <example>
		/// "/p/pits/{id}"
		/// </example>
		public string PitUrl { get; set; }

		/// <summary>
		/// The URL of the specific version of peach that this fault was detected against.
		/// TODO: Include version in the URL
		/// </summary>
		/// <example>
		/// "/p/peaches/{id}"
		/// </example>
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
		public uint Seed { get; set; }

		/// <summary>
		/// The list of files that are part of this fault.
		/// </summary>
		public List<File> Files { get; set; }
	}
}
