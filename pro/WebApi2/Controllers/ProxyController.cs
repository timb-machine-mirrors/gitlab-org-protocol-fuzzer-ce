using System;
using System.Net;
using System.Web.Http;
using Peach.Pro.Core.WebServices;
using Peach.Pro.WebApi2.Utility;
using Swashbuckle.Swagger.Annotations;

namespace Peach.Pro.WebApi2.Controllers
{
	[Serializable]
	public class TestCase
	{
		/// <summary>
		/// The name of the test case being run.
		/// </summary>
		/// <example>
		/// "/p/pits/{id}"
		/// </example>
		public string Name { get; set; }
	}

	/// <summary>
	/// Proxy Functionality
	/// </summary>
	/// <remarks>
	/// Contains all functionality needed to control web proxy fuzzer
	/// </remarks>
	[NoCache]
	[RestrictedApi]
	[RoutePrefix(Prefix)]
	public class ProxyController : ApiController
	{
		public const string Prefix = "p/proxy";

		private readonly IJobMonitor _jobMonitor;

		public ProxyController(IJobMonitor jobMonitor)
		{
			_jobMonitor = jobMonitor;
		}

		[Route("{id}/sessionSetUp")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified proxy does not exist")]
		public IHttpActionResult PutSessionSetUp(Guid id)
		{
			return WithActiveProxy(id, Ok);
		}

		[Route("{id}/sessionTearDown")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified proxy does not exist")]
		public IHttpActionResult PutSessionTearDown(Guid id)
		{
			return WithActiveProxy(id, Ok);
		}

		[Route("{id}/testSetUp")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified proxy does not exist")]
		public IHttpActionResult PutTestSetUp(Guid id)
		{
			return WithActiveProxy(id, Ok);
		}

		[Route("{id}/testTearDown")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified proxy does not exist")]
		public IHttpActionResult PutTestTearDown(Guid id)
		{
			return WithActiveProxy(id, Ok);
		}

		[Route("{id}/testCase")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified proxy does not exist")]
		public IHttpActionResult PutTestCase(Guid id, [FromBody]TestCase request)
		{
			return WithActiveProxy(id, Ok);
		}

		private IHttpActionResult WithActiveProxy(Guid id, Func<IHttpActionResult> fn)
		{
			var job = _jobMonitor.GetJob();
			if (job == null || job.Guid != id)
				return NotFound();
			return fn();
		}
	}
}