using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using NLog;
using PacketDotNet.Utils;
using Peach.Core;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Pro.Core.Analyzers;
using Peach.Pro.Core.Analyzers.WebApi;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Network;
using Double = Peach.Core.Dom.Double;
using Encoding = System.Text.Encoding;

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

		public DataElement GetDefaultElement(Variant defaultValue = null)
		{
			var elem = new Peach.Core.Dom.String() {DefaultValue = defaultValue};
			elem.Hints.Add("Peach.TypeTransform", new Hint("Peach.TypeTransform", "false"));

			return elem;

		}

		/// <summary>
		/// Find matching WebApiOperation, clone and update with data from request.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="body"></param>
		/// <returns>Clone of WebApiOperation with only Parameters used by current request.</returns>
		public WebApiOperation PopulateWebApiFromRequest(SessionEventArgs e, byte[] body)
		{
			try
			{
				var request = e.ProxySession.Request;

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

						param.DataElement = GetDefaultElement(new Variant(value));
						param.ShadowParameter = param;
					}
				}
				else
				{
					var paths = pathString.Split('/');
					var cnt = 0;
					var apiPath = new WebApiPath();
					
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
							DataElement = GetDefaultElement(new Variant(path))
						};

						apiPath.Path += "/{" + param.Name + "}";

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
						DataElement = GetDefaultElement(new Variant(query[key]))
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
						Name = header.Name.ToLower(),
						Required = false,
						DataElement = GetDefaultElement(new Variant(header.Value))
					};

					if (op.ShadowOperation != null)
						param.ShadowParameter = op.ShadowOperation.Parameters.FirstOrDefault(
							i => i.In == WebApiParameterIn.Header && i.Name == param.Name);

					op.Parameters.Add(param);
				}

				// Form Data

				var contentTypeHeader = request.RequestHeaders.FirstOrDefault(i => i.Name.ToLower() == "content-type");
				var contentType = string.Empty;
				if(contentTypeHeader != null)
					contentType = contentTypeHeader.Value.ToLower();

				if (contentType == "application/x-www-form-urlencoded")
				{
					var bodyForm = HttpUtility.ParseQueryString(Encoding.UTF8.GetString(body));
					foreach (var key in bodyForm.AllKeys)
					{
						var param = new WebApiParameter
						{
							In = WebApiParameterIn.FormData,
							Name = key,
							Required = false,
							DataElement = GetDefaultElement(new Variant(bodyForm[key]))
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
					block[0].DefaultValue = new Variant(Encoding.UTF8.GetString(body));

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
					block[0].DefaultValue = new Variant(Encoding.UTF8.GetString(body));

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
						DataElement = new Blob { DefaultValue = new Variant(body) }
					};

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
				if (param.DataElement != null && param.DataElement.parent != null)
				{
					dm.Remove(param.DataElement.parent, false);
				}
				else if (param.DataElement == null)
				{
					param.DataElement = new Peach.Core.Dom.String();
					param.DataElement.Hints.Add("Peach.TypeTransform", new Hint("Peach.TypeTransform", "false"));
				}

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
		/// <param name="body"></param>
		/// <param name="op"></param>
		public void PopulateRequestFromWebApi(SessionEventArgs e, byte[] body, WebApiOperation op)
		{
			try
			{
				var request = e.ProxySession.Request;
				var path = op.Path;

				// Path

				var newPath = path.Path;
				foreach (var param in op.Parameters.Where(p => p.In == WebApiParameterIn.Path))
				{
					var value = (string)param.DataElement.InternalValue;
					newPath = newPath.Replace("{" + param.Name + "}", value);
				}

				// Query string

				var queryString = new StringBuilder();
				foreach (var param in op.Parameters.Where(p => p.In == WebApiParameterIn.Query))
				{
					var value = (string)param.DataElement.InternalValue;

					if (queryString.Length == 0)
						queryString.Append(string.Format("{0}={1}",
							HttpUtility.UrlEncode(param.Name),
							HttpUtility.UrlEncode(value)));
					else
						queryString.Append(string.Format("&{0}={1}",
							HttpUtility.UrlEncode(param.Name),
							HttpUtility.UrlEncode(value)));
				}

				var url = string.Format("{0}://{1}:{2}{3}{4}{5}{6}",
					request.RequestUri.Scheme, 
					request.RequestUri.Host, 
					request.RequestUri.Port, 
					newPath.StartsWith("/") ? "" : "/",
					newPath, 
					queryString.Length > 0 ? "?" : "",
					queryString);

				request.RequestUri = new Uri(url);

				// Headers

				var headers = request.RequestHeaders;
				foreach (var param in op.Parameters.Where(p => p.In == WebApiParameterIn.Header))
				{
					var header = headers.First(i => i.Name.ToLower() == param.Name.ToLower());
					header.Value = (string)param.DataElement.InternalValue;
				}

				// Form Data

				StringBuilder bodyForm = null;
				foreach (var param in op.Parameters.Where(p => p.In == WebApiParameterIn.FormData))
				{
					if (bodyForm == null)
						bodyForm = new StringBuilder();

					if (bodyForm.Length == 0)
						bodyForm.Append(string.Format("{0}={1}",
							HttpUtility.UrlEncode(param.Name),
							HttpUtility.UrlEncode((string)param.DataElement.InternalValue)));
					else
						bodyForm.Append(string.Format("&{0}={1}",
							HttpUtility.UrlEncode(param.Name),
							HttpUtility.UrlEncode((string)param.DataElement.InternalValue)));
				}

				if (bodyForm != null)
				{
					e.SetRequestBodyString(bodyForm.ToString());
					var contentLength = e.ProxySession.Request.RequestHeaders.First(h => h.Name.ToLower() == "content-length");
					contentLength.Value = bodyForm.Length.ToString();

					return;
				}

				// Body
				
				foreach (var param in op.Parameters.Where(p => p.In == WebApiParameterIn.Body))
				{
					var outStream = param.DataElement.Value;

					var buff = new byte[outStream.Length];

					outStream.Position = 0;
					outStream.Read(buff, 0, buff.Length);
					e.SetRequestBody(buff);

					var contentLength = e.ProxySession.Request.RequestHeaders.First(h => h.Name.ToLower() == "content-length");
					contentLength.Value = buff.Length.ToString();

					return;
				}
			}
			catch (Exception ex)
			{
				logger.Error("PopulateWebApiFromRequest: Exception: {0}", ex.Message);
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
