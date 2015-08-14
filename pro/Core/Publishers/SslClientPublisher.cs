using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NLog;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using Peach.Core;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("Ssl")]
	[Parameter("Host", typeof(string), "Hostname to connect to")]
	[Parameter("Port", typeof(ushort), "Port to connect to")]
	[Parameter("VerifyServer", typeof(bool), "Verify the server certificate", "false")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data (default 3000)", "3000")]
	[Parameter("ConnectTimeout", typeof(int), "Max milliseconds to wait for connection (default 10000)", "10000")]
	[Parameter("Sni", typeof(string), "Sni to use for SSL connection. Will use Host by default", "")]
	public class SslClientPublisher : Peach.Core.Publishers.BufferedStreamPublisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		#region BouncyCastle TLS Helper Classes

		// Need class with TlsClient in inheritance chain
		class MyTlsClient : DefaultTlsClient
		{
			public override TlsAuthentication GetAuthentication()
			{
				return new MyTlsAuthentication();
			}
		}

		// Need class to handle certificate auth
		class MyTlsAuthentication : TlsAuthentication
		{
			public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
			{
				// return client certificate
				return null;
			}

			public void NotifyServerCertificate(Certificate serverCertificate)
			{
				// validate server certificate
			}
		}

		#endregion

		public bool VerifyServer { get; protected set; }
		public string Host { get; protected set; }
		public string Sni { get; protected set; }
		public int ConnectTimeout { get; protected set; }
		public ushort Port { get; protected set; }

		protected TcpClient _tcp = null;
		protected EndPoint _localEp = null;
		protected EndPoint _remoteEp = null;

		private MyTlsClient _tlsClient = null;
		private TlsClientProtocol _tlsClientHandler = null;

		public SslClientPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnOpen()
		{
			base.OnOpen();

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
						throw new TimeoutException();
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

						Logger.Warn("open: Warn, Unable to connect to remote host {0} on port {1}.  Trying again in {2}ms...", Host, Port, waitTime);
						Thread.Sleep(waitTime);
					}
					else
					{
						Logger.Error("open: Error, Unable to connect to remote host {0} on port {1}.", Host, Port);
						throw new SoftException(ex);
					}
				}
			}

			Debug.Assert(_client == null);

			try
			{
				_tlsClientHandler = new TlsClientProtocol(_tcp.GetStream(), new SecureRandom());
				_tlsClient = new MyTlsClient();

				_tlsClientHandler.Connect(_tlsClient);

				_client = _tlsClientHandler.Stream;
				_localEp = _tcp.Client.LocalEndPoint;
				_remoteEp = _tcp.Client.RemoteEndPoint;
				_clientName = _remoteEp.ToString();

			}
			catch (Exception ex)
			{
				Logger.Error("open: Error, Unable to start tcp client reader. {0}.", ex.Message);
				throw new SoftException(ex);
			}

			StartClient();
		}

		protected override void ClientClose()
		{
			_tlsClientHandler.Close();
			_tlsClient = null;
			_tlsClientHandler = null;
			_tcp = null;
			_remoteEp = null;
			_localEp = null;
		}
	}
}
