using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Ionic.Zip;

namespace Peach.Pro.WebApi2.Utility
{
	internal class ZipResult : IHttpActionResult
	{
		private readonly DirectoryInfo _info;
		private readonly string _fileName;

		public ZipResult(string fileName, DirectoryInfo di)
		{
			_fileName = fileName;
			_info = di;
		}

		public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
		{
			var resp = new HttpResponseMessage
			{
				Content = new PushStreamContent((a,b,c) => WriteToStream(a, b, c))
			};

			var filename = Uri.EscapeDataString(_fileName);
			var contentDisposition = string.Format("attachment; filename*=utf-8''{0}", filename);
			var contentType = MimeMapping.GetMimeMapping(_fileName);

			resp.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(contentDisposition);
			resp.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

			return Task.FromResult(resp);
		}

		private void WriteToStream(Stream stream, HttpContent content, TransportContext context)
		{
			try
			{
				using (var zip = new ZipFile())
				{
					zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestSpeed;
					zip.AddDirectory(_info.FullName);
					zip.Save(stream);
				}
			}
			catch (HttpException)
			{
			}
			finally
			{
				stream.Close();
			}
		}
	}
}
