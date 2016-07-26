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
		/// Inspect request after WebApiOperation but before fuzzing.
		/// </summary>
		private OnRequestInspector _onRequestInspectorPre;

		/// <summary>
		/// Inspect request after fuzzing
		/// </summary>
		private OnRequestInspector _onRequestInspectorPost;

		public ProcessRequests ProcessRequests { get; set; }

		public WebApiProxy()
		{
			ProcessRequests = new ProcessRequests();
		}

		/// <summary>
		/// Start proxy in background worker thread
		/// </summary>
		/// <param name="onRequestInspectorPre">Hook used to inspect requests pre-fuzzing. Used by unit tests.</param>
		/// <param name="onRequestInspectorPost">Hook used to inspect requests post-fuzzing. Used by unit tests.</param>
		/// <returns></returns>
		public WebApiProxy Start(OnRequestInspector onRequestInspectorPre = null, OnRequestInspector onRequestInspectorPost = null)
		{
			_onRequestInspectorPre = onRequestInspectorPre;
			_onRequestInspectorPost = onRequestInspectorPost;

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
			try
			{
#if DEBUG
				// Code to allow unit tests to work correctly
				if (e.ProxySession.Request.Url.StartsWith("http://localhost.:"))
				{
					var newUrl = e.ProxySession.Request.Url.Replace("http://localhost.:", "http://localhost:");
					e.ProxySession.Request.RequestUri = new Uri(newUrl);
				}
#endif

				logger.Trace("OnRequest: {0}", e.ProxySession.Request.Url);

				byte[] body = null;
				
				var contentLength = e.ProxySession.Request.RequestHeaders.FirstOrDefault(h => h.Name.ToLower() == "content-length");
				if(contentLength != null && int.Parse(contentLength.Value) > 0)
					body = e.GetRequestBody();

				// 1. Get WebApiOperation

				var op = ProcessRequests.PopulateWebApiFromRequest(e, body);
				if (op == null)
				{
					logger.Error("Unable to convert request to web api operation.");
					return;
				}

				// Call hook, this allows unit tests to function

				if (_onRequestInspectorPre != null)
					_onRequestInspectorPre(sender, e, op);

				// 2. Fuzz

				// 3. Update e.ProxySession.Request

				ProcessRequests.PopulateRequestFromWebApi(e, body, op);

				// Call hook, this allows unit tests to function

				if (_onRequestInspectorPost != null)
					_onRequestInspectorPost(sender, e, op);
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Exception during OnRequest: {0}", ex.Message);
				throw;
			}
		}
	}
}
