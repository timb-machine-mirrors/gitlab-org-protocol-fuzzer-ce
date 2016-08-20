using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.WebApi;
using Encoding = System.Text.Encoding;

namespace Peach.Pro.Test.WebProxy
{
	[TestFixture]
	[Quick]
	[Peach]
	class WebProxyRouteSwaggerHttpTests
	{
		public class SimpleHttpListener : IDisposable
		{
			private readonly StringBuilder _sb = new StringBuilder();
			private readonly TcpListener _listener = new TcpListener(IPAddress.Loopback, 0);
			readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

			public int StatusCode { get; set; }
			public int FaultStatusCode { get; set; }
			public int FaultOnRequest { get; set; }

			private int _requestCount = 0;

			public SimpleHttpListener()
			{
				StatusCode = 200;
				FaultStatusCode = 500;
				FaultOnRequest = -1;

				_listener.Start();

				_listener.BeginAcceptTcpClient(OnAccept, _listener);
			}

			public void Dispose()
			{
				_listener.Stop();
			}

			public StringBuilder Request
			{
				get { return _sb; }
			}

			public string ResponseBody { get; set; }

			public int Port
			{
				get { return ((IPEndPoint)_listener.LocalEndpoint).Port; }
			}

			private void OnAccept(IAsyncResult ar)
			{
				var listener = (TcpListener)ar.AsyncState;

				try
				{
					var client = listener.EndAcceptTcpClient(ar);

					var strm = client.GetStream();

					var reader = new StreamReader(strm);

					const string lenHeader = "Content-Length: ";

					var contentLen = 0;

					while (true)
					{
						var line = reader.ReadLine();

						if (line == null)
							break;

						_sb.Append(line);
						_sb.Append("\r\n");

						if (line == string.Empty)
							break;

						if (line.StartsWith(lenHeader))
							contentLen = int.Parse(line.Substring(lenHeader.Length));
					}

					while (contentLen > 0)
					{
						var buf = new char[contentLen];
						var len = reader.Read(buf, 0, buf.Length);
						// FIXME: it would be more correct to read bytes instead of chars
						//        but these tests fail under windows if this is done.
						//var buf = new byte[contentLen];
						//var len = strm.Read(buf, 0, buf.Length);
						if (len == 0)
							break;

						_sb.Append(buf, 0, len);
						//_sb.Append(Encoding.UTF8.GetString(buf));
						contentLen -= len;
					}

					var writer = new StreamWriter(strm);

					if (_requestCount == FaultOnRequest)
						writer.Write("HTTP/1.0 {0} Internal Server Error\r\n", FaultStatusCode);
					else
						writer.Write("HTTP/1.0 {0} OK\r\n", StatusCode);

					writer.Write("Content-Type: text/xml; charset=utf-8\r\n");
					writer.Write("Content-Length: {0}\r\n", ResponseBody.Length);
					writer.Write("\r\n");
					writer.Write(ResponseBody);
					writer.Flush();

					_requestCount++;
					client.Client.Shutdown(SocketShutdown.Send);

					var rest = reader.ReadToEnd();
					_sb.Append(rest);

					client.Close();
				}
				catch (ObjectDisposedException)
				{
					// ignore these, stop got called while listener was waiting for a connection
				}
				catch (Exception ex)
				{
					// Closed when calling Accept
					_logger.Debug("Error in OnAccept: {0}".Fmt(ex));
				}
				finally
				{
					try
					{
						listener.BeginAcceptTcpClient(OnAccept, listener);
					}
					catch
					{
						// ignore errors if we can't start again
						// probably means we're shutting down from the Dispose()
					}
				}
			}
		}

		SimpleHttpListener _listener;
		NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

		[OneTimeSetUp]
		public virtual void Init()
		{
			BaseProgram.Initialize();
		}

		[SetUp]
		public void SetUp()
		{
			_listener = new SimpleHttpListener();
		}

		[TearDown]
		public void TearDown()
		{
			_listener.Dispose();
			_listener = null;
		}

		[Test]
		public void RouteSwaggerHttpUrlTest()
		{
				var xml = @"
<Peach>
	<Test name='Default'>
		<WebProxy>
			<Route url='/p/jobs' swagger='http://localhost:{0}/swagger.json' />
		</WebProxy>
		<Strategy class='WebProxy' />
		<Publisher class='WebApiProxy'>
			<Param name='Port' value='0' />
		</Publisher>
	</Test>
</Peach>
".Fmt(_listener.Port);

			_listener.ResponseBody = @"
{
    ""swagger"": ""2.0"",
    ""info"": {
        ""title"": ""XXX"",
        ""description"": ""XXX"",
        ""version"": ""1.0.0""
    },
    ""host"": ""localhost:8002"",
    ""schemes"": [
        ""http""
    ],
    ""basePath"": ""/api"",
    ""produces"": [
        ""application/json""
    ],
    ""paths"": {
        ""/values"": {
            ""get"": {
                ""operationId"": ""GetAllValues"",
                ""summary"": ""Values"",
                ""description"": ""Get all values"",
                ""responses"": {
                    ""200"": {
                        ""description"": ""Value"",
                        ""schema"": {
                            ""$ref"": ""#/definitions/EmptyObject""
                        }
                    }
                }
            }
        }
    },
    ""definitions"": {
        ""EmptyObject"": {
            ""type"": ""object""
        }
    }
}
";
			var dom = new ProPitParser().asParser(null, new StringReader(xml));
			var webProxy = (WebProxyStateModel)dom.tests[0].stateModel;
			var swagger = webProxy.Options.Routes[0].Swagger;

			Assert.NotNull(_listener.ResponseBody, swagger);

		}
	}
}
