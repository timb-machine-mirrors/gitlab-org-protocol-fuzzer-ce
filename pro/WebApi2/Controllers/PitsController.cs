using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;
using Peach.Pro.Core.WebServices.Models;
using Swashbuckle.Swagger.Annotations;
using Peach.Pro.Core.WebServices;

namespace Peach.Pro.WebApi2.Controllers
{
	[RoutePrefix(Prefix)]
	public class PitsController : BaseController
	{
		public const string Prefix = "p/pits";

		public PitsController()
			: base(null)
		{
		}

		/// <summary>
		/// Gets the list of all pits.
		/// </summary>
		/// <remarks>
		/// The result does not included configuration variables and monitoring configuration.
		/// </remarks>
		/// <returns></returns>
		[Route("")]
		public IEnumerable<LibraryPit> Get()
		{
			return PitDatabase.Entries;
		}

		/// <summary>
		/// Create a new pit configuration from a library pit.
		/// </summary>
		/// <param name="data">Source pit and destination configuration information.</param>
		/// <returns>The newly created pit configuration.</returns>
		[Route("")]
		[ResponseType(typeof(Pit))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified pit or library does not exist")]
		[SwaggerResponse(HttpStatusCode.BadRequest, Description = "Request did not contain a valid PitCopy")]
		[SwaggerResponse(HttpStatusCode.Forbidden, Description = "Access denied when saving config")]
		public IHttpActionResult Post([FromBody]PitCopy data)
		{
			try
			{
				PitDetail pit;
				if (!string.IsNullOrEmpty(data.LegacyPitUrl))
				{
					pit = PitDatabase.MigratePit(data.LegacyPitUrl, data.PitUrl);
				}
				else
				{
					pit = PitDatabase.CopyPit(
						data.PitUrl,
						data.Name,
						data.Description
					);
				}
				return Ok(pit.Pit);
			}
			catch (KeyNotFoundException)
			{
				return NotFound();
			}
			catch (UnauthorizedAccessException)
			{
				return Forbidden();
			}
			catch (ArgumentException)
			{
				return BadRequest();
			}
		}

		/// <summary>
		/// Get the details for a specific pit configuration.
		/// </summary>
		/// <remarks>
		/// The result does includes configuration variables and monitoring configuration.
		/// </remarks>
		/// <param name="id">Pit identifier.</param>
		/// <returns></returns>
		[Route("{id}")]
		[ResponseType(typeof(Pit))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified pit does not exist")]
		public IHttpActionResult Get(string id)
		{
			var pit = PitDatabase.GetPitById(id);
			if (pit == null)
				return NotFound();

			return Ok(pit);
		}

		/// <summary>
		/// Update the configuration for a specific pit
		/// </summary>
		/// <remarks>
		/// This is how you save new configuration variables and monitors.
		/// </remarks>
		/// <param name="id">The pit to update.</param>
		/// <param name="config">The variables and monitors configuration.</param>
		/// <returns>The pit with its updated configuration.</returns>
		[Route("{id}")]
		[ResponseType(typeof(Pit))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified pit does not exist")]
		[SwaggerResponse(HttpStatusCode.BadRequest, Description = "Request did not contain a valid PitConfig")]
		[SwaggerResponse(HttpStatusCode.Forbidden, Description = "Access denied when saving config")]
		public IHttpActionResult Post(string id, [FromBody]PitConfig config)
		{
			try
			{
				var pit = PitDatabase.UpdatePitById(id, config);
				return Ok(pit);
			}
			catch (KeyNotFoundException)
			{
				return NotFound();
			}
			catch (UnauthorizedAccessException)
			{
				return Forbidden();
			}
			catch (ArgumentException)
			{
				return BadRequest();
			}
		}
	}
}
