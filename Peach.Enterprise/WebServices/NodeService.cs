using Nancy;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Enterprise.WebServices
{
	public class NodeService : NancyModule
	{
		public NodeService()
			: base("/p/nodes")
		{
			Get[""] = _ => GetNodes();
			Get["/{id}"] = _ => GetNode(_.id);
		}

		Node[] GetNodes()
		{
			return new Node[0];
		}

		Node GetNode(string id)
		{
			Context.Response.StatusCode = HttpStatusCode.NotFound;
			return null;
		}
	}
}
