using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Peach.Pro.WebApi2.Utility
{
	public class FileResult : IHttpActionResult
	{
		private readonly FileInfo _info;

		public FileResult(FileInfo info)
		{
			_info = info;
		}

		public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
		{
			// TODO: There has to be a better way to do this

			var resp = new HttpResponseMessage
			{
				Content = new StreamContent(File.OpenRead(_info.FullName))
			};

			var filename = Uri.EscapeDataString(_info.Name);
			var contentDisposition = string.Format("attachment; filename*=utf-8''{0}", filename);

			resp.Content.Headers.LastModified = _info.LastWriteTime;
			resp.Content.Headers.ContentLength = _info.Length;
			resp.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(contentDisposition);
			resp.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

			return Task.FromResult(resp);
		}
	}
}
