using System;
using Ionic.Zip;
using Nancy;
using Peach.Core;

namespace Peach.Pro.Core.WebServices.Utility
{
	public static class FormatterExtensions
	{
		public static Response AsFile(
			this IResponseFormatter formatter, 
			System.IO.FileInfo fi)
		{
			return new FileResponse(fi);
		}

		public static Response AsZip(
			this IResponseFormatter formatter,
			string filename,
			Func<ZipFile> fn)
		{
			return new ZipResponse(filename, fn);
		}
	}

	public class FileResponse : Response
	{
		public FileResponse(System.IO.FileInfo fi)
		{
			Contents = stream =>
			{
				using (var fs = fi.OpenRead())
				{
					fs.CopyTo(stream);
				}
			};
			Headers.Add("Content-Length", fi.Length.ToString());
			Headers.Add("Content-Disposition", "attachment; filename=\"{0}\"".Fmt(fi.Name));
			ContentType = "application/octet-stream";
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
