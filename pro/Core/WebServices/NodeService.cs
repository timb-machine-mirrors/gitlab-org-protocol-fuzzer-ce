using System;
using System.Collections.Generic;
using System.Reflection;
using Nancy;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core.WebServices
{
	public class NodeService : WebService
	{
		public static readonly string Prefix = "/p/nodes";

		public NodeService(WebContext context)
			: base(context, Prefix)
		{
			Get[""] = _ => GetNodes();
			Get["/{id}"] = _ => GetNode(_.id);
		}

		public static string MakeUrl(string id)
		{
			return string.Join("/", Prefix, id);
		}

		Response GetNodes()
		{
			return Response.AsJson(new[] { MakeNode() });
		}

		Response GetNode(string id)
		{
			if (id != NodeGuid)
				return HttpStatusCode.NotFound;

			return Response.AsJson(MakeNode());
		}

		Node MakeNode()
		{
			return new Node
			{
				NodeUrl = Prefix + "/" + NodeGuid,
				Name = Environment.MachineName,
				Mac = "00:00:00:00:00:00",
				Ip = "0.0.0.0",
				Tags = new List<Tag>(),
				Status = NodeStatus.Alive,
				Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
				Timestamp = DateTime.UtcNow,
				JobUrl = JobService.Prefix,
			};
		}
	}
}
