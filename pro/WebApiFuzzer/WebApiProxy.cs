using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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

		public ProcessRequests ProcessRequests { get; set; }

		public WebApiProxy()
		{
			ProcessRequests = new ProcessRequests();
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
			ProxyServer.BeforeRequest += OnRequest;

			var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8001, false);

			//Add an explicit endpoint where the client is aware of the proxy
			//So client would send request in a proxy friendly manner
			ProxyServer.AddEndPoint(explicitEndPoint);
			ProxyServer.Start();

			//Only explicit proxies can be set as a system proxy!
			//ProxyServer.SetAsSystemHttpProxy(explicitEndPoint);
			//ProxyServer.SetAsSystemHttpsProxy(explicitEndPoint);

			return this;
		}

		public void Dispose()
		{
			//Unsubscribe & Quit
			ProxyServer.BeforeRequest -= OnRequest;
			ProxyServer.Stop();
		}

		//Test On Request, intecept requests
		//Read browser URL send back to proxy by the injection script in OnResponse event
		public void OnRequest(object sender, SessionEventArgs e)
		{
#if DEBUG
			// Code to allow unit tests to work correctly
			if (e.ProxySession.Request.Url.StartsWith("http://localhost.:"))
			{
				var newUrl = e.ProxySession.Request.Url.Replace("http://localhost.:", "http://localhost:");
				e.ProxySession.Request.RequestUri = new Uri(newUrl);
			}
#endif

			Console.WriteLine(e.ProxySession.Request.Url);

			// 1. Get WebApiOperation

			var op = ProcessRequests.PopulateWebApiFromRequest(e);
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
