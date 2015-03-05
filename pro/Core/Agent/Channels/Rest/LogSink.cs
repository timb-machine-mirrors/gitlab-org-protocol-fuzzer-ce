using NLog;
using System;
using WebSocketSharp;
using Newtonsoft.Json;
using Peach.Core;
using System.Threading;
using System.Collections.Generic;
using System.Net;

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
		private AutoResetEvent _evtFlushed;
		private readonly SortedDictionary<long, LogEventInfo> _pending;
		private long _expectId;
		private WebSocket _ws;
		private bool _isClosed = false;

		public LogSink(string name, Uri baseUri)
		{
			_name = name;
			_baseUri = baseUri;
			_logger = LogManager.GetCurrentClassLogger();
			_chain = LogManager.GetLogger("Agent." + name);
			_pending = new SortedDictionary<long, LogEventInfo>();
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
			//_ws.Log.Level = WebSocketSharp.LogLevel.Debug;

			_ws.OnOpen += OnOpen;
			_ws.OnMessage += OnMessage;
			_ws.OnError += OnError;
			_ws.OnClose += OnClose;

			//_ws.Compression = CompressionMethod.DEFLATE;

			var timeout = 1;
			Retry.Execute(() =>
			{
				// This will prevent requiring users to add a TcpPortMonitor
				// to wait for remote agents to become available.
				// It will also prevent the log websocket from hanging
				// when it tries to connect too soon.

				var urlGet = "http://{0}:{1}{2}".Fmt(
					_baseUri.Host,
					_baseUri.Port,
					Server.LogPath);

				_logger.Trace("Attempting to GET '{0}' with timeout = {1}ms", urlGet, timeout);

				var req = (HttpWebRequest)WebRequest.Create(urlGet);
				req.Timeout = timeout;

				timeout *= 2;
				timeout = Math.Min(timeout, 1000);

				using (var resp = (HttpWebResponse)req.GetResponse())
				{
					resp.Close();

					if (resp.StatusCode != HttpStatusCode.OK)
						throw new PeachException("Logging service not available");
				}
			}, TimeSpan.Zero, 30);

			_ws.Connect();

			if (!_evtReady.WaitOne(TimeSpan.FromSeconds(10)))
				throw new SoftException("Timeout waiting for remote logging service to start");
		}

		public void Stop()
		{
			_logger.Trace("Stop>");

			if (_ws != null)
			{
				_evtFlushed = new AutoResetEvent(false);

				try
				{
					_ws.Send("Flush");
					if (!_evtFlushed.WaitOne(TimeSpan.FromSeconds(10)))
						_logger.Warn("Timeout waiting for remote logging service to flush");
				}
				finally
				{
					_evtFlushed.Dispose();
					_evtFlushed = null;
				}

				if (!_isClosed)
				{
					_ws.Close();

					if (!_evtClosed.WaitOne(TimeSpan.FromSeconds(10)))
						_logger.Warn("Timeout waiting for remote logging service to stop");

					_isClosed = true;
				}

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
			_logger.Trace("OnClose> WasClean: {0}, Code: {1}, Reason: {2}",
				e.WasClean,
				e.Code,
				e.Reason);
			_evtClosed.Set();
			if (_evtFlushed != null)
				_evtFlushed.Set();
		}

		void OnError(object sender, ErrorEventArgs e)
		{
			_logger.Debug("OnError> {0}", e.Message);
			_evtReady.Set();
			if (_evtFlushed != null)
				_evtFlushed.Set();
		}

		void OnMessage(object sender, MessageEventArgs e)
		{
			_logger.Trace("OnMessage>");
			try
			{
				var logEvent = JsonConvert.DeserializeObject<LogEventInfo>(e.Data);
				lock (this)
				{
					ProcessMessage(logEvent);
				}
			}
			catch (Exception ex)
			{
				_logger.Error("OnMessage> Error: Could not deserialize logEvent: {0}", ex.Message);
			}
		}

		void ProcessMessage(LogEventInfo logEvent)
		{
			var id = Convert.ToInt64(logEvent.Properties["ID"]);
			if (id == -1)
			{
				foreach (var kv in _pending)
				{
					ProcessEvent(kv.Value);
					_expectId = kv.Key + 1;
				}
				_pending.Clear();
				if (_evtFlushed != null)
					_evtFlushed.Set();
			}
			else
			{
				_pending.Add(id, logEvent);
				ProcessPending();
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
				ProcessEvent(logEvent);
				_expectId++;
			}
		}

		void ProcessEvent(LogEventInfo logEvent)
		{
			logEvent.LoggerName = "[{0}] {1}".Fmt(_name, logEvent.LoggerName);
			logEvent.Properties.Add("PreventLoop", true);
			_chain.Log(logEvent);
		}

		public void Dispose()
		{
			if (_ws != null)
			{
				if (!_isClosed)
				{
					_ws.Close();
					_isClosed = true;
				}
	
				_ws.OnOpen -= OnOpen;
				_ws.OnMessage -= OnMessage;
				_ws.OnError -= OnError;
				_ws.OnClose -= OnClose;

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
