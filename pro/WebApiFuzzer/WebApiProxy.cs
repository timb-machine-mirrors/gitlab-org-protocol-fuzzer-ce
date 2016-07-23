using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using NLog;
using Peach.Core;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Pro.Core.Analyzers;
using Peach.Pro.Core.Analyzers.WebApi;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Network;
using Double = System.Double;

namespace PeachWebApiFuzzer
{
	public class WebApiProxy : IDisposable
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Hook to inspect during request processing
		/// </summary>
		/// <remarks>
		/// Added to enable testing.
		/// </remarks>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <param name="op"></param>
		public delegate void OnRequestInspector(object sender, SessionEventArgs e, WebApiOperation op);

		/// <summary>
		/// Hook used to inspect requests.
		/// </summary>
		private OnRequestInspector _onRequestInspector;

		private readonly ProxyServer _proxy;

		public ProcessRequests ProcessRequests { get; set; }

		public WebApiProxy()
		{
			ProcessRequests = new ProcessRequests();
			_proxy = new ProxyServer();
		}

		public IEnumerable<ProxyEndPoint> ProxyEndPoints
		{
			get { return _proxy.ProxyEndPoints; }
		}

		/// <summary>
		/// Start proxy in background worker thread
		/// </summary>
		/// <param name="onRequestInspector">Hook used to inspect requests. Used by unit tests.</param>
		/// <returns></returns>
		public WebApiProxy Start(OnRequestInspector onRequestInspector = null)
		{
			_onRequestInspector = onRequestInspector;

			// listen to client request & server response events
			_proxy.BeforeRequest += OnRequest;

			var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8001, false);

			//Add an explicit endpoint where the client is aware of the proxy
			//So client would send request in a proxy friendly manner
			_proxy.AddEndPoint(explicitEndPoint);
			_proxy.Start();

			//Only explicit proxies can be set as a system proxy!
			//ProxyServer.SetAsSystemHttpProxy(explicitEndPoint);
			//ProxyServer.SetAsSystemHttpsProxy(explicitEndPoint);

			return this;
		}

		public void Dispose()
		{
			//Unsubscribe & Quit
			_proxy.BeforeRequest -= OnRequest;
			_proxy.Stop();
			_proxy.Dispose();
		}

		//Test On Request, intecept requests
		//Read browser URL send back to proxy by the injection script in OnResponse event
		public async Task OnRequest(object sender, SessionEventArgs e)
		{
#if DEBUG
			// Code to allow unit tests to work correctly
			if (e.WebSession.Request.Url.StartsWith("http://localhost.:"))
			{
				var newUrl = e.WebSession.Request.Url.Replace("http://localhost.:", "http://localhost:");
				e.WebSession.Request.RequestUri = new Uri(newUrl);
			}
#endif

			Console.WriteLine(e.WebSession.Request.Url);

			// 1. Get WebApiOperation

			var op = await ProcessRequests.PopulateWebApiFromRequest(e);
			if (op == null)
			{
				logger.Debug("Request not found in swagger, skipping");
				return;
			}

			if (_onRequestInspector != null)
				_onRequestInspector(sender, e, op);

			// 2. Convert to dom

			// 3. Fuzz

			// 4. Update e.ProxySession.Request
		}
	}
}
