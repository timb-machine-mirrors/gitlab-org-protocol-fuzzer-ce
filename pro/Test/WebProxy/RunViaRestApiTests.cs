using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Moq;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core;
using Peach.Pro.Core.License;
using Peach.Pro.Core.Publishers;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.WebApi;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Test.WebProxy.TestTarget;
using Monitor = System.Threading.Monitor;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	internal class RunViaRestApiTests
	{
		IWebStatus _server;
		TempDirectory _tmpDir;
		bool _oldAsync;
		string _oldLogRoot;
		InternalJobMonitor _monitor;
		string _proxyUri;

		private const string Pit = @"
<Peach>
	<Test name='Default' maxOutputSize='65000'>
		<WebProxy>
			<Route
				url='*' mutate='false'
				baseUrl='{0}'
			/> 
		</WebProxy>
		<Strategy class='WebProxy' />
		<Publisher class='WebApiProxy'>
			<Param name='Port' value='0' />
		</Publisher>
	</Test>
</Peach>";

		private const string Config = @"
{
	'OriginalPit': 'Rest.xml',
	'Config': [],
	'Agents': [],
	'Weights': []
}
";

		private class SetUpHelper : Peach.Core.Test.SetUpFixture
		{
			public void SetUp()
			{
				DoSetUp();
				EnableDebug();
			}
		}

		internal class TestSetUpEvt : BaseProxyEvent { }
		internal class TestTearDownEvt : BaseProxyEvent { }

		[OneTimeSetUp]
		public void SessionSetUp()
		{
			StartJob();

			// SessionSetUp event will return when the
			// proxy is running and ready to accept connections

			Assert.True(_monitor.ProxyEvent(new SessionStartProxyEvent()), "Session SetUp Event Failed!");

			Assert.IsNotNull(_proxyUri, "Proxy uri should not be null");

			// At this point:
			// 1) _server is our target running at _server.Uri
			// 2) Peach proxy is running at _proxyUri
			// 3) WebRoute will remap everything to _server.Uri
		}

		[OneTimeTearDown]
		public void SessionTearDown()
		{
			Assert.True(_monitor.ProxyEvent(new SessionStopProxyEvent()), "Session TearDown Event Failed!");

			StopJob();
		}

		[SetUp]
		public void SetUp()
		{
			Assert.True(_monitor.ProxyEvent(new TestSetUpEvt()), "SetUp Event Failed!");
		}

		[TearDown]
		public void TearDown()
		{
			Assert.True(_monitor.ProxyEvent(new TestTearDownEvt()), "TearDown Event Failed!");
		}

		[Test]
		[Repeat(10)]
		public void Test1()
		{
			Assert.True(_monitor.ProxyEvent(new TestProxyEvent { Name = "Test1" }), "Test Event Failed!");

			var client = GetHttpClient();
			var response1 = client.GetAsync("http://testhost/unknown/api/values/5").Result;
			Assert.NotNull(response1);
			var response2 = client.GetAsync("http://testhost/unknown/api/values/6").Result;
			Assert.NotNull(response2);
		}

		[Test]
		[Repeat(10)]
		public void Test2()
		{
			Assert.True(_monitor.ProxyEvent(new TestProxyEvent { Name = "Test2" }), "Test Event Failed!");

			var client = GetHttpClient();

			try
			{
				var content = new FormUrlEncodedContent(new[] 
				{
					new KeyValuePair<string, string>("value", "Foo Bar")
				});
				var response = client.PutAsync("http://testhost/unknown/api/values/5?filter=foo", content).Result;
				Assert.NotNull(response);
			}
			catch
			{
				// Eat errors
			}

			try
			{
				var content = new FormUrlEncodedContent(new[] 
				{
					new KeyValuePair<string, string>("value", "Foo Bar")
				});
				var response = client.PutAsync("http://testhost/unknown/api/values/6?filter=foo", content).Result;
				Assert.NotNull(response);
			}
			catch
			{
				// Eat errors
			}
		}

		private void StartJob()
		{
			BaseProgram.Initialize();

			new SetUpHelper().SetUp();

			_tmpDir = new TempDirectory();

			_oldAsync = Configuration.UseAsyncLogging;
			_oldLogRoot = Configuration.LogRoot;

			Configuration.UseAsyncLogging = false;
			Configuration.LogRoot = _tmpDir.Path;

			_server = TestTargetServer.StartServer();

			var license = new Mock<ILicense>();
			_monitor = new InternalJobMonitor(license.Object);

			_monitor.TestHook += e =>
			{
				e.TestStarting += ctx =>
				{
					ctx.StateModelStarting += (c, sm) =>
					{
						var port = ((WebApiProxyPublisher)c.test.publishers[0]).Port;
						_proxyUri = "http://127.0.0.1:" + port;
					};
				};
				e.TestFinished += ctx =>
				{
					Assert.IsNotNull(ctx);
					Assert.Greater(ctx.currentIteration, 0);
				};
			};

			File.WriteAllText(Path.Combine(_tmpDir.Path, "Rest.peach"), Config);
			File.WriteAllText(Path.Combine(_tmpDir.Path, "Rest.xml"), Pit.Fmt(_server.Uri));

			Assert.IsNull(_proxyUri, "Proxy uri should null");

			_monitor.Start(_tmpDir.Path, Path.Combine(_tmpDir.Path, "Rest.peach"), new JobRequest());
		}

		private void StopJob()
		{
			if (_server != null)
			{
				_server.Dispose();
				_server = null;
			}

			if (_monitor != null)
			{
				_monitor.Dispose();
				_tmpDir = null;
			}

			Configuration.UseAsyncLogging = _oldAsync;
			Configuration.LogRoot = _oldLogRoot;

			if (_tmpDir != null)
			{
				_tmpDir.Dispose();
				_tmpDir = null;
			}
		}

		public HttpClient GetHttpClient()
		{
			var cookies = new CookieContainer();
			var handler = new HttpClientHandler
			{
				CookieContainer = cookies,
				UseCookies = true,
				UseDefaultCredentials = false,
				Proxy = new System.Net.WebProxy(_proxyUri, false, new string[] { }),
				UseProxy = true,
			};

			return new HttpClient(handler);
		}

	}
}