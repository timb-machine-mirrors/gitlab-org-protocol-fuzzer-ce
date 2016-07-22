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
using Titanium.Web.Proxy.Network;
using Double = Peach.Core.Dom.Double;

namespace PeachWebApiFuzzer
{
	public class ProcessRequests
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

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

			var dom = new Dom();
			WebApiToDom.Convert(dom, Collection);

			Collection = Collection;
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
		public WebApiOperation PopulateWebApiFromRequest(SessionEventArgs e)
		{
			try
			{
				var request = e.ProxySession.Request;
				var activeParameters = new List<WebApiParameter>();

				var op = GetOperation(request);
				op = op.ShallowClone();

				// Path

				// Strip query string
				var pathString = request.RequestUri.PathAndQuery;
				var queryIndex = pathString.IndexOf('?');
				if (queryIndex > -1)
					pathString = pathString.Substring(0, queryIndex);

				var pathMatch = op.Path.PathRegex().Match(pathString);

				foreach (var param in op.Parameters.Where(p => p.In == WebApiParameterIn.Path))
				{
					var group = pathMatch.Groups[param.Name];
					if (!group.Success)
						continue;

					logger.Trace("PopulateWebApiFromRequest: Setting param: " + param.Name);

					var value = group.Value;
					var elem = param.DataElement;

					if (elem is DataModel)
						elem = ((DataModel)elem)[0];

					if (elem is Peach.Core.Dom.String)
						elem.DefaultValue = new Variant(value);

					else if (elem is Double)
						elem.DefaultValue = new Variant(float.Parse(value));

					else if (elem is Number)
						elem.DefaultValue = new Variant(int.Parse(value));

					else
					{
						logger.Trace("Failed to set param.DataElement.  Unknown element type: {0} For {1} {2}, {3}",
							param.DataElement, op.Method, op.Path.Path, param.Name);
						throw new ApplicationException("Failed to set param.DataElement.  Unknown element type: " + param.DataElement);
					}

					activeParameters.Add(param);
				}

				// Query string

				var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
				foreach (var param in op.Parameters.Where(p => p.In == WebApiParameterIn.Query))
				{
					var value = (string)query[param.Name];
					if (value == null)
					{
						if (param.Required)
						{
							logger.Error("PopulateWebApiFromRequest: Unable to find required query string parameter: {0} {1}, {2}",
								op.Method, op.Path.Path, param.Name);
						}

						continue;
					}

					logger.Trace("PopulateWebApiFromRequest: Query string: " + param.Name);

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
						elem.DefaultValue = new Variant(value);

					else if (elem is Double)
						elem.DefaultValue = new Variant(float.Parse(value));

					else if (elem is Number)
						elem.DefaultValue = new Variant(int.Parse(value));

					else
					{
						logger.Trace("Failed to set param.DataElement.  Unknown element type: {0} For {1} {2}, {3}",
							param.DataElement, op.Method, op.Path.Path, param.Name);
						throw new ApplicationException("Failed to set param.DataElement.  Unknown element type: " + param.DataElement);
					}

					activeParameters.Add(param);
				}

				// Headers

				var headers = request.RequestHeaders;
				foreach (var param in op.Parameters.Where(p => p.In == WebApiParameterIn.Header))
				{
					logger.Trace("PopulateWebApiFromRequest: Header: " + param.Name);

					var header = headers.First(i => i.Name.ToLower() == param.Name.ToLower());
					var elem = param.DataElement;

					if (elem is DataModel)
						elem = ((DataModel)elem)[0];

					if (elem is Peach.Core.Dom.String)
						elem.DefaultValue = new Variant(header.Value);

					else if (elem is Double)
						elem.DefaultValue = new Variant(float.Parse(header.Value));

					else if (elem is Number)
						elem.DefaultValue = new Variant(int.Parse(header.Value));

					else
						throw new ApplicationException("Failed to set param.DataElement.  Unknown element type: " + param.DataElement);

					activeParameters.Add(param);
				}

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
			}
			catch (Exception ex)
			{
				logger.Error("PopulateWebApiFromRequest: Exception: {0}", ex.Message);
				return null;
			}
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
				var request = e.ProxySession.Request;
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

					var header = headers.First(i => i.Name == param.Name);
					var elem = param.DataElement;

					if (elem is DataModel)
						elem = ((DataModel)elem)[0];

					if (elem is Peach.Core.Dom.String)
						elem.DefaultValue = new Variant(header.Value);

					else if (elem is Double)
						elem.DefaultValue = new Variant(float.Parse(header.Value));

					else if (elem is Number)
						elem.DefaultValue = new Variant(int.Parse(header.Value));

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
