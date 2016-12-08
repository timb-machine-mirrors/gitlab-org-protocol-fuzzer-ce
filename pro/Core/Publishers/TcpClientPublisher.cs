

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("Tcp")]
	[Alias("TcpClient")]
	[Alias("tcp.Tcp")]
	[Parameter("Host", typeof(string), "Hostname or IP address of remote host")]
	[Parameter("Port", typeof(ushort), "Remote port to connect to")]
	[Parameter("Lifetime", typeof(Test.Lifetime), "Lifetime of connection (Iteration, Session)", "Iteration")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait when receiving data (default 3000)", "3000")]
	[Parameter("SendTimeout", typeof(int), "How many milliseconds to wait when sending data (default infinite)", "-1")]
	[Parameter("ConnectTimeout", typeof(int), "Max milliseconds to wait for connection (default 10000)", "10000")]
	public class TcpClientPublisher : TcpPublisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public string Host { get; protected set; }
		public int ConnectTimeout { get; protected set; }
		public Test.Lifetime Lifetime { get; protected set; }

		public TcpClientPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected void Connect()
		{
			var timeout = ConnectTimeout;
			var sw = new Stopwatch();

			for (int i = 1; _tcp == null; i *= 2)
			{
				try
				{
					// Must build a new client object after every failed attempt to connect.
					// For some reason, just calling BeginConnect again does not work on mono.
					_tcp = new TcpClient();

					sw.Restart();

					var ar = _tcp.BeginConnect(Host, Port, null, null);
					if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeout)))
						throw new TimeoutException("Timed out connecting to remote host {0} port {1}.".Fmt(Host, Port));
					_tcp.EndConnect(ar);
				}
				catch (Exception ex)
				{
					sw.Stop();

					if (_tcp != null)
					{
						_tcp.Close();
						_tcp = null;
					}

					timeout -= (int)sw.ElapsedMilliseconds;

					if (timeout > 0)
					{
						int waitTime = Math.Min(timeout, i);
						timeout -= waitTime;

						Logger.Trace("Unable to connect to remote host {0} on port {1}.  Trying again in {2}ms...", Host, Port, waitTime);
						Thread.Sleep(waitTime);
					}
					else
					{
						Logger.Debug("Unable to connect to remote host {0} on port {1}.", Host, Port);
						throw new SoftException(ex);
					}
				}
			}

			StartClient();
		}

		protected override void OnStart()
		{
			base.OnStart();

			if (Lifetime == Test.Lifetime.Session)
				Connect();
		}

		protected override void OnStop()
		{
			if (Lifetime == Test.Lifetime.Session)
				base.OnClose();

			base.OnStop();
		}

		protected override void OnOpen()
		{
			// Complete socket shutdown if CloseClient was called
			// but not OnClose.
			// Note: CloseClient can happen after OnClose is called
			//  so this code is required in both OnOpen and OnClose.
			lock (_clientLock)
			{
				if (_client == null && _buffer != null)
					base.OnClose();
			}

			if (Lifetime == Test.Lifetime.Iteration || _tcp == null ||  _tcp.Connected == false)
			{
				base.OnOpen();
				Connect();
			}
		}

		protected override void OnClose()
		{
			if (Lifetime == Test.Lifetime.Iteration)
				base.OnClose();

			// _client will be null if CloseClient was called
			// so we need to complete the shutdown
			lock (_clientLock)
			{
				if (_client == null)
					base.OnClose();
			}
		}
	}
}
