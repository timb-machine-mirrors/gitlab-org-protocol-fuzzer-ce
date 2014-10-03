using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Enterprise.WebServices.Utility
{
	public static class FormatterExtensions
	{
		public static Response AsStream(this IResponseFormatter formatter, Func<System.IO.Stream> readStream, string contentType)
		{
			return new StreamResponse(readStream, contentType);
		}
	}

	public class StreamResponse : Response
	{
		public StreamResponse(Func<System.IO.Stream> readStream, string contentType)
		{
			Contents = stream =>
			{
				using (var read = readStream())
				{
					read.CopyTo(stream);
				}
			};
			ContentType = contentType;
			StatusCode = HttpStatusCode.OK;
		}
	}
}
