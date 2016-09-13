using System;
using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices.Models
{
	[Serializable]
	public class WebProxy
	{
		public List<WebRoute> Routes { get; set; }
	}
}
