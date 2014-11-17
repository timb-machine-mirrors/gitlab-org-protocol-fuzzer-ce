using System;
using System.IO;
using System.Net;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;

namespace Peach.Pro.Test.Publishers
{

	class SimpleHttpListener
	{
		bool _stop;

		public SimpleHttpListener()
		{
			Assert.True(HttpListener.IsSupported);
		}

		HttpListener _listener;

		public void Stop()
		{
			_stop = true;

			try
			{
				_listener.Stop();
			}
			catch
			{
			}
		}

		// This example requires the System and System.Net namespaces. 
		public void Listen(string[] prefixes)
		{
			// URI prefixes are required, 
			// for example "http://localhost:8080/index/".
			if (prefixes == null || prefixes.Length == 0)
				throw new ArgumentException("prefixes");

			// Create a listener.
			_listener = new HttpListener();
			// Add the prefixes. 
			foreach (var s in prefixes)
			{
				_listener.Prefixes.Add(s);
			}
			_listener.Start();

			IAsyncResult ar = null;

			while (!_stop)
			{
				try
				{
					if (ar == null)
						ar = _listener.BeginGetContext(null, null);

					if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1)))
						continue;

					// Note: The GetContext method blocks while waiting for a request. 
					var context = _listener.EndGetContext(ar);
					var request = context.Request;
					// Obtain a response object.

					if (request.ContentLength64 > 0)
					{
						var buf = new byte[request.ContentLength64];
						request.InputStream.Read(buf, 0, buf.Length);
					}

					var response = context.Response;

					// Construct a response. 
					var responseString = request.HttpMethod + " Hello World Too = " + request.ContentLength64;
					var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
					// Get a response stream and write the response to it.
					response.ContentLength64 = buffer.Length;
					var output = response.OutputStream;
					output.Write(buffer, 0, buffer.Length);
					// You must close the output stream.
					output.Close();
					response.Close();

					ar = null;
				}
				catch
				{
					return;
				}

			}
		}
	}

	[TestFixture]
	[Category("Peach")]
	public class HttpPublisherTests : DataModelCollector
	{
		private const string SendRecvTemplate = @"
<Peach>
	<DataModel name=""TheDataModel"">
		<String name=""str"" value=""Hello World""/>
	</DataModel>

	<DataModel name=""TheDataModel2"">
		<String name=""str""/>
	</DataModel>

	<StateModel name=""ClientState"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Send"" type=""output"">
				<DataModel ref=""TheDataModel""/>
			</Action>
			<Action name=""Recv"" type=""input"">
				<DataModel ref=""TheDataModel2""/>
			</Action>
		</State>
	</StateModel>

<Test name=""Default"">
		<StateModel ref=""ClientState""/>
		<Publisher class=""Http"">
			<Param name=""Method"" value=""{0}""/>
			<Param name=""Url"" value=""{1}""/>
			<Param name=""IgnoreCertErrors"" value=""true""/>
		</Publisher>
	</Test>

</Peach>
";

		private const string RecvTemplate = @"
<Peach>
	<DataModel name=""TheDataModel"">
		<String name=""str""/>
	</DataModel>

	<StateModel name=""ClientState"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Recv"" type=""input"">
				<DataModel ref=""TheDataModel""/>
			</Action>
		</State>
	</StateModel>

<Test name=""Default"">
		<StateModel ref=""ClientState""/>
		<Publisher class=""Http"">
			<Param name=""Method"" value=""{0}""/>
			<Param name=""Url"" value=""{1}""/>
			<Param name=""IgnoreCertErrors"" value=""true""/>
		</Publisher>
	</Test>

</Peach>
";

		public void HttpClient(bool sendRecv, string method)
		{
			HttpClient(sendRecv, method, false);
		}

		public void HttpClient(bool sendRecv, string method, bool isHttps)
		{
			var port = TestBase.MakePort(56000, 57000);
			string url;
			SimpleHttpListener listener = null;
			Thread lThread = null;
			if (isHttps)
			{
				url = "https://changethisurltotest.peach";
			}
			else
			{
				url = "http://localhost:" + port + "/";

				listener = new SimpleHttpListener();
				var prefixes = new string[1] { url };
				lThread = new Thread(() => listener.Listen(prefixes));

				lThread.Start();
			}

			try
			{
				var xml = string.Format(sendRecv ? SendRecvTemplate : RecvTemplate, method, url);

				var parser = new PitParser();
				var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

				var config = new RunConfiguration {singleIteration = true};

				var e = new Engine(this);
				e.startFuzzing(dom, config);

				if (sendRecv && !isHttps)
				{
					Assert.AreEqual(2, actions.Count);

					var de1 = actions[0].dataModel.find("TheDataModel.str");
					Assert.NotNull(de1);
					var de2 = actions[1].dataModel.find("TheDataModel2.str");
					Assert.NotNull(de2);

					var send = (string)de1.DefaultValue;
					var recv = (string)de2.DefaultValue;

					Assert.AreEqual("Hello World", send);
					Assert.AreEqual(method + " Hello World Too = 11", recv);
				}
				else if (!isHttps)
				{
					Assert.AreEqual(1, actions.Count);
					var de1 = actions[0].dataModel.find("TheDataModel.str");
					Assert.NotNull(de1);

					var recv = (string)de1.DefaultValue;

					Assert.AreEqual(method + " Hello World Too = 0", recv);
				}
			}
			finally
			{
				if (!isHttps)
				{
					listener.Stop();
					lThread.Join();
				}
			}
		}

		[Test, ExpectedException(typeof(PeachException))]
		public void HttpClientSendGet()
		{
			// Http publisher does not support sending data when the GET method is used
			HttpClient(true, "GET");
		}

		[Test]
		public void HttpClientRecvGet()
		{
			// Http publisher does not support sending data when the GET method is used
			HttpClient(false, "GET");
		}

		[Test]
		public void HttpClientSendPost()
		{
			HttpClient(true, "POST");
		}

		[Test]
		public void HttpClientRecvPost()
		{
			HttpClient(false, "POST");
		}

		[Test, Ignore]
		public void AreCertErrorsIgnored()
		{
			// need to set url above to something that has a self signed cert for https
			HttpClient(false, "GET", true);
			HttpClient(false, "POST", true);
		}
	}
}
