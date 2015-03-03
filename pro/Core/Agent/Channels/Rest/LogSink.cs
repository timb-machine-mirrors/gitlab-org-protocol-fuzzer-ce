using NLog;
using System;
using WebSocketSharp;
using Newtonsoft.Json;
using Peach.Core;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	class LogSink : IDisposable
	{
		private readonly string _name;
		private readonly Uri _baseUri;
		private readonly NLog.Logger _logger;
		private readonly NLog.Logger _chain;
		private WebSocket _ws;
		
		public LogSink(string name, Uri baseUri)
		{
			_name = name;
			_baseUri = baseUri;
			_logger = LogManager.GetCurrentClassLogger();
			_chain = LogManager.GetLogger("Agent." + name);
		}
		
		public void Start()
		{
			Dispose();

			var url = "ws://{0}:{1}{2}".Fmt(_baseUri.Host, _baseUri.Port, Server.LogPath);
			_ws = new WebSocket(url, "log");
			_ws.OnOpen += OnOpen;
			_ws.OnMessage += OnMessage;
			_ws.OnError += OnError;
			_ws.OnClose += OnClose;
			_ws.Connect();
		}

		void OnOpen(object sender, EventArgs e)
		{
			_logger.Trace("OnOpen>");
		}

		void OnClose(object sender, CloseEventArgs e)
		{
			_logger.Trace("OnClose> WasClean: {0}, Code: {1}", e.WasClean, e.Code);
			Dispose();
		}

		void OnError(object sender, ErrorEventArgs e)
		{
			_logger.Error("OnError> {0}", e.Message);
		}

		void OnMessage(object sender, MessageEventArgs e)
		{
			try
			{
				var logEvent = JsonConvert.DeserializeObject<LogEventInfo>(e.Data);
				logEvent.LoggerName = "[{0}] {1}".Fmt(_name, logEvent.LoggerName);
				logEvent.Properties.Add("PreventLoop", true);
				_chain.Log(logEvent);
			}
			catch (Exception ex)
			{
				_logger.Error("OnMessage> Error: Could not deserialize logEvent: {0}", ex.Message);
			}
		}
	
		public void Dispose()
		{
			if (_ws != null)
			{
				_ws.Dispose();
				_ws = null;
			}
		}
	}
}
