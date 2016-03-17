using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices.Models
{
	public class PitConfig
	{
		public string Id { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }

		public string OriginalPit { get; set; }

		public List<Param> Config { get; set; }

		public List<Agent> Agents { get; set; }

		public List<PitWeight> Weights { get; set; }
	}
}
