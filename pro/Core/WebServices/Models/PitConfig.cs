using System.Collections.Generic;
using System;

namespace Peach.Pro.Core.WebServices.Models
{
	[Serializable]
	public class PitConfig
	{
		public string Name { get; set; }

		public string Description { get; set; }

		public string OriginalPit { get; set; }

		public List<Param> Config { get; set; }

		public List<Agent> Agents { get; set; }

		public List<PitWeight> Weights { get; set; }
	}
}
