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

			var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, Port);

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

			// We should never have more than one response
			// in the queue at a time.
			System.Diagnostics.Debug.Assert(_responses.Count == 0);

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

		public class SessionState
		{
			public WebProxyRoute Route;
			public WebApiOperation Op;
		}

		private async Task OnRequest(object sender, SessionEventArgs e)
		{
			var req = e.WebSession.Request;

			var route = FindRoute(req.Url);
			if (route == null)
				return;

			if(req.ContentLength >= 0)
				await e.GetRequestBody();

			e.DisposingEvent += OnDisposing;
			e.State = new SessionState {Route = route};

			var msg = new RequestArgs
			{
				Session = e,
				Route = route,
			};

			lock (e)
			{
				// Send RequestArgs to Engine thread
				_requests.Add(msg);

				// Wait for Engine thread to finish
				Monitor.Wait(e);
			}

			if (req.RequestBody != null)
				await e.SetRequestBody(req.RequestBody);
		}

		private async Task OnResponse(object sender, SessionEventArgs e)
		{
			var op = ((SessionState)e.State).Op;
			var route = ((SessionState)e.State).Route;
			var statusCode = int.Parse(e.WebSession.Response.ResponseStatusCode);

			var msg = new ResponseArgs
			{
				Session = e,
				Route = route,
			};

			// Not sure if this is what we want to do long term, but currently
			// we are testing for a fault in the proxy worker thread so we only
			// grab the response body when a fault occurs.
			// If we never call GetResponseBody() then the response will be
			// automatically streamed to the client and is probably more performant.
			if (route.FaultOnStatusCodes != null  && route.FaultOnStatusCodes.Contains(statusCode))
			{
				msg.Fault = true;
				msg.Request = e.WebSession.Request.ContentLength > 0 ? await e.GetRequestBodyAsString() : string.Empty;
				msg.Response = await e.GetResponseBodyAsString();
			}

			// Even if there is no fault, we need to signal the response was
			// successfully received so the user can know the fuzzed
			// request has been completly processed by the server.

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
				Route = ((SessionState)e.State).Route
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
