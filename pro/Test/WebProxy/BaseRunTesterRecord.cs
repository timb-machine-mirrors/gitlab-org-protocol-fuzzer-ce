using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Pro.Core;
using Peach.Pro.Core.Publishers;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.WebApi;
using Peach.Pro.Core.WebApi.Recorder;
using Peach.Pro.Test.WebProxy.TestTarget;
using Titanium.Web.Proxy.EventArguments;
using Encoding = System.Text.Encoding;

namespace Peach.Pro.Test.WebProxy
{
	public class BaseRunTesterRecord
	{
		public string BaseUrl { get { return "http://testhost"; } }

		public delegate void HookRequestEvent(SessionEventArgs e, RunContext context, WebApiOperation op);

		protected RunContext Context;
		protected IWebStatus Server;
		protected TempFile SwaggerFile;
		protected int Port;
		protected WebApiRecorder Recorder;

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
			return Recorder.Operations[0];
		}

		public string SerializeOperations(Dom dom = null)
		{
			if (dom == null)
				dom = OperationsToDom();

			var settings = new XmlWriterSettings
			{
				Encoding = Encoding.UTF8,
				Indent = true
			};

			using (var sout = new MemoryStream())
			{
				using (var xml = XmlWriter.Create(sout, settings))
				{
					xml.WriteStartDocument();
					dom.WritePit(xml);
					xml.WriteEndDocument();
				}

				return Encoding.UTF8.GetString(sout.ToArray());
			}
		}

		public Dom OperationsToDom()
		{
			var dom = new Peach.Core.Dom.Dom();
			WebApiToDom.Convert(dom, Recorder.Operations);

			return dom;
		}

		[SetUp]
		public virtual void SetUp()
		{
			var options = new WebProxyOptions();
			options.Routes.Add(new WebProxyRoute()
			{
				Url = "*", 
				BaseUrl = Server.Uri.ToString(),
				SwaggerAttr = SwaggerFile.Path
			});

			Recorder = new WebApiRecorder(Port, options);
			Recorder.Start();

			Port = Recorder.Port;
		}

		[TearDown]
		public void TearDown()
		{
			Recorder.Dispose();
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
