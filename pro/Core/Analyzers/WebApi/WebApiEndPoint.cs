using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peach.Pro.Core.Analyzers.WebApi
{
	public enum WebApiScheme
	{
		HTTP,
		HTTPS
	}

	/// <summary>
	/// Web API End Point
	/// </summary>
	/// <remarks>
	/// An end point has one or more paths exposed.  Each
	/// path will have one or more operations (GET, POST, etc.)
	/// 
	/// A Swagger document will define an API endpoint.  Multiple
	/// endpoints might comprise a WebApiCollection.
	/// </remarks>
	public class WebApiEndPoint
	{
		public string Host { get; set; }
		public List<WebApiScheme> Schemes { get; set; }
		public List<WebApiPath> Paths { get; set; }

		public WebApiEndPoint()
		{
			Schemes = new List<WebApiScheme>();
			Paths = new List<WebApiPath>();
		}

		public IEnumerable<WebApiOperation> Operations
		{
			get {
				return Paths.SelectMany(path => path.Operations);
			}
		}
	}
}
