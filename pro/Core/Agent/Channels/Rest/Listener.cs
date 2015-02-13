using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading;
using NLog;
using Peach.Core;
using Logger = NLog.Logger;

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
			while (true)
			{
				var ar = _listener.BeginGetContext(null, null);
				var idx = WaitHandle.WaitAny(new[] { _event, ar.AsyncWaitHandle });

				if (idx == 0)
					break;

				var ctx = _listener.EndGetContext(ar);

				ProcessContext(ctx);
			}
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

			ctx.Response.StatusCode = (int)response.StatusCode;

			// On mono, reset the connection reuse counter
			ctx.ResetReuses();

			if (response.Content == null)
			{
				ctx.Response.ContentLength64 = 0;
				ctx.Response.OutputStream.Close();
			}
			else
			{
				// Leave the stream at the position it was given to us at
				// so we can return data starting at an offset.

				ctx.Response.ContentType = response.ContentType;
				ctx.Response.ContentLength64 = response.Content.Length - response.Content.Position;

				using (var stream = ctx.Response.OutputStream)
				{
					response.Content.CopyTo(stream);
				}
			}

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
				var ret = new HttpListener();
				ret.Prefixes.Add(prefix);

				try
				{
					ret.Start();

					return ret;
				}
				catch (HttpListenerException ex)
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
	}
}
