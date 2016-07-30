using System;
using System.Collections.Generic;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Pro.Core.WebApi;
using Peach.Pro.Core.WebApi.Proxy;
using Titanium.Web.Proxy.EventArguments;

namespace Peach.Pro.Core.Publishers
{
	/// <summary>
	/// This publisher exists only to pass configuration parameters
	/// to the web api proxy. And perhaps to start it.
	/// </summary>
	[Publisher("WebApiProxy", Scope = PluginScope.Internal)]
	[Parameter("Port", typeof(int), "Port to listen on", "8001")]
	public class WebApiProxyPublisher : Publisher
	{
		public string Proxy { get; set; }

		private static readonly NLog.Logger ClassLogger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return ClassLogger; } }

		public RunContext Context { get; set; }
		public WebProxyStateModel Model { get; set; }
		public int Port { get; set; }

		private WebApiProxy _proxy;
		private readonly AutoResetEvent _iterationStarting = new AutoResetEvent(false);
		private readonly AutoResetEvent _iterationFinished = new AutoResetEvent(false);

		internal Action<SessionEventArgs, WebApiOperation> RequestEventPre { get; set; }
		internal Action<SessionEventArgs, WebApiOperation> RequestEventPost { get; set; }

		public WebApiProxyPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnStart()
		{
			base.OnStart();

			_proxy = new WebApiProxy
			{
				Options = Model.Options,
				Context = Context,
				IterationFinishedEvent = _iterationFinished,
				IterationStartingEvent = _iterationStarting,
				Port = Port
			};

			if (RequestEventPre != null && RequestEventPost != null)
			{
				_proxy.Start((s, e, a) => { RequestEventPre(e, a); }, (s, e, a) => { RequestEventPost(e, a); });
			}
			else if (RequestEventPre != null)
			{
				_proxy.Start((s, e, a) => { RequestEventPre(e, a); });
			}
			else if (RequestEventPost != null)
			{
				_proxy.Start(null, (s, e, a) => { RequestEventPost(e, a); });
			}
			else
			{
				_proxy.Start();
			}

			Port = _proxy.Port;
		}

		protected override void OnStop()
		{
			base.OnStop();

			_proxy.Dispose();
			_proxy = null;
		}

		protected override void OnOpen()
		{
			base.OnOpen();

			_iterationStarting.Set();
			Logger.Trace("OnOuput: Waiting on _iterationFinished");
			_iterationFinished.WaitOne();
		}

		protected override Variant OnCall(string method, List<ActionParameter> args)
		{
			// Could hack in sending COntext to WebApiProxy here. Hacky hack hack.

			return null;
		}
	}
}
