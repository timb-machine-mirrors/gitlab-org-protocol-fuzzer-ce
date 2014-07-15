using Nancy;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Enterprise.WebServices
{
	public class FaultService : WebService
	{
		public static readonly string Prefix = "/p/faults";

		public FaultService(WebContext context)
			: base(context, Prefix)
		{
			Get[""] = _ => GetFaults();
			Get["/{id}"] = _ => GetFault(_.id);
		}

		object GetFaults()
		{
			return new object[0];
		}

		object GetFault(string id)
		{
			var ret = Logger.GetFault(id);
			if (ret == null)
				return HttpStatusCode.NotFound;

			return ret;
		}
	}
}
