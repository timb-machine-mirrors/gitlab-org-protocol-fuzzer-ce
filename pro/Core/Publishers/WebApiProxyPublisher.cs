using System.Collections.Generic;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Pro.Core.WebApi;
using Peach.Pro.Core.WebApi.Proxy;

namespace Peach.Pro.Core.Publishers
{
	/// <summary>
	/// This publisher exists only to pass configuration parameters
	/// to the web api proxy. And perhaps to start it.
	/// </summary>
	[Publisher("WebApiProxy", Scope = PluginScope.Internal)]
	[Parameter("Proxy", typeof(string), "Use an HTTP proxy in the format of 'http://192.168.1.1:8080'. Default none.", "")]
	[Parameter("IgnoreCertErrors", typeof(bool), "Allow https regardless of cert status (defaults to true)", "true")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	public class WebApiProxyPublisher : Publisher
	{
		public bool IgnoreCertErrors { get; protected set; }
		public int Timeout{ get; protected set; }
		public string Proxy{ get; protected set; }

		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public RunContext Context { get; set; }
		public WebProxyStateModel Model { get; set; }

		private WebApiProxy _proxy = null;
		private AutoResetEvent _iterationStarting = new AutoResetEvent(false);
		private AutoResetEvent _iterationFinished = new AutoResetEvent(false);

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
				IterationStartingEvent = _iterationStarting
			};

			_proxy.Start();
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
			logger.Trace("OnOuput: Waiting on _iterationFinished");
			_iterationFinished.WaitOne();
		}

		protected override Variant OnCall(string method, List<ActionParameter> args)
		{
			// Could hack in sending COntext to WebApiProxy here. Hacky hack hack.

			return null;
		}
	}
}
