using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aspose.Words;
using IronPython.Runtime;
using Peach.Core;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.Dom.Actions;

namespace Peach.Pro.Core.Analyzers.WebApi
{
	public enum WebApiOperationType
	{
		GET,
		PUT,
		POST,
		DELETE,
		OPTIONS,
		HEAD,
		PATCH
	}

	/// <summary>
	/// Operation for path "GET /p/jobs"
	/// </summary>
	public class WebApiOperation
	{
		public WebApiPath Path { get; set; }
		public WebApiOperationType Type { get; set; }
		public List<WebApiParameter> Parameters { get; set; }

		/// <summary>
		/// Name suitable for use as Name attribute in Action
		/// </summary>
		public string OperationId { get; set; }

		/// <summary>
		/// body element for this WebApiOperation. Maybe null.
		/// </summary>
		public DataElement Body { get; set; }

		/// <summary>
		/// Call action this WebApiOperation maps to
		/// </summary>
		public Call Call { get; set; }

		/// <summary>
		/// HTTP Method for operation
		/// </summary>
		public string Method
		{
			get { return Type.ToString(); }
			set
			{
				WebApiOperationType type;
				if (!WebApiOperationType.TryParse(value, out type))
					throw new PeachException("Invalid method found: " + value);

				Type = type;
			}
		}

		public WebApiOperation()
		{
			Parameters = new List<WebApiParameter>();
		}

		/// <summary>
		/// Regular expression to match path w/o query
		/// </summary>
		/// <returns></returns>
		public Regex PathRegex()
		{
			var operation = this;
			var urlRegex = operation.Parameters.Where(item => item.In == WebApiParameterIn.Path).
				Aggregate(Path.Path, (current, part) => current.Replace("{" + part.Name + "}", "(?<" + part.Name + ">[^/]+)"));

			return new Regex(urlRegex);
		}

		/// <summary>
		/// Set raw text onto body property.
		/// </summary>
		/// <param name="body"></param>
		public void SetRawBody(string body)
		{
			if (string.IsNullOrEmpty(body))
				return;

			Body = new Peach.Core.Dom.String("body") { DefaultValue = new Variant(body) };
		}

		/// <summary>
		/// Set a Json body payload onto body property.
		/// </summary>
		/// <param name="body"></param>
		public void SetJsonBody(string body)
		{
			if (string.IsNullOrEmpty(body))
				return;

			var analyzer = new JsonAnalyzer();

			var block = new Block();
			var json = new Peach.Core.Dom.String("body") { DefaultValue = new Variant(body) };
			block.Add(json);

			analyzer.asDataElement(json, new Dictionary<DataElement, Position>());

			Body = block[0];
		}

		/// <summary>
		/// Set an XML body payload onto body property.
		/// </summary>
		/// <param name="body"></param>
		public void SetXmlBody(string body)
		{
			if (string.IsNullOrEmpty(body))
				return;

			var analyzer = new XmlAnalyzer();

			var block = new Block();
			var json = new Peach.Core.Dom.String("body") { DefaultValue = new Variant(body) };
			block.Add(json);

			analyzer.asDataElement(json, new Dictionary<DataElement, Position>());

			Body = block[0];
		}
	}
}
