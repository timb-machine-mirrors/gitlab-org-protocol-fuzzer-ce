using System;
using System.Collections.Generic;
using System.IO;
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
		/// <summary>
		/// Operation from Swagger definition if it exists.
		/// </summary>
		/// <remarks>
		/// When used by the web api proxy, new operation
		/// is generated based on the request data.  In such
		/// a case this property is set to the matching swagger
		/// definition which can be used to identify differences
		/// between a documented possible operation and the actual
		/// request received by the proxy.
		/// 
		/// This property will be null if Operation comes from a 
		/// swagger definition
		/// </remarks>
		public WebApiOperation ShadowOperation { get; set; }

		/// <summary>
		/// Was this instance created via a swagger spec
		/// </summary>
		public bool FromSwagger { get; set; }

		/// <summary>
		/// Path this operation is for.
		/// </summary>
		public WebApiPath Path { get; set; }

		/// <summary>
		/// Type of operation (HTTP Verb)
		/// </summary>
		public WebApiOperationType Type { get; set; }

		/// <summary>
		/// Parameters for this operation.
		/// </summary>
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
		/// DataModel for this operation. Contains all parameters and body.
		/// </summary>
		public DataModel DataModel { get; set; }

		/// <summary>
		/// HTTP Method for operation
		/// </summary>
		public string Method
		{
			get { return Type.ToString(); }
			set
			{
				WebApiOperationType type;
				if (!Enum.TryParse(value, out type))
					throw new PeachException("Invalid method found: " + value);

				Type = type;
			}
		}

		public WebApiOperation()
		{
			Parameters = new List<WebApiParameter>();
			FromSwagger = false;
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
		public void SetRawBody(byte[] body)
		{
			if (body == null)
				return;

			Body = new Blob("body") { DefaultValue = new Variant(body) };
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

		public WebApiOperation ShallowClone()
		{
			var newOp = new WebApiOperation
			{
				Method = Method,
				Path = Path,
				Body = Body,
				Call = Call,
				OperationId = OperationId,
				Type = Type,
				Parameters = Parameters
			};

			return newOp;
		}
	}
}
