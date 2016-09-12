using System;

namespace Peach.Pro.Core.WebServices.Models
{
	[Serializable]
	public class WebHeader
	{
		public string Name { get; set; }
		public bool Mutate { get; set; }
	}
}
