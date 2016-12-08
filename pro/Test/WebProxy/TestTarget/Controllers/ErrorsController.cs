using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace Peach.Pro.Test.WebProxy.TestTarget.Controllers
{
	[RoutePrefix(Prefix)]
	public class ErrorsController : ApiController
	{
		public const string Prefix = "unknown/api/errors";

		[Route("{code}")]
		public StatusCodeResult Get(int code)
		{
			return new StatusCodeResult((HttpStatusCode) code, this);
		}
	}
}
