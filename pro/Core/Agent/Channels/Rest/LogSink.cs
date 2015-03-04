using NLog;
using System;
using WebSocketSharp;
using Newtonsoft.Json;
using Peach.Core;
using System.Threading;
using System.Collections.Generic;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	class LogSink : IDisposable
	{
		private readonly string _name;
		private readonly Uri _baseUri;
		private readonly NLog.Logger _logger;
		private readonly NLog.Logger _chain;
		private AutoResetEvent _evtReady;
		private AutoResetEvent _evtClosed;
		private readonly Dictionary<long, LogEventInfo> _pending;
		private int _expectId;
		private WebSocket _ws;

		public LogSink(string name, Uri baseUri)
		{
			_name = name;
			_baseUri = baseUri;
			_logger = LogManager.GetCurrentClassLogger();
			_chain = LogManager.GetLogger("Agent." + name);
			_pending = new Dictionary<long, LogEventInfo>();
		}

		public void Start()
		{
			_logger.Trace("Start>");

			Stop();

			_expectId = 0;
			_evtReady = new AutoResetEvent(false);
			_evtClosed = new AutoResetEvent(false);

			var url = "ws://{0}:{1}{2}?level={3}".Fmt(
				_baseUri.Host,
				_baseUri.Port,
				Server.LogPath,
				Utilities.LogLevel);

			_ws = new WebSocket(url, "log");

			_ws.OnOpen += OnOpen;
			_ws.OnMessage += OnMessage;
			_ws.OnError += OnError;
			_ws.OnClose += OnClose;

			//_ws.Compression = CompressionMethod.DEFLATE;
			_ws.Connect();

			if (!_evtReady.WaitOne(TimeSpan.FromSeconds(10)))
				throw new SoftException("Timeout waiting for remote logging service to start");
		}

		public void Stop()
		{
			_logger.Trace("Stop>");

			if (_ws != null)
			{
				using (var evtFlushed = new AutoResetEvent(false))
				{
					_ws.Send("Flush", (done) => evtFlushed.Set());
					evtFlushed.WaitOne();
				}

				_ws.Close();
	
				if (!_evtClosed.WaitOne(TimeSpan.FromSeconds(10)))
					_logger.Warn("Timeout waiting for remote logging service to stop");

				Dispose();
			}
		}

		void OnOpen(object sender, EventArgs e)
		{
			_logger.Trace("OnOpen>");
			_evtReady.Set();
		}

		void OnClose(object sender, CloseEventArgs e)
		{
			_logger.Trace("OnClose> WasClean: {0}, Code: {1}", e.WasClean, e.Code);
			_evtClosed.Set();
		}

		void OnError(object sender, ErrorEventArgs e)
		{
			_logger.Error("OnError> {0}", e.Message);
			_evtReady.Set();
		}

		void OnMessage(object sender, MessageEventArgs e)
		{
			try
			{
				var logEvent = JsonConvert.DeserializeObject<LogEventInfo>(e.Data);
				var id = Convert.ToInt64(logEvent.Properties["ID"]);
				_pending.Add(id, logEvent);
				ProcessPending();
			}
			catch (Exception ex)
			{
				_logger.Error("OnMessage> Error: Could not deserialize logEvent: {0}", ex.Message);
			}
		}

		void ProcessPending()
		{
			while (true)
			{
				LogEventInfo logEvent;
				if (!_pending.TryGetValue(_expectId, out logEvent))
					break;

				_pending.Remove(_expectId);
				logEvent.LoggerName = "[{0}] {1}".Fmt(_name, logEvent.LoggerName);
				logEvent.Properties.Add("PreventLoop", true);
				_chain.Log(logEvent);
				_expectId++;
			}
		}

		public void Dispose()
		{
			if (_ws != null)
			{
				_ws.Dispose();
				_ws = null;
			}
			
			if (_evtReady != null)
			{
				_evtReady.Dispose();
				_evtReady = null;
			}
	
			if (_evtClosed != null)
			{
				_evtClosed.Dispose();
				_evtClosed = null;
			}

			_pending.Clear();
		}
	}
}
