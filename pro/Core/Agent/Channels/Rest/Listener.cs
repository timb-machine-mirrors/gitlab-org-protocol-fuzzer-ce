//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.Linq;
using System.Threading;
using NLog;
using Peach.Core;
using Logger = NLog.Logger;
using HttpListener = SocketHttpListener.Net.HttpListener;
using HttpListenerContext = SocketHttpListener.Net.HttpListenerContext;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	internal class Listener : IDisposable
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private HttpListener _listener;
		private ManualResetEvent _event;

		public RouteHandler Routes { get; private set; }
		public Uri Uri { get; private set; }

		public static Listener Create(string prefix)
		{
			return new Listener(MakeListener(prefix));
		}

		public void Dispose()
		{
			if (_listener != null)
			{
				_listener.Close();
				_listener = null;
			}

			if (_event != null)
			{
				_event.Dispose();
				_event = null;
			}
		}

		public void Start()
		{
			_listener.OnContext = ProcessContext;

			_listener.Start();

			_event.WaitOne();
		}

		public void Stop()
		{
			_event.Set();
		}

		private Listener(HttpListener listener)
		{
			_listener = listener;
			_event = new ManualResetEvent(false);

			Uri = new Uri(_listener.Prefixes.First().Replace("+", Environment.MachineName));
			Routes = new RouteHandler();
		}

		private void ProcessContext(HttpListenerContext ctx)
		{
			Logger.Trace(">>> {0} {1}", ctx.Request.HttpMethod, ctx.Request.RawUrl);

			var response = Routes.Dispatch(ctx.Request);

			response.Complete(ctx);

			Logger.Trace("<<< {0} {1}", (int)response.StatusCode, response.StatusCode);
		}

		private static HttpListener MakeListener(string prefix)
		{
			// If the listener fails to start it is disposed so we
			// need to make a new one each time.
			var ret = new HttpListener(new LogHook(), "")
			{
				IgnoreWriteExceptions = true
			};

			ret.Prefixes.Add(prefix);

			try
			{
				ret.Start();

				return ret;
			}
			catch (System.Net.HttpListenerException ex)
			{
				throw new PeachException("An error occurred starting the HTTP listener.", ex);
			}

			// Because we are using SocketHttpListener we don't need to worry
			// about reserving special ports via netsh
		}

		private class LogHook : Patterns.Logging.ILogger
		{
			public void Info(string message, params object[] paramList)
			{
				Logger.Info(message, paramList);
			}

			public void Error(string message, params object[] paramList)
			{
				Logger.Error(message, paramList);
			}

			public void Warn(string message, params object[] paramList)
			{
				Logger.Warn(message, paramList);
			}

			public void Debug(string message, params object[] paramList)
			{
				Logger.Debug(message, paramList);
			}

			public void Fatal(string message, params object[] paramList)
			{
				Logger.Fatal(message, paramList);
			}

			public void FatalException(string message, Exception exception, params object[] paramList)
			{
				Logger.Fatal(exception, message, paramList);
			}

			public void ErrorException(string message, Exception exception, params object[] paramList)
			{
				Logger.Error(exception, message, paramList);
			}

			public void LogMultiline(string message, Patterns.Logging.LogSeverity severity, System.Text.StringBuilder additionalContent)
			{
				Log(severity, message);
			}

			public void Log(Patterns.Logging.LogSeverity severity, string message, params object[] paramList)
			{
				switch (severity)
				{
					case Patterns.Logging.LogSeverity.Debug:
						Debug(message, paramList);
						break;
					case Patterns.Logging.LogSeverity.Error:
						Error(message, paramList);
						break;
					case Patterns.Logging.LogSeverity.Fatal:
						Fatal(message, paramList);
						break;
					case Patterns.Logging.LogSeverity.Info:
						Info(message, paramList);
						break;
					case Patterns.Logging.LogSeverity.Warn:
						Warn(message, paramList);
						break;
				}
			}
		}
	}
}
