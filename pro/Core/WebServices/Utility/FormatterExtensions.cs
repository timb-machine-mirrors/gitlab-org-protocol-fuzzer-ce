using System;
using Ionic.Zip;
using Nancy;
using Peach.Core;
using System.IO;

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
			DirectoryInfo di)
		{
			return new ZipResponse(filename, di);
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

			Headers["Last-Modified"] = fi.LastWriteTimeUtc.ToString("R");
			Headers["Content-Length"] = fi.Length.ToString();
			Headers["Content-Disposition"] = "attachment; filename=\"{0}\"".Fmt(fi.Name);
			ContentType = MimeTypes.GetMimeType(fi.FullName); ;
			StatusCode = HttpStatusCode.OK;
		}
	}

	public class ZipResponse : Response
	{
		public ZipResponse(string filename, DirectoryInfo di)
		{
			Contents = stream =>
			{
				using (var zip = new ZipFile())
				{
					zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestSpeed;
					zip.AddDirectory(di.FullName);
					zip.Save(stream);
				}
			};
			Headers["Content-Disposition"] = "attachment; filename=\"{0}\"".Fmt(filename);
			ContentType = MimeTypes.GetMimeType(filename);
			StatusCode = HttpStatusCode.OK;
		}
	}
}
