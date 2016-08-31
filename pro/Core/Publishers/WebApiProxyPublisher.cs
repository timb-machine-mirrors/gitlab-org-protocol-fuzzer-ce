using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Tls;
using Peach.Core;
using Peach.Pro.Core.WebApi;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Monitor = System.Threading.Monitor;
using Uri = System.Uri;

namespace Peach.Pro.Core.Publishers
{
	/// <summary>
	/// This publisher exists only to pass configuration parameters
	/// to the web api proxy. And perhaps to start it.
	/// </summary>
	[Publisher("WebApiProxy", Scope = PluginScope.Internal)]
	[Parameter("Port", typeof(int), "Port to listen on", "8001")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default infinite)", "-1")]
	[Parameter("ClientCert", typeof(string), "Path to client certificate in PEM format", "")]
	[Parameter("ClientKey", typeof(string), "Path to client private key in PEM format", "")]
	public class WebApiProxyPublisher : Publisher
	{
		public class BaseArgs : IProxyEvent
		{
			public bool Handled { get; set; }

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
		}

		private readonly ProxyServer _proxy = new ProxyServer();
		private readonly BlockingCollection<IProxyEvent> _requests = new BlockingCollection<IProxyEvent>();
		private readonly BlockingCollection<ResponseArgs> _responses = new BlockingCollection<ResponseArgs>();

		private static readonly NLog.Logger ClassLogger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return ClassLogger; } }

		private Certificate _clientCert;
		private AsymmetricKeyParameter _clientKey;

		public WebProxyStateModel Model { get; set; }

		public BlockingCollection<IProxyEvent> Requests { get { return _requests; } }

		public int Port { get; set; }
		public int Timeout { get; set; }
		public string ClientCert { get; set; }
		public string ClientKey { get; set; }

		public WebApiProxyPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			// listen to client request & server response events
			_proxy.BeforeRequest += OnRequest;
			_proxy.BeforeResponse += OnResponse;

			_proxy.SelectClientCertificate += (sender, eventArgs) =>
			{
				eventArgs.ClientCert = _clientCert;
				eventArgs.ClientPrivateKey = _clientKey;
			};
		}

		protected override void OnStart()
		{
			if (!string.IsNullOrEmpty(ClientCert))
			{
				try
				{
					using (var rdr = new StreamReader(ClientCert))
						_clientCert = ProxyServer.LoadPemCert(rdr);
				}
				catch (Exception ex)
				{
					throw new PeachException("Error reading client certificate '{0}'. {1}".Fmt(ClientCert, ex.Message), ex);
				}
			}

			if (!string.IsNullOrEmpty(ClientKey))
			{
				try
				{
					using (var rdr = new StreamReader(ClientKey))
						_clientKey = ProxyServer.LoadPemKey(rdr);
				}
				catch (Exception ex)
				{
					throw new PeachException("Error reading client private key '{0}'. {1}".Fmt(ClientKey, ex.Message), ex);
				}
			}

			var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, Port);

			//Add an explicit endpoint where the client is aware of the proxy
			//So client would send request in a proxy friendly manner
			_proxy.AddEndPoint(explicitEndPoint);

			if (!string.IsNullOrEmpty(Model.Options.Proxy))
			{
				Uri uri;
				if (!Uri.TryCreate(Model.Options.Proxy, UriKind.Absolute, out uri))
					throw new PeachException("The specified proxy '{0}' is not a valid uri.".Fmt(Model.Options.Proxy));

				_proxy.ExternalHttpProxy = new ExternalProxy { HostName = uri.DnsSafeHost, Port = uri.Port };
				_proxy.ExternalHttpsProxy = _proxy.ExternalHttpProxy;
			}

			_proxy.Start();

			// Update ephemeral port
			Port = _proxy.ProxyEndPoints[0].Port;

			Logger.Debug("Proxy listening at {0}:{1}", _proxy.ProxyEndPoints[0].IpAddress, _proxy.ProxyEndPoints[0].Port);
		}

		protected override void OnStop()
		{
			_proxy.Stop();

			_requests.CompleteAdding();

			foreach (var item in _requests)
			{
				item.Handled = false;
				item.Dispose();
			}
		}

		public IProxyEvent GetRequest()
		{
			IProxyEvent ret;
			if (!_requests.TryTake(out ret, Timeout))
				throw new TimeoutException();
			ret.Handled = true;
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

			ret.Handled = true;
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

			e.State = new SessionState {Route = route};

			var msg = new RequestArgs
			{
				Session = e,
				Route = route,
			};

			var abort = false;

			try
			{
				lock (e)
				{
					// Send RequestArgs to Engine thread
					_requests.Add(msg);

					// Wait for Engine thread to finish
					Monitor.Wait(e);
				}
			}
			catch (InvalidOperationException)
			{
				// We are stopped!
				abort = true;
			}

			if (abort)
			{
				await e.Ok("");
				return;
			}

			if (!msg.Handled)
			{
				// If WebPRoxyModel didn't handle the request, we don't need
				// to handle the response.
				e.State = null;
				return;
			}

			e.DisposingEvent += OnDisposing;

			if (req.RequestBody != null)
				await e.SetRequestBody(req.RequestBody);
		}

		private async Task OnResponse(object sender, SessionEventArgs e)
		{
			// If State is null then we decided to ignore the request.
			// This happens with requests that happen during unit test setup/teardown
			if (e.State == null)
				return;

			var route = ((SessionState)e.State).Route;
			var statusCode = int.Parse(e.WebSession.Response.ResponseStatusCode);

			var msg = new ResponseArgs
			{
				Session = e,
				Route = route,
			};

			try
			{
				await e.GetResponseBody();
			}
			catch (Exception ex)
			{
				e.WebSession.Response.Exception = ex;
			}

			// Not sure if this is what we want to do long term, but currently
			// we are testing for a fault in the proxy worker thread so we only
			// grab the response body when a fault occurs.
			// If we never call GetResponseBody() then the response will be
			// automatically streamed to the client and is probably more performant.
			if (route.FaultOnStatusCodes != null  && route.FaultOnStatusCodes.Contains(statusCode))
			{
				msg.Fault = true;
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

			if (e.WebSession.Response.Exception == null)
				e.WebSession.Response.Exception = new ObjectDisposedException("WebSession has been disposed.");

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
