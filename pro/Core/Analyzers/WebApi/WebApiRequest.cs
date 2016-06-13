using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Peach.Core;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.Dom.Actions;
using SocketHttpListener.Net;
using Encoding = System.Text.Encoding;

namespace Peach.Pro.Core.Analyzers.WebApi
{
	/// <summary>
	/// Convert a web api request to Peach pizzaz
	/// </summary>
	class WebApiRequest
	{
		private Dictionary<string, string> Headers = new Dictionary<string, string>();
		private DataElement Body = null;
		public string Method;
		public string Url;

		public WebApiRequest()
		{

		}

		/// <summary>
		/// Add a collection of headers
		/// </summary>
		/// <param name="headers"></param>
		public void ParseHeaders(string headers)
		{
			headers = headers.Replace("\r\n", "\n");

			foreach (var header in headers.Split('\n'))
			{
				var token = header.IndexOf(": ");
				var name = header.Substring(0, token);
				var value = header.Substring(token + 2);

				Headers.Add(name, value);
			}
		}

		public void SetRawBody(string body)
		{
			if (string.IsNullOrEmpty(body))
				return;

			Body = new Peach.Core.Dom.String("body") { DefaultValue = new Variant(body) };
		}

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

		WebApiUrl AnalyzeUrl()
		{
			var apiUrl = new WebApiUrl(Url);

			var guid = Guid.Empty;
			long l;

			var url = new Uri(Url);

			var path = url.AbsolutePath;
			var query = url.Query;

			var pathParts = path.Split('/');

			foreach (var part in pathParts)
			{
				if (Guid.TryParse(part, out guid))
				{
					apiUrl.Parts.Add(new WebApiUrlPart(part, WebApiUrlPartType.GuidType));
				}
				else if (long.TryParse(part, out l))
				{
					apiUrl.Parts.Add(new WebApiUrlPart(part, WebApiUrlPartType.IntType));
				}
				else
				{
					apiUrl.Parts.Add(new WebApiUrlPart(part, WebApiUrlPartType.StringType));
				}
			}

			if (string.IsNullOrEmpty(query))
				return apiUrl;

			var queryKeyValues = query.TrimStart('?').Split('&');

			foreach (var keyValue in queryKeyValues)
			{
				var keyValuePair = keyValue.Split('=');

				var key = HttpUtility.UrlDecode(keyValuePair[0], Encoding.UTF8);
				var value = HttpUtility.UrlDecode(keyValuePair[1], Encoding.UTF8);

				if (Guid.TryParse(value, out guid))
				{
					apiUrl.Params.Add(new WebApiUrlParam(key, value, WebApiUrlPartType.GuidType));
				}
				else if (long.TryParse(value, out l))
				{
					apiUrl.Params.Add(new WebApiUrlParam(key, value, WebApiUrlPartType.IntType));
				}
				else
				{
					apiUrl.Params.Add(new WebApiUrlParam(key, value, WebApiUrlPartType.StringType));
				}
			}

			return apiUrl;
		}

		public void ToPeach(Peach.Core.Dom.Dom dom)
		{
			var apiUrl = AnalyzeUrl();
			var call = new Call() { Name = Guid.NewGuid().ToString() };

			call.method = string.Format("{0} {1}", Method, apiUrl.GetMethodUrl());

			apiUrl.ToPeach(dom, call);

			if (Body != null)
			{
				var body = new ActionParameter("body");
				body.dataModel = new DataModel(call.Name);
				body.dataModel.Add(Body);

				call.parameters.Add(body);
				dom.dataModels.Add(body.dataModel);
			}

			switch (Method)
			{
				case "GET":
				case "POST":
					call.result = new ActionResult();
					call.result.dataModel = dom.dataModels["WebApiResult"];
					break;
			}

			var test = dom.tests["Default"];
			test.stateModel.states[0].actions.Add(call);
		}
	}

}
