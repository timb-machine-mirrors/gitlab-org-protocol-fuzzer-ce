using System;
using Ionic.Zip;
using Nancy;
using Peach.Core;

namespace Peach.Pro.Core.WebServices.Utility
{
	public static class FormatterExtensions
	{
		public static Response AsStream(
			this IResponseFormatter formatter, 
			Func<System.IO.Stream> fn, 
			string contentType)
		{
			return new StreamResponse(fn, contentType);
		}

		public static Response AsZip(
			this IResponseFormatter formatter,
			string filename,
			Func<ZipFile> fn)
		{
			return new ZipResponse(filename, fn);
		}
	}

	public class StreamResponse : Response
	{
		public StreamResponse(Func<System.IO.Stream> fn, string contentType)
		{
			Contents = stream =>
			{
				using (var read = fn())
				{
					read.CopyTo(stream);
				}
			};
			ContentType = contentType;
			StatusCode = HttpStatusCode.OK;
		}
	}

	public class ZipResponse : Response
	{
		public ZipResponse(string filename, Func<ZipFile> fn)
		{
			Contents = stream =>
			{
				using (var zip = fn())
				{
					zip.Save(stream);
				}
			};
			Headers.Add("Content-Disposition", "attachment; filename=\"{0}\"".Fmt(filename));
			ContentType = "application/octet-stream";
			StatusCode = HttpStatusCode.OK;
		}
	}
}
