using Nancy;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Enterprise.WebServices
{
	public class FaultService : NancyModule
	{
		public static readonly string Prefix = "/p/faults";

		WebLogger logger;

		public FaultService(WebLogger logger)
			: base(Prefix)
		{
			this.logger = logger;

			Get[""] = _ => GetFaults();
			Get["/{id}"] = _ => GetFault(_.id);
		}

		object GetFaults()
		{
			return new object[0];
		}

		object GetFault(string id)
		{
			var ret = logger.GetFault(id);
			if (ret == null)
				return HttpStatusCode.NotFound;

			return ret;
		}
	}
}
