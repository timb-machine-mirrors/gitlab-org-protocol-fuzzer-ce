﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Dom.Actions;

namespace Peach.Pro.Core.Analyzers.WebApi
{
	enum WebApiUrlPartType
	{
		StringType,
		IntType,
		GuidType,
		EmptyType
	}

	class WebApiUrlPart
	{
		public string Value;
		public WebApiUrlPartType Type;

		public WebApiUrlPart(string value, WebApiUrlPartType type)
		{
			Value = value;
			Type = type;
		}
	}

	class WebApiUrlParam
	{
		public string Name;
		public string Value;
		public WebApiUrlPartType Type;

		public WebApiUrlParam(string name, string value, WebApiUrlPartType type)
		{
			Name = name;
			Value = value;
			Type = type;
		}
	}

	class WebApiUrl
	{
		public List<WebApiUrlPart> Parts = new List<WebApiUrlPart>();
		public List<WebApiUrlParam> Params = new List<WebApiUrlParam>();
		public string Url;

		private List<WebApiUrlPart> FuzzedParts = new List<WebApiUrlPart>();

		public WebApiUrl(string url)
		{
			Url = url;
		}

		public string GetMethodUrl()
		{
			var fuzzedParamCount = 0;
			var sb = new StringBuilder(Url.Length);

			sb.Append("##UrlPrefix##");

			for (var cnt = 0; cnt < Parts.Count; cnt++)
			{
				var part = Parts[cnt];

				if (part.Type == WebApiUrlPartType.StringType)
					sb.Append(part.Value);
				else
				{
					sb.AppendFormat("{{{0}}}", fuzzedParamCount);
					fuzzedParamCount++;

					FuzzedParts.Add(part);
				}

				if ((cnt + 1) < Parts.Count)
					sb.Append("/");
			}

			if (Params.Count == 0)
				return sb.ToString();

			sb.Append("?");

			for (var cnt = 0; cnt < Params.Count; cnt++)
			{
				var param = Params[cnt];

				if(param.Type == WebApiUrlPartType.EmptyType)
					sb.AppendFormat("{0}&",
						System.Web.HttpUtility.UrlEncode(param.Name));
				else
					sb.AppendFormat("{0}={{{1}}}&",
						System.Web.HttpUtility.UrlEncode(param.Name), fuzzedParamCount);

				fuzzedParamCount++;
			}

			return sb.ToString().TrimEnd('&');
		}

		public void ToPeach(Peach.Core.Dom.Dom dom, Call call)
		{
			foreach (var part in FuzzedParts)
			{
				var id = WebApiToDom.GenerateName();

				var data = new Peach.Core.Dom.String("value");
				data.Hints.Add("Peach.TypeTransform", new Hint("Peach.TypeTransform", "false"));
				data.DefaultValue = new Variant(part.Value);

				var param = new ActionParameter(id) {dataModel = new DataModel(id) {data}};

				call.parameters.Add(param);
				dom.dataModels.Add(param.dataModel);
			}

			foreach (var param in Params)
			{
				if (param.Type == WebApiUrlPartType.EmptyType) continue;

				var id = WebApiToDom.GenerateName();

				var data = new Peach.Core.Dom.String("value");
				data.Hints.Add("Peach.TypeTransform", new Hint("Peach.TypeTransform", "false"));
				data.DefaultValue = new Variant(param.Value);

				var p = new ActionParameter(id) {dataModel = new DataModel(id) {data}};

				call.parameters.Add(p);
				dom.dataModels.Add(p.dataModel);
			}
		}
	}
}
