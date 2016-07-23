using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using NLog;
using Peach.Core;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Pro.Core.Analyzers;
using Peach.Pro.Core.Analyzers.WebApi;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Http;
using Double = Peach.Core.Dom.Double;

namespace PeachWebApiFuzzer
{
	/// <summary>
	/// Methods to process requests intercepted by the
	/// web api proxy.
	/// </summary>
	public class ProcessRequests
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public List<string> HeaderBlacklist { get; protected set; }
		public WebApiCollection Collection { get; protected set; }

		/// <summary>
		/// Add swagger definition to our collection of services
		/// </summary>
		/// <param name="json"></param>
		public void AddSwaggerJson(string json)
		{
			var apiEndPoint = SwaggerToWebApi.Convert(json);

			if(Collection == null)
				Collection = new WebApiCollection();

			Collection.EndPoints.Add(apiEndPoint);
		}

		/// <summary>
		/// Find a matching WebApiOperation based on incoming request
		/// </summary>
		/// <param name="request"></param>
		/// <returns>Returns null if operation not found.</returns>
		public WebApiOperation GetOperation(Request request)
		{
			var endPoint = GetWebApiEndPointFromRequest(request);
			if (endPoint == null)
			{
				logger.Warn("PopulateWebApiFromRequest: endPoint == null");
				return null;
			}

			var path = endPoint.Paths.Where(p => p.PathRegex().IsMatch(request.RequestUri.PathAndQuery)).OrderByDescending(p => p.Path.Length).FirstOrDefault();
			if (path == null)
			{
				logger.Warn("PopulateWebApiFromRequest: path == null");
				return null;
			}

			var method = request.Method.ToUpper();
			var op = path.Operations.FirstOrDefault(o => o.Method == method);
			if (op == null)
			{
				logger.Warn("PopulateWebApiFromRequest: op == null");
				return null;
			}

			return op;
		}

		/// <summary>
		/// Find matching WebApiOperation, clone and update with data from request.
		/// </summary>
		/// <param name="e"></param>
		/// <returns>Clone of WebApiOperation with only Parameters used by current request.</returns>
		public async Task<WebApiOperation> PopulateWebApiFromRequest(SessionEventArgs e)
		{
			try
			{
				var request = e.WebSession.Request;

				var op = new WebApiOperation
				{
					ShadowOperation = GetOperation(request)
				};

				// Process Path

				// Strip query string
				var pathString = request.RequestUri.PathAndQuery;
				var queryIndex = pathString.IndexOf('?');
				if (queryIndex > -1)
					pathString = pathString.Substring(0, queryIndex);

				if (op.ShadowOperation != null)
				{
					var pathMatch = op.ShadowOperation.Path.PathRegex().Match(pathString);
					op.Path = op.ShadowOperation.Path;

					// Copy over known path parameters
					op.Parameters.AddRange(op.ShadowOperation.Parameters.Where(p => p.In == WebApiParameterIn.Path));

					foreach (var param in op.Parameters)
					{
						var group = pathMatch.Groups[param.Name];
						if (!group.Success)
							continue;

						var value = group.Value;

						param.DataElement = new Peach.Core.Dom.String{ DefaultValue = new Variant(value)};
						param.ShadowParameter = param;
					}
				}
				else
				{
					var paths = pathString.Split('/');
					var cnt = 0;

					foreach (var path in paths)
					{
						if (string.IsNullOrEmpty(path))
							continue;

						var param = new WebApiParameter
						{
							In = WebApiParameterIn.Path,
							Name = path.ToLower(),
							Required = false,
							PathFormatId = cnt,
							DataElement = new Peach.Core.Dom.String { DefaultValue = new Variant(path) }
						};

						cnt++;
						op.Parameters.Add(param);
					}
				}

				// Query string

				var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
				foreach (var key in query.AllKeys)
				{
					var param = new WebApiParameter
					{
						In = WebApiParameterIn.Query,
						Name = key.ToLower(),
						Required = false,
						DataElement = new Peach.Core.Dom.String {DefaultValue = new Variant(query[key])}
					};

					if (op.ShadowOperation != null)
						param.ShadowParameter = op.ShadowOperation.Parameters.FirstOrDefault(
							i => i.In == WebApiParameterIn.Query && i.Name == param.Name);

					op.Parameters.Add(param);
				}

				// Headers

				foreach (var header in request.RequestHeaders)
				{
					var param = new WebApiParameter
					{
						In = WebApiParameterIn.Header,
						Name = header.Value.Name.ToLower(),
						Required = false,
						DataElement = new Peach.Core.Dom.String {DefaultValue = new Variant(header.Value.Value)}
					};

					if (op.ShadowOperation != null)
						param.ShadowParameter = op.ShadowOperation.Parameters.FirstOrDefault(
							i => i.In == WebApiParameterIn.Header && i.Name == param.Name);

					op.Parameters.Add(param);
				}

				// Form Data

				var contentTypeHeader = request.RequestHeaders.Select(i => i.Value).FirstOrDefault(i => i.Name.ToLower() == "content-type");
				var contentType = string.Empty;
				if(contentTypeHeader != null)
					contentType = contentTypeHeader.Value.ToLower();

				if (contentType == "application/x-www-form-urlencoded")
				{
					var bodyForm = HttpUtility.ParseQueryString(await e.GetRequestBodyAsString());
					foreach (var key in bodyForm.AllKeys)
					{
						var param = new WebApiParameter
						{
							In = WebApiParameterIn.FormData,
							Name = key,
							Required = false,
							DataElement = new Peach.Core.Dom.String { DefaultValue = new Variant(bodyForm[key]) }
						};

						if (op.ShadowOperation != null)
							param.ShadowParameter = op.ShadowOperation.Parameters.FirstOrDefault(
								i => i.In == WebApiParameterIn.FormData && i.Name == param.Name);

						op.Parameters.Add(param);
					}
				}
				else if(contentType == "application/json")
				{
					var param = new WebApiParameter
					{
						In = WebApiParameterIn.Body,
						Name = "jsonBody",
						Required = true,
					};

					var block = new Block { new Peach.Core.Dom.String() };
					block[0].DefaultValue = new Variant(await e.GetRequestBodyAsString());

					var jsonAnalyzer = new JsonAnalyzer();
					jsonAnalyzer.asDataElement(block, new Dictionary<DataElement, Position>());

					param.DataElement = block[0];

					if (op.ShadowOperation != null)
						param.ShadowParameter = op.ShadowOperation.Parameters.FirstOrDefault(
							i => i.In == WebApiParameterIn.Header && i.Name == param.Name);

					op.Parameters.Add(param);
				}
				else if (contentType == "application/xml" || contentType == "test/xml")
				{
					var param = new WebApiParameter
					{
						In = WebApiParameterIn.Body,
						Name = "xmlBody",
						Required = true,
					};

					var block = new Block { new Peach.Core.Dom.String() };
					block[0].DefaultValue = new Variant(await e.GetRequestBodyAsString());

					var xmlAnalyzer = new XmlAnalyzer();
					xmlAnalyzer.asDataElement(block, new Dictionary<DataElement, Position>());

					param.DataElement = block[0];

					if (op.ShadowOperation != null)
						param.ShadowParameter = op.ShadowOperation.Parameters.FirstOrDefault(
							i => i.In == WebApiParameterIn.Header && i.Name == param.Name);

					op.Parameters.Add(param);
				}
				else if (contentType != string.Empty)
				{
					var param = new WebApiParameter
					{
						In = WebApiParameterIn.Body,
						Name = "unknownBody",
						Required = true,
						DataElement = new Blob { DefaultValue = new Variant(await e.GetRequestBody()) }
					};

					var block = new Block { new Peach.Core.Dom.String() };
					block[0].DefaultValue = new Variant(await e.GetRequestBodyAsString());

					var xmlAnalyzer = new XmlAnalyzer();
					xmlAnalyzer.asDataElement(block, new Dictionary<DataElement, Position>());

					param.DataElement = block[0];

					if (op.ShadowOperation != null)
						param.ShadowParameter = op.ShadowOperation.Parameters.FirstOrDefault(
							i => i.In == WebApiParameterIn.Header && i.Name == param.Name);

					op.Parameters.Add(param);
				}

				op.DataModel = GenearteDataModelForOperation(op);

				return op;
			}
			catch (Exception ex)
			{
				logger.Error("PopulateWebApiFromRequest: Exception: {0}", ex.Message);
				throw;
				//return null;
			}
		}

		/// <summary>
		/// Generate a data model using DataElements on op.parameters.
		/// </summary>
		/// <remarks>
		/// This will *not* rebuild a data model if one already exists on the
		/// operation.
		/// </remarks>
		/// <param name="op"></param>
		/// <param name="clone">Clone DataElements from parameters</param>
		/// <returns></returns>
		public DataModel GenearteDataModelForOperation(WebApiOperation op, bool clone = false)
		{
			if (op.DataModel != null)
				return op.DataModel;

			var dm = new DataModel();

			foreach (var param in op.Parameters.OrderBy(i => i.In))
			{
				if (param.DataElement.parent != null)
					dm.Remove(param.DataElement.parent, false);

				dm.Add(param.DataElement);
			}

			return dm;
		}

		/// <summary>
		/// Update a Request instance from a web api operation
		/// </summary>
		/// <remarks>
		/// Typical flow is to mutate, then convert op into request
		/// </remarks>
		/// <param name="e"></param>
		/// <param name="op"></param>
		public void PopulateRequestFromWebApi(SessionEventArgs e, WebApiOperation op)
		{
			try
			{
				var request = e.WebSession.Request;
				var path = op.Path;

				// Path

				var newPath = path.Path;
				foreach (var param in op.Parameters.Where(p => p.In == WebApiParameterIn.Path))
				{
					logger.Trace("PopulateWebApiFromRequest: Setting param: " + param.Name);

					string value;
					var elem = param.DataElement;

					if (elem is DataModel)
						elem = ((DataModel)elem)[0];

					if (elem is Peach.Core.Dom.String)
						value = (string)elem.DefaultValue;

					else
					{
						logger.Trace("Failed to set param.DataElement.  Unknown element type: {0} For {1} {2}, {3}",
							param.DataElement, op.Method, op.Path.Path, param.Name);
						throw new ApplicationException("Failed to set param.DataElement.  Unknown element type: " + param.DataElement);
					}

					newPath = newPath.Replace("{" + param.PathFormatId + "}", value);
				}

				// Query string

				var queryString = new StringBuilder();
				foreach (var param in op.Parameters.Where(p => p.In == WebApiParameterIn.Query))
				{
					logger.Trace("PopulateWebApiFromRequest: Query string: " + param.Name);

					string value;
					var elem = param.DataElement;
					if (elem == null)
					{
						logger.Trace("Error, param.DataElement is null: {0} For {1} {2}, {3}",
							param.DataElement, op.Method, op.Path.Path, param.Name);
						throw new ApplicationException("Error, param.DataElement is null");
					}

					if (elem is DataModel)
						elem = ((DataModel)elem)[0];

					if (elem is Peach.Core.Dom.String)
						value = (string)elem.DefaultValue;

					else
					{
						logger.Trace("Failed to set param.DataElement.  Unknown element type: {0} For {1} {2}, {3}",
							param.DataElement, op.Method, op.Path.Path, param.Name);
						throw new ApplicationException("Failed to set param.DataElement.  Unknown element type: " + param.DataElement);
					}

					if (queryString.Length == 0)
						queryString.Append(string.Format("{0}={1}",
							HttpUtility.UrlEncode(param.Name),
							HttpUtility.UrlEncode(value)));
					else
						queryString.Append(string.Format("&{0}={1}",
							HttpUtility.UrlEncode(param.Name),
							HttpUtility.UrlEncode(value)));
				}

				var url = string.Format("{0}://{1}:{2}/{3}?{4}",
					request.RequestUri.Scheme, request.RequestUri.Host, request.RequestUri.Port, newPath, queryString);

				logger.Trace("New Url: {0}", url);

				request.RequestUri = new Uri(url);

				// Headers

				var headers = request.RequestHeaders;
				foreach (var param in op.Parameters.Where(p => p.In == WebApiParameterIn.Header))
				{
					logger.Trace("PopulateWebApiFromRequest: Header: " + param.Name);

					var header = headers.First(i => i.Value.Name == param.Name);
					var elem = param.DataElement;

					if (elem is DataModel)
						elem = ((DataModel)elem)[0];

					if (elem is Peach.Core.Dom.String)
						elem.DefaultValue = new Variant(header.Value.Value);

					else if (elem is Double)
						elem.DefaultValue = new Variant(float.Parse(header.Value.Value));

					else if (elem is Number)
						elem.DefaultValue = new Variant(int.Parse(header.Value.Value));

					else
						throw new ApplicationException("Failed to set param.DataElement.  Unknown element type: " + param.DataElement);
				}

				// TODO -- CONTINEU WITH THIS METHOD!

				/*
				// Form Data

				System.Collections.Specialized.NameValueCollection bodyForm = null;

				foreach (var param in op.Parameters.Where(p => p.In == WebApiParameterIn.FormData))
				{
					logger.Trace("PopulateWebApiFromRequest: Form Data: " + param.Name);

					if (bodyForm == null)
					{
						bodyForm = HttpUtility.ParseQueryString(e.GetRequestBodyAsString());
					}

					var value = bodyForm[param.Name];
					if (value == null)
					{
						if (param.Required)
						{
							logger.Error("PopulateWebApiFromRequest: Unable to find required body form parameter: {0} {1}, {2}",
								op.Method, op.Path.Path, param.Name);
						}

						activeParameters.Add(param);
						continue;
					}

					var elem = param.DataElement;

					if (elem is DataModel)
					{
						logger.Error("PopulateWebApiFromRequest: param.DataElement shouldn't be a DataModel for: {0} {1}, {2}",
							op.Method, op.Path.Path, param.Name);

						elem = ((DataModel)elem)[0];
					}

					if (elem is Peach.Core.Dom.String)
						elem.DefaultValue = new Variant(value);

					else if (elem is Double)
						elem.DefaultValue = new Variant(float.Parse(value));

					else if (elem is Number)
						elem.DefaultValue = new Variant(int.Parse(value));

					else
						throw new ApplicationException("Failed to set param.DataElement.  Unknown element type: " + param.DataElement);

					activeParameters.Add(param);
				}

				// Body

				foreach (var param in op.Parameters.Where(p => p.In == WebApiParameterIn.Body))
				{
					logger.Trace("PopulateWebApiFromRequest: Body: " + param.Name);

					if (param.DataElement is Blob)
					{
						logger.Trace("PopulateWebApiFromRequest: Binary Body");

						param.DataElement.DefaultValue = new Variant(e.GetRequestBody());

						activeParameters.Add(param);
						continue;
					}

					// MIKE: I wonder if there is any benefit to using the Swagger definition
					//       when parsing the body.

					if (param.DataElement is Peach.Pro.Core.Dom.IJsonElement)
					{
						logger.Trace("PopulateWebApiFromRequest: JSON Body");

						var block = new Block { new Peach.Core.Dom.String() };
						block[0].DefaultValue = new Variant(e.GetRequestBodyAsString());

						var jsonAnalyzer = new JsonAnalyzer();
						jsonAnalyzer.asDataElement(block, new Dictionary<DataElement, Position>());

						param.DataElement = block[0];

						activeParameters.Add(param);
						continue;
					}

					if (param.DataElement is XmlElement)
					{
						logger.Trace("PopulateWebApiFromRequest: XML Body");

						var block = new Block { new Peach.Core.Dom.String() };
						block[0].DefaultValue = new Variant(e.GetRequestBodyAsString());

						var xmlAnalyzer = new XmlAnalyzer();
						xmlAnalyzer.asDataElement(block, new Dictionary<DataElement, Position>());

						param.DataElement = block[0];

						activeParameters.Add(param);
						continue;
					}

					logger.Trace("Failed to set param.DataElement.  Unknown element type: {0} For {1} {2}, {3}",
						param.DataElement, op.Method, op.Path.Path, param.Name);
					throw new ApplicationException("Failed to set param.DataElement.  Unknown element type: " + param.DataElement);
				}
				
				logger.Trace("PopulateWebApiFromRequest: Returning op");

				op.Parameters = activeParameters;
				 
				return op;
				 */
			}
			catch (Exception ex)
			{
				logger.Error("PopulateWebApiFromRequest: Exception: {0}", ex.Message);
				//return null;
			}
		}

		public WebApiEndPoint GetWebApiEndPointFromRequest(Request request)
		{
			if (Collection.EndPoints.Count() == 1)
				return Collection.EndPoints[0];

			var host = request.RequestUri.Host.ToLower();
			if (!request.RequestUri.IsDefaultPort)
				host += ":" + request.RequestUri.Port;

			logger.Trace("GetWebApiEndPointFromRequest: host: " + host);

			// TODO - Add mapping ability
			// TODO - Add match by path

			return Collection.EndPoints.FirstOrDefault(ep => ep.Host == host);
		}

	}
}
