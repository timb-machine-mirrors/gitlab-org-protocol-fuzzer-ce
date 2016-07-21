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
		/// <summary>
		/// Full path with format identifiers
		/// </summary>
		/// <remarks>
		/// Path with format id's for any WebApiParameters of 
		/// type Path. e.g. /foo/{FooId}/list
		/// </remarks>
		public string Path { get; set; }

		/// <summary>
		/// Operations that can occur on this path
		/// </summary>
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
				Aggregate(Path, (current, part) => current.Replace("{" + part.Name + "}", "(?<"+part.Name+">[^/]+)"));

			return new Regex(urlRegex);
		}
	}
}
