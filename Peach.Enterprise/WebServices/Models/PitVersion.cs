using System;
using System.Collections.Generic;

namespace Peach.Enterprise.WebServices.Models
{
	public class PitVersion
	{
		public uint Version { get; set; }

		public bool Configured { get; set; }

		public bool Locked { get; set; }

		public List<PitFile> Files { get; set; }

		public string User { get; set; }

		public DateTime Timestamp { get; set; }
	}
}
