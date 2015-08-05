using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices.Models
{
	public class PitConfig
	{
		public string PitUrl { get; set; }

		public string Name { get; set; }

		public List<Parameter> Config { get; set; }

		public List<Agent> Agents { get; set; }
	}
}
