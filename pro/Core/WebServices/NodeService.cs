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

		object GetNodes()
		{
			return new[] { MakeNode() };
		}

		object GetNode(string id)
		{
			if (id != NodeGuid)
				return HttpStatusCode.NotFound;

			return MakeNode();
		}

		Node MakeNode()
		{
			var job = JobRunner.GetDefault();
			var jobUrl = job != null ? JobService.Prefix + "/" + job.Guid : null;
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
				JobUrl = jobUrl,
			};
		}
	}
}
