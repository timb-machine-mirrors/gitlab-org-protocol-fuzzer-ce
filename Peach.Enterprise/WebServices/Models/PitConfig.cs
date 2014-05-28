using System;
using System.Collections.Generic;

namespace Peach.Enterprise.WebServices.Models
{
	public class PitConfig
	{
		public string PitUrl { get; set; }
		public List<ConfigItem> Config { get; set; }
	}
}
