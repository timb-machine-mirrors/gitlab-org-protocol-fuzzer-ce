
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json.Linq;
using NLog;
using Peach.Core;
using Peach.Core.IO;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using vtortola.WebSockets;

#pragma warning disable 4014

namespace Peach.Pro.Core.Publishers
{
	[Publisher("WebSocket")]
	[Description("WebSocket Publisher")]
	[Parameter("Port", typeof(int), "Port to listen for connections on", "8080")]
	[Parameter("Template", typeof(string), "Data template for publishing")]
	[Parameter("Publish", typeof(string), "How to publish data, base64 or url.", "base64")]
	[Parameter("DataToken", typeof(string), "Token to replace with data in template", "##DATA##")]
	[Parameter("Timeout", typeof(int), "Time in milliseconds to wait for client response", "60000")]
	public class WebSocketPublisher : Publisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		readonly WebSocketListener _socketServer;
		readonly BufferBlock<string> _msgQueue = new BufferBlock<string>();

		readonly AutoResetEvent _evaluated = new AutoResetEvent(false);
		readonly AutoResetEvent _msgReceived = new AutoResetEvent(false);

		private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

		public int Port { get; protected set; }
		public string Template { get; protected set; }
		public string Publish { get; protected set; }
		public string DataToken { get; protected set; }
		public int Timeout { get; protected set; }

		string _template = null;

		public WebSocketPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			_socketServer = new WebSocketListener(new IPEndPoint(IPAddress.Any, Port));
			var rfc6455 = new vtortola.WebSockets.Rfc6455.WebSocketFactoryRfc6455(_socketServer);
			_socketServer.Standards.RegisterStandard(rfc6455);

			_template = File.ReadAllText(Template);
		}

		static async Task AcceptWebSocketClientAsync(WebSocketListener server, CancellationToken token, 
			BufferBlock<string> queue,
			AutoResetEvent msgReceived, AutoResetEvent evaluated)
		{
			Task task = null;
			var cancelSource = new CancellationTokenSource();

			while (!token.IsCancellationRequested)
			{
				try
				{
					var ws = await server.AcceptWebSocketAsync(token).ConfigureAwait(false);
					if (ws == null) continue;

					if (task != null)
					{
						logger.Debug("New web socket connection. Closing down existing connection.");

						cancelSource.Cancel();
						//cancelSource = new CancellationTokenSource();
					}
					else
						logger.Debug("New web socket connection");

					task = Task.Run(() => HandleConnectionAsync(ws, cancelSource.Token, msgReceived, evaluated));
					Task.Run(() => HandleSendQueueAsync(ws, cancelSource.Token, queue));
				}
				catch (Exception aex)
				{
					logger.Debug("Error Accepting clients: {0}", aex.GetBaseException().Message);
				}
			}

			cancelSource.Cancel();
			logger.Debug("Server Stop accepting clients");
		}

		static async Task HandleConnectionAsync(WebSocket ws, CancellationToken cancellation, AutoResetEvent msgReceived, AutoResetEvent evaluated)
		{
			try
			{
				while (ws.IsConnected && !cancellation.IsCancellationRequested)
				{
					var msg = await ws.ReadStringAsync(cancellation).ConfigureAwait(false);
					if (msg != null)
					{
						logger.Debug("NewMessageReceived: " + msg);
						msgReceived.Set();

						var json = JObject.Parse(msg);
						if (((string)json["msg"]) == "Evaluation complete" || ((string)json["msg"]) == "Client ready")
							evaluated.Set();
					}
				}
			}
			catch (Exception aex)
			{
				logger.Debug("Error Handling connection: {0}", aex.GetBaseException().Message);
				try { ws.Close(); }
				catch { }
			}
			finally
			{
				ws.Dispose();
			}
		}

		static async Task HandleSendQueueAsync(WebSocket ws, CancellationToken cancellation, BufferBlock<string> queue)
		{
			try
			{
				while (ws.IsConnected && !cancellation.IsCancellationRequested)
				{
					var msg = await queue.ReceiveAsync(cancellation);
					if (msg == null) continue;

					logger.Debug("Dequeued and sending message");
					ws.WriteString(msg);
				}
			}
			catch (Exception aex)
			{
				logger.Debug("Error handling queue: {0}", aex.GetBaseException().Message);
				try { ws.Close(); }
				catch { }
			}
			finally
			{
				ws.Dispose();
			}
		}

		protected override void OnStart()
		{
			base.OnStart();

			_socketServer.Start();
			Task.Run(() => AcceptWebSocketClientAsync(_socketServer, _cancellation.Token, _msgQueue, _msgReceived, _evaluated));
		}

		protected override void OnStop()
		{
			base.OnStop();

			_cancellation.Cancel();
			_socketServer.Stop();
		}

		protected override void OnOutput(BitwiseStream data)
		{
			try
			{
				logger.Debug(">> OnOutput");
				logger.Debug("Waiting for evaluated or client ready msg");
				
				_evaluated.WaitOne(Timeout);
				_evaluated.Reset();

				_msgQueue.Post(BuildMessage(data));

				logger.Debug("<< OnOutput");
			}
			catch (Exception ex)
			{
				logger.Debug(ex.ToString());
				throw;
			}
		}

		protected string BuildTemplate(BitwiseStream data)
		{
			var value = Publish;

			if (Publish == "base64")
			{
				data.Seek(0, SeekOrigin.Begin);
				var buf = new BitReader(data).ReadBytes((int)data.Length);
				value = Convert.ToBase64String(buf);
			}

			return _template.Replace(DataToken, value);
		}

		protected string BuildMessage(BitwiseStream data)
		{
			var ret = new StringBuilder();
			var msg = new JObject();

			msg["type"] = "template";
			msg["content"] = BuildTemplate(data);

			ret.Append(msg.ToString(Newtonsoft.Json.Formatting.None));
			ret.Append("\n");

			// Compatability with older usage
			msg["type"] = "msg";
			msg["content"] = "evaluate";

			ret.Append(msg.ToString(Newtonsoft.Json.Formatting.None));
			ret.Append("\n");

			return ret.ToString();
		}
	}
}

#pragma warning restore 4014

// end
