using System;
using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices.Models
{
	public class PitMetadata
	{
		public List<Parameter> Defines { get; set; }
		public List<Parameter> Monitors { get; set; } 
	}

	public class Pit : LibraryPit
	{		
		public bool Locked { get; set; }

		public List<PitVersion> Versions { get; set; }

		public List<PeachVersion> Peaches { get; set; }

		public string User { get; set; }

		public DateTime Timestamp { get; set; }

		public List<Parameter> Config { get; set; }

		public List<Agent> Agents { get; set; }

		public PitMetadata Metadata { get; set; }
	}
}
