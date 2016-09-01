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
	public class NoSwaggerValuesController : ApiController
	{
		public const string Prefix = "unknown/api/values";

		public static string Filter = null;
		public static string Value = null;
		public static int Id = -1;
		public static string X_Peachy = null;
		public static string[] ArrayValue = null;
		public static bool MethodDelete = false;

		public static void Clear()
		{
			Filter = null;
			Value = null;
			Id = -1;
			X_Peachy = null;
			ArrayValue = null;
			MethodDelete = false;
		}

		// GET api/values 
		[Route("")]
		public IEnumerable<string> Get([FromUri]string filter = null, [FromUri]string[] a = null)
		{
			Clear();
			Filter = filter;
			ArrayValue = a;

			if (Request.Headers.Contains("X-Peachy"))
				X_Peachy = Request.Headers.GetValues("X-Peachy").First();

			return new string[] { "value1", "value2" };
		}

		// DELETE api/values 
		[Route("")]
		public StatusCodeResult Delete([FromUri]string filter = null, [FromUri]string[] a = null)
		{
			Clear();
			Filter = filter;
			ArrayValue = a;
			MethodDelete = true;

			if (Request.Headers.Contains("X-Peachy"))
				X_Peachy = Request.Headers.GetValues("X-Peachy").First();

			return new StatusCodeResult(HttpStatusCode.OK, this);
		}

		// GET api/values/5 
		[Route("{id}")]
		public string Get(int id)
		{
			Clear();
			Id = id;

			if (Request.Headers.Contains("X-Peachy"))
				X_Peachy = Request.Headers.GetValues("X-Peachy").First();

			return "value";
		}

		// POST api/values 
		[Route("")]
		public StatusCodeResult Post([FromBody]Value data)
		{
			Clear();
			Value = data.value;
			ArrayValue = data.a;

			if (Request.Headers.Contains("X-Peachy"))
				X_Peachy = Request.Headers.GetValues("X-Peachy").First();

			System.Diagnostics.Debug.Assert(data != null);
			return new StatusCodeResult(HttpStatusCode.Created, this);
		}

		// PUT api/values/5 
		[Route("{id}")]
		public StatusCodeResult Put(int id, [FromBody]Value value, [FromUri]string filter = null)
		{
			Clear();
			Id = id;
			Value = value.value;
			Filter = filter;

			if (Request.Headers.Contains("X-Peachy"))
				X_Peachy = Request.Headers.GetValues("X-Peachy").First();

			return new StatusCodeResult(HttpStatusCode.OK, this);
		}

		// DELETE api/values/5 
		[Route("{id}")]
		public void Delete(int id)
		{
			Clear();
			Id = id;

			if (Request.Headers.Contains("X-Peachy"))
				X_Peachy = Request.Headers.GetValues("X-Peachy").First();
		}
	}
}
