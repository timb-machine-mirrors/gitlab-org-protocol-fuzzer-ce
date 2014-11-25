using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices.Models
{
	public class PitConfig
	{
		public string PitUrl { get; set; }
		public List<Parameter> Config { get; set; }
	}
}
