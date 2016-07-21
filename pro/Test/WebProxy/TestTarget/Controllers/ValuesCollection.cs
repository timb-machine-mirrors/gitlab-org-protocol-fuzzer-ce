using System.Collections.Generic;
using System.Web.Http;

namespace Peach.Pro.Test.WebProxy.TestTarget.Controllers
{
	[RoutePrefix(Prefix)]
	public class ValuesController : ApiController
	{
		public const string Prefix = "api/values";

		// GET api/values 
		[Route("")]
		public IEnumerable<string> Get()
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
		public void Post([FromBody]string value)
		{
		}

		// PUT api/values/5 
		[Route("{id}")]
		public void Put(int id, [FromBody]string value)
		{
		}

		// DELETE api/values/5 
		[Route("{id}")]
		public void Delete(int id)
		{
		}
	} 
}
