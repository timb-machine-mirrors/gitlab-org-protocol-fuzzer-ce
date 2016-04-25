using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Peach.Pro.Core.Analyzers.WebApi
{
	/// <summary>
	/// Web API path "/p/jobs"
	/// </summary>
	public class WebApiPath
	{
		public string Path { get; set; }
		public List<WebApiOperation> Operations { get; set; }

		public WebApiPath()
		{
			Operations = new List<WebApiOperation>();
		}

		/// <summary>
		/// Regular expression to match path w/o query
		/// </summary>
		/// <returns></returns>
		public Regex PathRegex()
		{
			var operation = Operations.First();
			var urlRegex = operation.Parameters.Where(item => item.In == WebApiParameterIn.Path).
				Aggregate(Path, (current, part) => current.Replace("{" + part.Name + "}", "[^/]+"));

			return new Regex(urlRegex);
		}
	}
}
