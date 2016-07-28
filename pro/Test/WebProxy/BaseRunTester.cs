using System.Net;
using System.Net.Http;
using NUnit.Framework;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Test.WebProxy.TestTarget;

namespace Peach.Pro.Test.WebProxy
{
	public class BaseRunTester// : Watcher
	{
		public string BaseUrl { get { return "http://testhost/"; } }

		protected IWebStatus _server;

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
				Proxy = new System.Net.WebProxy("http://127.0.0.1:8001", false, new string[] { }),
				UseProxy = true,
			};

			return new HttpClient(handler);
		}

		[OneTimeSetUp]
		public virtual void Init()
		{
			BaseProgram.Initialize();

			_server = TestTargetServer.StartServer();
		}

		[OneTimeTearDown]
		public virtual void Cleanup()
		{
			if (_server != null)
				_server.Dispose();

			_server = null;
		}
	}
}
