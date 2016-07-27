using System.Collections.Generic;
using NLog;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Pro.Core.WebApi.Proxy;

namespace Peach.Pro.Core.Publishers
{
	/// <summary>
	/// This publisher exists only to pass configuration parameters
	/// to the web api proxy. And perhaps to start it.
	/// </summary>
	[Publisher("WebApiProxy", Scope = PluginScope.Beta)]
	[Parameter("FailureStatusCodes", typeof(int[]), "Comma separated list of status codes that are failures causing current test case to stop.", "400,401,402,403,404,405,406,407,408,409,410,411,412,413,414,415,416,417,500,501,502,503,504,505")]
	[Parameter("FaultOnStatusCodes", typeof(int[]), "Comma separated list of status codes that are faults. Defaults to none.", "")]
	[Parameter("Proxy", typeof(string), "Use an HTTP proxy in the format of 'http://192.168.1.1:8080'. Default none.", "")]
	[Parameter("IgnoreCertErrors", typeof(bool), "Allow https regardless of cert status (defaults to true)", "true")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	public class WebApiProxyPublisher : Publisher
	{
		public int[] FaultOnStatusCodes { get; protected set; }
		public int[] FailureStatusCodes { get; protected set; }
		public bool IgnoreCertErrors { get; protected set; }
		public int Timeout{ get; protected set; }
		public string Proxy{ get; protected set; }

		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		protected WebApiProxy _proxy = null;

		public WebApiProxyPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnOutput(BitwiseStream data)
		{
		}

		protected override Variant OnCall(string method, List<ActionParameter> args)
		{
			// Could hack in sending COntext to WebApiProxy here. Hacky hack hack.

			return null;
		}
	}
}
