using Nancy;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Peach.Enterprise.WebServices
{
	public class NodeService : NancyModule
	{
		public static readonly string Prefix = "/p/nodes";

		WebLogger logger;

		public NodeService(WebLogger logger)
			: base(Prefix)
		{
			this.logger = logger;

			Get[""] = _ => GetNodes();
			Get["/{id}"] = _ => GetNode(_.id);
		}

		Node[] GetNodes()
		{
			return new[] { MakeNode() };
		}

		Node GetNode(string id)
		{
			if (id != logger.NodeGuid)
			{
				Context.Response.StatusCode = HttpStatusCode.NotFound;
				return null;
			}

			return MakeNode();
		}

		Node MakeNode()
		{
			// Make copy so we don't have to lock
			var guid = logger.JobGuid;

			var node = new Node()
			{
				NodeUrl = Prefix + "/" + logger.NodeGuid,
				Name = Environment.MachineName,
				Mac = "00:00:00:00:00:00",
				Ip = "0.0.0.0",
				Tags = new List<Tag>(),
				Status = NodeStatus.Alive,
				Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
				Timestamp = DateTime.UtcNow,
				JobUrl = guid != null ? NodeService.Prefix + "/" + guid : null
			};

			return node;
		}
	}
}
