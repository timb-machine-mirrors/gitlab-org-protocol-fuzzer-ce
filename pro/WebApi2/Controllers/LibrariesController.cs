using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.WebApi2.Utility;
using Swashbuckle.Swagger.Annotations;

namespace Peach.Pro.WebApi2.Controllers
{
	[RestrictedApi]
	[RoutePrefix(Prefix)]
	public class LibrariesController : BaseController
	{
		public const string Prefix = "p/libraries";

		public LibrariesController()
			:base(null)
		{
		}

		/// <summary>
		/// Gets the list of all libraries
		/// </summary>
		/// <example>
		/// GET /p/libraries
		/// </example>
		/// <remarks>
		/// Returns a list of all libraries
		/// </remarks>
		/// <returns>List of all libraries</returns>
		[Route("")]
		public IEnumerable<Library> Get()
		{
			return PitDatabase.Libraries;
		}

		/// <summary>
		/// Gets the details for specified library
		/// </summary>
		/// <example>
		/// GET /p/libraries/id
		/// </example>
		/// <remarks>
		/// The library details contains the list of all pits contained in the library
		/// </remarks>
		/// <param name="id">Library identifier</param>
		/// <returns>Library details</returns>
		[Route("{id}")]
		[ResponseType(typeof(Library))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified library does not exist")]
		public IHttpActionResult Get(string id)
		{
			var lib = PitDatabase.GetLibraryById(id);
			if (lib == null)
				return NotFound();

			return Ok(lib);
		}
	}
}
