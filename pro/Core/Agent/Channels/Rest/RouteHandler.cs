using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	internal class RouteHandler
	{
		private class Route
		{
			public string Method { get; set; }
			public string Prefix { get; set; }
			public RequestHandler Handler { get; set; }
		}

		private readonly Dictionary<string, Route> _routes = new Dictionary<string, Route>();

		public delegate RouteResponse RequestHandler(HttpListenerRequest req);

		public void Add(string prefix, string method, RequestHandler handler)
		{
			_routes.Add(prefix, new Route
			{
				Method = method,
				Prefix = prefix,
				Handler = handler,
			});
		}

		public void Remove(string prefix)
		{
			if (!_routes.Remove(prefix))
				throw new KeyNotFoundException();
		}

		public RouteResponse Dispatch(HttpListenerRequest req)
		{
			Route value;

			if (!_routes.TryGetValue(req.Url.AbsolutePath, out value))
				return RouteResponse.NotFound();

			Debug.Assert(req.Url.AbsolutePath == value.Prefix);

			if (value.Method != req.HttpMethod)
				return RouteResponse.NotAllowed();

			try
			{
				return value.Handler(req);
			}
			catch (Exception ex)
			{
				return RouteResponse.Error(ex);
			}
		}
	}	
}
