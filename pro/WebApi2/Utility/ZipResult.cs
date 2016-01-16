using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Peach.Pro.WebApi2.Utility
{
	public class ZipResult : IHttpActionResult
	{
		private readonly DirectoryInfo _info;
		private readonly string _filename;

		public ZipResult(string filename, DirectoryInfo di)
		{
			_filename = filename;
			_info = di;
		}

		public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
		{
			// TODO: Find a good way to do this

			var resp = new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.NotImplemented
			};

			return Task.FromResult(resp);
		}
	}
}
