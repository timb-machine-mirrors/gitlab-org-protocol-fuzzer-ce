using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Analyzers.WebApi
{
	public enum WebApiParameterIn
	{
		Query,
		Header,
		Path,
		FormData,
		Body
	}

	public enum WebApiParameterType
	{
		Integer,
		Number,
		String,
		Boolean,
		Array,
		File
	}

	/// <summary>
	/// Web API Parameter
	/// </summary>
	public class WebApiParameter
	{
		/// <summary>
		/// Parameter from Swagger definition
		/// </summary>
		/// <remarks>
		/// When used by the web api proxy, new parameter
		/// is generated based on the request data.  In such
		/// a case this property is set to the matching swagger
		/// definition which can be used to identify differences
		/// between a documented possible operation and the actual
		/// request received by the proxy.
		/// 
		/// This property will be null if parameter comes from a 
		/// swagger definition
		/// </remarks>
		public WebApiParameter ShadowParameter { get; set; }

		/// <summary>
		/// Was this instance created via a swagger spec
		/// </summary>
		public bool FromSwagger { get; set; }

		/// <summary>
		/// Format identifier for path replacements
		/// </summary>
		public int PathFormatId { get; set; }

		/// <summary>
		/// Parameter name
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Where is parameter located (query, header, etc.)
		/// </summary>
		public WebApiParameterIn In { get; set; }

		/// <summary>
		/// Is parameter required?
		/// </summary>
		public bool Required { get; set; }

		/// <summary>
		/// Parameter type
		/// </summary>
		public WebApiParameterType Type { get; set; }

		/// <summary>
		/// Open string format hint for type.
		/// </summary>
		/// <remarks>
		/// Example:
		///    type: integer
		///    format: int32
		///    type: string
		///    format: uuid
		/// </remarks>
		public string Format { get; set; }

		/// <summary>
		/// If type == Array, item type
		/// </summary>
		public WebApiParameterType ArrayItemType { get; set; }

		/// <summary>
		/// If type == Array, item format
		/// </summary>
		public string ArrayItemFormat { get; set; }

		/// <summary>
		/// DataElement for this parameter
		/// </summary>
		public DataElement DataElement { get; set; }

		public WebApiParameter()
		{
			FromSwagger = false;
		}
	}
}
