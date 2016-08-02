using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using Peach.Core;
using Peach.Pro.Core.WebApi;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Monitor = System.Threading.Monitor;

namespace Peach.Pro.Core.Publishers
{
	/// <summary>
	/// This publisher exists only to pass configuration parameters
	/// to the web api proxy. And perhaps to start it.
	/// </summary>
	[Publisher("WebApiProxy", Scope = PluginScope.Internal)]
	[Parameter("Port", typeof(int), "Port to listen on", "8001")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default infinite)", "-1")]
	public class WebApiProxyPublisher : Publisher
	{
		public class BaseArgs : IDisposable
		{
			public WebProxyRoute Route { get; set; }
			public SessionEventArgs Session { get; set; }

			public void Dispose()
			{
				lock (Session)
					Monitor.Pulse(Session);
			}
		}

		public class RequestArgs : BaseArgs
		{
			public byte[] Body { get; set; }
		}

		public class ResponseArgs : BaseArgs
		{
			public bool Fault { get; set; }

			public string Request { get; set;}
			public string Response { get; set;}
		}

		private readonly ProxyServer _proxy = new ProxyServer();
		private readonly BlockingCollection<RequestArgs> _requests = new BlockingCollection<RequestArgs>();
		private readonly BlockingCollection<ResponseArgs> _responses = new BlockingCollection<ResponseArgs>();

		private static readonly NLog.Logger ClassLogger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return ClassLogger; } }

		public WebProxyStateModel Model { get; set; }

		public int Port { get; set; }
		public int Timeout { get; set; }

		public WebApiProxyPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnStart()
		{
			// listen to client request & server response events
			_proxy.BeforeRequest += OnRequest;
			_proxy.BeforeResponse += OnResponse;

			var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, Port, false);

			//Add an explicit endpoint where the client is aware of the proxy
			//So client would send request in a proxy friendly manner
			_proxy.AddEndPoint(explicitEndPoint);
			_proxy.Start();

			// Update ephemeral port
			Port = _proxy.ProxyEndPoints[0].Port;
		}

		protected override void OnStop()
		{
			_proxy.Stop();
		}

		public RequestArgs GetRequest()
		{
			RequestArgs ret;
			if (!_requests.TryTake(out ret, Timeout))
				throw new TimeoutException();
			return ret;
		}

		public ResponseArgs GetResponse()
		{
			ResponseArgs ret;
			if (!_responses.TryTake(out ret, Timeout))
				throw new TimeoutException();
			return ret;
		}

		private WebProxyRoute FindRoute(string url)
		{
			foreach (var r in Model.Options.Routes)
			{
				var urlRexex = "^" + r.Url.Replace("*", ".*").Replace("?", ".") + "$";
				if (!Regex.Match(url, urlRexex).Success)
					continue;

				return ObjectCopier.Clone(r);
			}

			return null;
		}

		private async Task OnRequest(object sender, SessionEventArgs e)
		{
			var req = e.WebSession.Request;

			var route = FindRoute(req.Url);
			if (route == null)
				return;

			var body = req.ContentLength >= 0
				? await e.GetRequestBody()
				: null;

			e.DisposingEvent += OnDisposing;
			e.State = route;

			var msg = new RequestArgs
			{
				Session = e,
				Route = route,
				Body = body
			};

			lock (e)
			{
				// Send RequestArgs to Engine thread
				_requests.Add(msg);

				// Wait for Engine thread to finish
				Monitor.Wait(e);
			}

			if (msg.Body != null)
				await e.SetRequestBody(body);
		}

		private async Task OnResponse(object sender, SessionEventArgs e)
		{
			var route = (WebProxyRoute)e.State;
			var statusCode = int.Parse(e.WebSession.Response.ResponseStatusCode);

			var msg = new ResponseArgs
			{
				Session = e,
				Route = route,
			};

			if (route.FaultOnStatusCodes != null  && route.FaultOnStatusCodes.Contains(statusCode))
			{
				msg.Fault = true;
				msg.Request = await e.GetRequestBodyAsString();
				msg.Response = await e.GetResponseBodyAsString();
			}

			lock (e)
			{
				// If we are delivering this to the engine as a response
				// we don't need to know when it gets disposed anymore
				e.DisposingEvent -= OnDisposing;
				_responses.Add(msg);
				Monitor.Wait(e);
			}
		}

		private void OnDisposing(object sender, SessionEventArgs e)
		{
			// OnDisposing is called if request is completed and we
			// have not signalled completion the the engine thread

			var msg = new ResponseArgs
			{
				Session = e,
				Route = (WebProxyRoute)e.State
			};

			lock (e)
			{
				e.DisposingEvent -= OnDisposing;
				_responses.Add(msg);
				Monitor.Wait(e);
			}
		}
	}
}
