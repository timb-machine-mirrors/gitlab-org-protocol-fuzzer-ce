using System;
using System.Collections.Generic;

namespace Peach.Enterprise.WebServices.Models
{
	public class LibraryPit
	{
		public string PitUrl { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public List<Tag> Tags { get; set; }
	}
}
