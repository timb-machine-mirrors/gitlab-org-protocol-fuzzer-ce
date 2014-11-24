using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices.Models
{
	public class PitMonitors
	{
		public string PitUrl { get; set; }

		public List<Agent> Monitors { get; set; }
	}
}
