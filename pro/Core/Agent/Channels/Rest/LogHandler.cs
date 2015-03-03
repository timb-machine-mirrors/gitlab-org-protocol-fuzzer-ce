using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using SocketHttpListener;
using SocketHttpListener.Net;
using System;
using System.Collections.Generic;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	class LogHandler : IDisposable
	{
		private readonly List<LogResponse> _responses = new List<LogResponse>();
		private readonly LogTarget _target;
		private const string Name = "RestLogTarget";

		public LogHandler(RouteHandler routes)
		{
			_target = new LogTarget();

			var config = LogManager.Configuration;
			var rule = new LoggingRule("*", LogLevel.Trace, _target);

			config.AddTarget(Name, _target);
			config.LoggingRules.Add(rule);
			LogManager.ReconfigExistingLoggers();

			routes.Add(Server.LogPath, "GET", OnSubscribe);
		}

		public void Dispose()
		{
			LogManager.Configuration.RemoveTarget(Name);

			foreach (var resp in _responses)
			{
				resp.Dispose();
			}
			_responses.Clear();
		}

		private RouteResponse OnSubscribe(HttpListenerRequest req)
		{
			var response = new LogResponse(_target);
			_responses.Add(response);
			return response;
		}
	}

	class LogResponse : RouteResponse, IDisposable
	{
		private WebSocket _ws;
		private readonly LogTarget _target;

		public LogResponse(LogTarget target)
		{
			_target = target;
		}

		public override void Complete(HttpListenerContext ctx)
		{
			_ws = ctx.AcceptWebSocket("log").WebSocket;
			_ws.ConnectAsServer();
			_ws.OnClose += (s, e) =>
			{
				_target.RemoveSocket(_ws);
				_ws = null;
			};
			_target.AddSocket(_ws);

			LogManager.GetCurrentClassLogger().Trace("New WebSocket");
		}

		public void Dispose()
		{
			if (_ws != null)
			{
				_target.RemoveSocket(_ws);
				_ws.Close();
				_ws = null;
			}
		}
	}

	class LogTarget : Target
	{
		private readonly List<WebSocket> _sockets = new List<WebSocket>();

		public void AddSocket(WebSocket ws)
		{
			lock (this)
			{
				_sockets.Add(ws);
			}
		}

		public void RemoveSocket(WebSocket ws)
		{
			lock(this)
			{
				_sockets.Remove(ws);
			}
		}

		protected override void Write(LogEventInfo logEvent)
		{
			// prevent logging loops
			if (logEvent.Properties.ContainsKey("PreventLoop"))
				return;

			var data = JsonConvert.SerializeObject(logEvent);
			lock (this)
			{
				_sockets.ForEach(ws =>
				{
					try
					{
						ws.SendAsync(data, null);
					}
					catch (Exception ex)
					{
						Console.WriteLine("LogTarget.Write Exception: {0}", ex.Message);
					}
				});
			}
		}
	}
}
