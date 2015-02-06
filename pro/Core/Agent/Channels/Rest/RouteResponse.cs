using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Peach.Core;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	internal class RouteResponse
	{
		public string ContentType { get; set; }

		public Stream Content { get; set; }

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
			var json = JsonConvert.SerializeObject(obj);
			var stream = new MemoryStream();
			var writer = new StreamWriter(stream, System.Text.Encoding.UTF8);

			writer.Write(json);
			writer.Flush();

			stream.Seek(0, SeekOrigin.Begin);

			return new RouteResponse
			{
				ContentType = "application/json;charset=utf-8",
				Content = stream,
				StatusCode = code,
			};
		}

		public static RouteResponse AsStream(Stream stream)
		{
			return new RouteResponse
			{
				ContentType = "application/octet-stream",
				Content = stream,
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

			return AsJson(resp, code);
		}
	}
}
