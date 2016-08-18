using System;
using System.Net;
using System.Web.Http;
using Peach.Pro.Core.WebApi;
using Peach.Pro.Core.WebServices;
using Peach.Pro.WebApi2.Utility;
using Swashbuckle.Swagger.Annotations;

namespace Peach.Pro.WebApi2.Controllers
{
	/// <summary>
	/// Contains information about a unit test being run thru the web proxy.
	/// </summary>
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

		/// <summary>
		/// Proxy Functionality
		/// </summary>
		/// <remarks>
		/// Contains all functionality needed to control web proxy fuzzer
		/// </remarks>
		public ProxyController(IJobMonitor jobMonitor)
		{
			_jobMonitor = jobMonitor;
		}

		/// <summary>
		/// Indicate that a unit test session is starting.
		/// </summary>
		/// <remarks>
		/// Should be called once at the start of a test session.
		/// This will return once the web proxy has been started and is ready to accept connections.
		/// </remarks>
		/// <param name="id">The job identifier the proxy corresponds to.</param>
		/// <returns></returns>
		[Route("{id}/sessionSetUp")]
		[SwaggerResponse(HttpStatusCode.OK, Description = "Request successfully processed")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified proxy does not exist")]
		[SwaggerResponse(HttpStatusCode.Forbidden, Description = "Job doesn't support web proxy operations")]
		public IHttpActionResult PutSessionSetUp(Guid id)
		{
			return WithActiveProxy(id, () => new SessionSetUpProxyEvent());
		}

		/// <summary>
		/// Indicate that a unit test session has finished.
		/// </summary>
		/// <remarks>
		/// Should be called once at the end of a test session.
		/// This will stop the job since there are no more tests to run.
		/// </remarks>
		/// <param name="id">The job identifier the proxy corresponds to.</param>
		/// <returns></returns>
		[Route("{id}/sessionTearDown")]
		[SwaggerResponse(HttpStatusCode.OK, Description = "Request successfully processed")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified proxy does not exist")]
		[SwaggerResponse(HttpStatusCode.Forbidden, Description = "Job doesn't support web proxy operations")]
		public IHttpActionResult PutSessionTearDown(Guid id)
		{
			return WithActiveProxy(id, () => new SessionTearDownProxyEvent());
		}

		/// <summary>
		/// Indicate that a test case is about to start.
		/// </summary>
		/// <param name="id">The job identifier the proxy corresponds to.</param>
		/// <returns></returns>
		[Route("{id}/testSetUp")]
		[SwaggerResponse(HttpStatusCode.OK, Description = "Request successfully processed")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified proxy does not exist")]
		[SwaggerResponse(HttpStatusCode.Forbidden, Description = "Job doesn't support web proxy operations")]
		public IHttpActionResult PutTestSetUp(Guid id)
		{
			return WithActiveProxy(id, () => new TestSetUpProxyEvent());
		}

		/// <summary>
		/// Indicate that a test case is finished.
		/// </summary>
		/// <param name="id">The job identifier the proxy corresponds to.</param>
		/// <returns></returns>
		[Route("{id}/testTearDown")]
		[SwaggerResponse(HttpStatusCode.OK, Description = "Request successfully processed")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified proxy does not exist")]
		[SwaggerResponse(HttpStatusCode.Forbidden, Description = "Job doesn't support web proxy operations")]
		public IHttpActionResult PutTestTearDown(Guid id)
		{
			return WithActiveProxy(id, () => new TestTearDownProxyEvent());
		}

		/// <summary>
		/// Indicate that a test case is has started.
		/// </summary>
		/// <param name="id">The job identifier the proxy corresponds to.</param>
		/// <param name="test">Information about the test case that is being run.</param>
		/// <returns></returns>
		[Route("{id}/testCase")]
		[SwaggerResponse(HttpStatusCode.OK, Description = "Request successfully processed")]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified proxy does not exist")]
		[SwaggerResponse(HttpStatusCode.Forbidden, Description = "Job doesn't support web proxy operations")]
		public IHttpActionResult PutTestCase(Guid id, [FromBody]TestCase test)
		{
			return WithActiveProxy(id, () => new TestProxyEvent { Name = test.Name });
		}

		private IHttpActionResult WithActiveProxy(Guid id, Func<IProxyEvent> fn)
		{
			var job = _jobMonitor.GetJob();
			if (job == null || job.Guid != id)
				return NotFound();

			var arg = fn();
			if (_jobMonitor.ProxyEvent(arg) && arg.Handled)
				return Ok();

			return StatusCode(HttpStatusCode.Forbidden);
		}
	}
}