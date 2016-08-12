using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core;
using Peach.Pro.Core.Publishers;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.WebApi;
using Peach.Pro.Test.WebProxy.TestTarget;
using Titanium.Web.Proxy.EventArguments;

namespace Peach.Pro.Test.WebProxy
{
	public class BaseRunTester
	{
		public string BaseUrl { get { return "http://testhost"; } }

		public delegate void HookRequestEvent(SessionEventArgs e, RunContext context, WebApiOperation op);

		protected RunContext Context;
		protected IWebStatus Server;
		protected TempFile SwaggerFile;
		protected Task Engine;
		protected int Port;
		protected List<WebApiOperation> Ops = new List<WebApiOperation>();

		/// <summary>
		/// Get an instance of HTTP Client
		/// </summary>
		/// <returns></returns>
		public HttpClient GetHttpClient()
		{
			var cookies = new CookieContainer();
			var handler = new HttpClientHandler
			{
				CookieContainer = cookies,
				UseCookies = true,
				UseDefaultCredentials = false,
				Proxy = new System.Net.WebProxy("http://127.0.0.1:" + Port, false, new string[] { }),
				UseProxy = true,
			};

			return new HttpClient(handler);
		}

		public static string GetValuesJson()
		{
			return Utilities.LoadStringResource(
				Assembly.GetExecutingAssembly(),
				"Peach.Pro.Test.WebProxy.TestTarget.SwaggerValuesApi.json");
		}

		public WebApiOperation GetOp()
		{
			return Ops.FirstOrDefault();
		}

		public void RunEngine(string xml, HookRequestEvent hookRequestEventPre = null, HookRequestEvent hookRequestEventPost = null)
		{
			var dom = new ProPitParser().asParser(null, new StringReader(xml));
			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			Ops.Clear();

			e.TestStarting += ctx =>
			{
				Context = ctx;

				var pub = (WebApiProxyPublisher)ctx.test.publishers[0];
				var stateModel = (WebProxyStateModel)ctx.test.stateModel;

				stateModel.RequestEventPre += (eventArgs, op) =>
				{
					Ops.Add(op);

					if (hookRequestEventPre != null)
						hookRequestEventPre(eventArgs, ctx, op);
					
				};

				if (hookRequestEventPost != null)
				{
					stateModel.RequestEventPost += (eventArgs, op) => hookRequestEventPost(eventArgs, ctx, op);
				}

				ctx.StateModelStarting += (c, sm) =>
				{
					Port = pub.Port;

					Monitor.Pulse(e);
					Monitor.Exit(e);
				};
			};

			Exception engineException = null;

			lock (e)
			{
				Engine = Task.Run(() =>
				{
					Monitor.Enter(e);

					try
					{
						e.startFuzzing(dom, cfg);
					}
					catch(Exception ex)
					{
						if (Monitor.IsEntered(e))
							Monitor.Pulse(e);
						engineException = ex;
					}
					finally
					{
						if (Monitor.IsEntered(e))
							Monitor.Exit(e);
					}
				});

				Monitor.Wait(e);
			}

			if (engineException != null)
				throw new ApplicationException("Engine exception", engineException); 
		}

		[SetUp]
		public virtual void SetUp()
		{
			var xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<Test name='Default' maxOutputSize='65000'>
		<WebProxy>
			<Route
				url='*'
				swagger='{0}'
				baseUrl='{1}'
			/> 
		</WebProxy>
		<Strategy class='WebProxy' />
		<Publisher class='WebApiProxy'>
			<Param name='Port' value='0' />
		</Publisher>
	</Test>
</Peach>".Fmt(SwaggerFile, Server.Uri);//"http://127.0.0.1:8003");// Server.Uri);

			RunEngine(xml);
		}

		[TearDown]
		public void TearDown()
		{
			Assert.IsTrue(Engine.Wait(10000));			
		}

		[OneTimeSetUp]
		public virtual void Init()
		{
			BaseProgram.Initialize();

			Server = TestTargetServer.StartServer();

			SwaggerFile = new TempFile(GetValuesJson());
		}

		[OneTimeTearDown]
		public virtual void Cleanup()
		{
			if (Server != null)
			{
				Server.Dispose();
				Server = null;
			}

			if (SwaggerFile != null)
			{
				SwaggerFile.Dispose();
				SwaggerFile = null;
			}
		}
	}
}
