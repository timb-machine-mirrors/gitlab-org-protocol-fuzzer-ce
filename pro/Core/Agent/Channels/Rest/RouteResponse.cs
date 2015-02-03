using System;
using System.Net;
using Newtonsoft.Json;
using Peach.Core;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	internal class RouteResponse
	{
		public string Content { get; set; }

		public HttpStatusCode StatusCode { get; set; }

		public static RouteResponse Success()
		{
			return new RouteResponse
			{
				Content = null,
				StatusCode = HttpStatusCode.OK,
			};
		}

		public static RouteResponse NotFound()
		{
			return new RouteResponse
			{
				Content = null,
				StatusCode = HttpStatusCode.NotFound,
			};
		}

		public static RouteResponse BadRequest()
		{
			return new RouteResponse
			{
				Content = null,
				StatusCode = HttpStatusCode.BadRequest,
			};
		}

		public static RouteResponse NotAllowed()
		{
			return new RouteResponse
			{
				Content = null,
				StatusCode = HttpStatusCode.MethodNotAllowed,
			};
		}

		public static RouteResponse AsJson(object obj, HttpStatusCode code = HttpStatusCode.OK)
		{
			return new RouteResponse
			{
				Content = ToJson(obj),
				StatusCode = HttpStatusCode.OK,
			};
		}

		public static RouteResponse Error(Exception ex)
		{
			var resp = new ExceptionResponse
			{
				Message = ex.Message,
				StackTrace = ex.ToString(),
			};

			var code = (ex is SoftException)
				? HttpStatusCode.ServiceUnavailable
				: HttpStatusCode.InternalServerError;

			return new RouteResponse
			{
				Content = ToJson(resp),
				StatusCode = code,
			};
		}

		public static string ToJson(object obj)
		{
			return JsonConvert.SerializeObject(obj);
		}
	}
}
