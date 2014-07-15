using System;
using System.Collections.Generic;

namespace Peach.Enterprise.WebServices.Models
{
	public class PitMonitors
	{
		public string PitUrl { get; set; }

		public List<Agent> Monitors { get; set; }
	}
}
