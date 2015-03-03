using System;
using System.IO;
using System.Net;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Publishers
{
	class SimpleHttpListener
	{
		volatile bool _stop;
		readonly HttpListener _listener = new HttpListener();
		readonly Thread _thread;
		int _delay;

		public SimpleHttpListener(params string[] prefixes)
		{
			Assert.True(HttpListener.IsSupported);
			foreach (var s in prefixes)
			{
				_listener.Prefixes.Add(s);
			}

			_thread = new Thread(OnThread);
		}

		public void Start(int delay)
		{
			_delay = delay;
			_thread.Start();
		}

		public void Stop()
		{
			_stop = true;
			_listener.Stop();
			_thread.Join();
		}

		private void OnThread()
		{
			Console.WriteLine("Delaying for {0}ms", _delay);
			Thread.Sleep(_delay);

			Console.WriteLine("Starting listener");
			_listener.Start();

			while (!_stop)
			{
				try
				{
					Console.WriteLine("Waiting for context");
					var context = _listener.GetContext();
					Console.WriteLine("Get context");

					var request = context.Request;

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
				}
				catch (Exception ex)
				{
					Console.WriteLine("Exception in listener loop: {0}", ex.Message);
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

		public void HttpClient(bool sendRecv, string method, int delay = 0)
		{
			var port = TestBase.MakePort(56000, 57000);
			var url = "http://localhost:{0}/".Fmt(port);
			var listener = new SimpleHttpListener(url);

			listener.Start(delay);

			try
			{
				var xml = string.Format(sendRecv ? SendRecvTemplate : RecvTemplate, method, url);

				var parser = new PitParser();
				var input = new MemoryStream(Encoding.ASCII.GetBytes(xml));
				var dom = parser.asParser(null, input);

				var config = new RunConfiguration {singleIteration = true};

				var e = new Engine(this);
				e.startFuzzing(dom, config);

				if (sendRecv)
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
				else
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
				listener.Stop();
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
			HttpClient(false, "GET");
			HttpClient(false, "POST");
		}

		[Test]
		public void TestReconnect()
		{
			HttpClient(true, "POST", 10000);
		}
	}
}
