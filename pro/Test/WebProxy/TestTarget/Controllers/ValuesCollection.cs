using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using System.Web.Http.Results;

namespace Peach.Pro.Test.WebProxy.TestTarget.Controllers
{
	[RoutePrefix(Prefix)]
	public class ValuesController : ApiController
	{
		public const string Prefix = "api/values";

		// GET api/values 
		[Route("")]
		public IEnumerable<string> Get([FromUri]string filter = null)
		{
			return new string[] { "value1", "value2" };
		}

		// GET api/values/5 
		[Route("{id}")]
		public string Get(int id)
		{
			return "value";
		}

		// POST api/values 
		[Route("")]
		public StatusCodeResult Post([FromBody]Value data)
		{
			System.Diagnostics.Debug.Assert(data != null);
			return new StatusCodeResult(HttpStatusCode.Created, this);
		}

		// PUT api/values/5 
		[Route("{id}")]
		public StatusCodeResult Put(int id, [FromBody]Value value)
		{
			return new StatusCodeResult(HttpStatusCode.OK, this);
		}

		// DELETE api/values/5 
		[Route("{id}")]
		public void Delete(int id)
		{
		}
	}

	public class Value
	{
		public string value { get; set; }
	}
}
