using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peach.Pro.Core.Analyzers.WebApi
{
	/// <summary>
	/// Collection of WebApiEndPoints.  Each endpoint is a
	/// different service.
	/// </summary>
	public class WebApiCollection
	{
		public List<WebApiEndPoint> EndPoints { get; set; }

		public WebApiCollection()
		{
			EndPoints = new List<WebApiEndPoint>();
		}
	}
}
