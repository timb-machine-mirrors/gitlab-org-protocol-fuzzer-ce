//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
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

		private const string EveryoneAccount = "Everyone";
		private const int AccessDenied = 5;

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

			//while (true)
			//{
			//	var ar = _listener.BeginGetContext(null, null);
			//	var idx = WaitHandle.WaitAny(new[] { _event, ar.AsyncWaitHandle });

			//	if (idx == 0)
			//		break;

			//	var ctx = _listener.EndGetContext(ar);

			//	ProcessContext(ctx);
			//}
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

		private static string GetAccountName()
		{
			try
			{
				var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
				var account = sid.Translate(typeof(NTAccount)) as NTAccount;
				if (account != null)
					return account.Value;

				return EveryoneAccount;
			}
			catch (Exception)
			{
				return EveryoneAccount;
			}
		}

		private static HttpListener MakeListener(string prefix)
		{
			for (var reserved = false; ; reserved = true)
			{
				// If the listener fails to start it is disposed so we
				// need to make a new one each time.
				var ret = new HttpListener(new LogHook(), "");
				ret.Prefixes.Add(prefix);

				try
				{
					ret.Start();

					return ret;
				}
				catch (System.Net.HttpListenerException ex)
				{
					if (reserved || ex.ErrorCode != AccessDenied)
						throw new PeachException("An error occurred starting the HTTP listener.", ex);
				}

				// Ensure we are allowed to listen for http connections
				using (var p = new Process())
				{
					var user = GetAccountName();

					p.StartInfo = new ProcessStartInfo
					{
						Verb = "runas",
						FileName = "netsh",
						Arguments = "http add urlacl url={0} user={1}".Fmt(prefix, user),
						UseShellExecute = true,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden,
					};

					p.Start();

					p.WaitForExit();

					if (p.ExitCode != 0)
						throw new PeachException("An error occurred reserving the prefix '{0}'.".Fmt(prefix));
				}
			}
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
				Logger.FatalException(message.Fmt(paramList), exception);
			}

			public void ErrorException(string message, Exception exception, params object[] paramList)
			{
				Logger.ErrorException(message.Fmt(paramList), exception);
			}

			public void LogMultiline(string message, Patterns.Logging.LogSeverity severity, System.Text.StringBuilder additionalContent)
			{
				Log(severity, message, new object[0]);
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
