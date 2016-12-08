using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core.WebApi.Proxy;
using Peach.Pro.Test.WebProxy.TestTarget;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;
using vtortola.WebSockets;
using Uri = System.Uri;
using WebSocketMessageType = vtortola.WebSockets.WebSocketMessageType;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	public class WebProxyRunningTests : BaseRunTester
	{
		[Test]
		public void TestOnRequest()
		{
			var client = GetHttpClient();
			var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		}

/*		[Test]
		public void TestFaulting()
		{
			const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<Test name=""Default"">
		<WebProxy>
			<Route url='*' />
		</WebProxy>
		<Strategy class='WebProxy' />
		<Publisher class='WebApiProxy' />
	</Test>
</Peach>";

			var task = Task.Run(() =>
			{
				try
				{
					RunEngine(xml, false, 2);
				}
				catch (Exception)
				{
					System.Diagnostics.Debugger.Break();
				}
			});

			Thread.Sleep(2000);

			var client = GetHttpClient();
			
			var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;
			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
			
			response = client.GetAsync(BaseUrl + "/unknown/api/errors/500").Result;
			Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);

			task.Wait();
		} */

		[Test]
		public void TestProxy()
		{
			using (var p = new ProxyServer())
			{
				p.AddEndPoint(new ExplicitProxyEndPoint(IPAddress.Loopback, 0));
				p.Start();

				var count = 0;

				p.BeforeRequest += (o, e) =>
				{
					var req = e.WebSession.Request;

					req.RequestUri = req.RequestUri.Rewrite(Server.Uri.ToString());

					++count;

					return Task.FromResult(0);
				};

				var xml = @"<?xml version='1.0' encoding='utf-8'?>
	<Peach>
		<Test name='Default' maxOutputSize='65000'>
			<WebProxy proxy='http://127.0.0.1:{0}'>
				<Route
					url='*' mutate='false'
				/> 
			</WebProxy>
			<Strategy class='WebProxy' />
			<Publisher class='WebApiProxy'>
				<Param name='Port' value='0' />
			</Publisher>
		</Test>
	</Peach>".Fmt(p.ProxyEndPoints[0].Port);

				RunEngine(xml);

				var client = GetHttpClient();
				var headers = client.DefaultRequestHeaders;
				headers.Add("X-Peachy", "Testing 1..2..3..");

				var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;
				Assert.NotNull(response);

				Assert.AreEqual(1, count);
			}
		}

		[Test]
		public void TestProxyWebSocketPassthrough()
		{
			var WsPort = Server.Uri.Port;
			Server.Dispose();


			var socketServer = new WebSocketListener(new IPEndPoint(IPAddress.Any, WsPort));
			var rfc6455 = new vtortola.WebSockets.Rfc6455.WebSocketFactoryRfc6455(socketServer);
			var msgText = string.Empty;
			var msgEvent = new AutoResetEvent(false);
			var readyEvent = new AutoResetEvent(false);

			try
			{
				var cancelToken = new CancellationTokenSource();
				socketServer.Standards.RegisterStandard(rfc6455);
				socketServer.Start();

				Task.Run(() =>
				{
					readyEvent.Set();

					var acceptTask = socketServer.AcceptWebSocketAsync(cancelToken.Token);
					acceptTask.Wait(cancelToken.Token);

					using (var client = acceptTask.Result)
					{
						var readStreamTask = client.ReadMessageAsync(cancelToken.Token);
						readStreamTask.Wait(cancelToken.Token);

						using (var messageReadStream = readStreamTask.Result)
						{
							if (messageReadStream.MessageType == WebSocketMessageType.Text)
							{
								//String msgContent = String.Empty;
								using (var sr = new StreamReader(messageReadStream, System.Text.Encoding.UTF8))
									msgText = sr.ReadToEnd();
							}
						}

						client.Close();
						msgEvent.Set();
					}
				});


				using (var p = new ProxyServer())
				{
					p.AddEndPoint(new ExplicitProxyEndPoint(IPAddress.Loopback, 0));
					p.Start();

					var count = 0;

					p.BeforeRequest += (o, e) =>
					{
						var req = e.WebSession.Request;

						req.RequestUri = req.RequestUri.Rewrite(Server.Uri.ToString());

						++count;

						return Task.FromResult(0);
					};

					var xml = @"<?xml version='1.0' encoding='utf-8'?>
	<Peach>
		<Test name='Default' maxOutputSize='65000'>
			<WebProxy proxy='http://127.0.0.1:{0}'>
				<Route
					url='*' mutate='false'
				/> 
			</WebProxy>
			<Strategy class='WebProxy' />
			<Publisher class='WebApiProxy'>
				<Param name='Port' value='0' />
			</Publisher>
		</Test>
	</Peach>".Fmt(p.ProxyEndPoints[0].Port);

					RunEngine(xml);

					readyEvent.WaitOne();

					using (var webSocket = new ClientWebSocket())
					{
						webSocket.Options.Proxy = new System.Net.WebProxy("http://127.0.0.1:" + Port, false, new string[] {});
						var connectTask = webSocket.ConnectAsync(
							new Uri(string.Format("ws://127.0.0.1:{0}", WsPort)), CancellationToken.None);
						connectTask.Wait();

						var buffer = System.Text.Encoding.UTF8.GetBytes("{\"op\":\"unconfirmed_sub\"}");
						var sendTask = webSocket.SendAsync(
							new ArraySegment<byte>(buffer),
							System.Net.WebSockets.WebSocketMessageType.Text, 
							true, 
							CancellationToken.None);

						sendTask.Wait();
					}

					msgEvent.WaitOne(10000);
					cancelToken.Cancel();

					Assert.AreEqual("{\"op\":\"unconfirmed_sub\"}", msgText);

					if (socketServer.IsStarted)
						socketServer.Stop();

					// Force engine to exit
					Server = TestTargetServer.StartServer();

					var client = GetHttpClient();
					var response = client.GetAsync(BaseUrl + "/unknown/api/values/5").Result;
					Assert.NotNull(response);
				}
			}
			catch (Exception)
			{

				throw;
			}
			finally
			{
				Server = null;
				
				if(socketServer.IsStarted)
					socketServer.Stop();

				socketServer.Dispose();
			}
		}
	}
}
