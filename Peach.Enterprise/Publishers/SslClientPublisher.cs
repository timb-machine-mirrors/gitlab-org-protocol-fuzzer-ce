
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Adam Cecchetti (adam@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Net.Security; 
using System.Security.Cryptography.X509Certificates;


using Peach.Core.Dom;

using NLog;
using System.Diagnostics;
using Peach.Core.IO;

namespace Peach.Core.Publishers
{
	[Publisher("Ssl", true)]
	[Publisher("SslClient")]
	[Publisher("ssl.Ssl")]
    [Parameter("Host", typeof(string), "Hostname to connect to")]
    [Parameter("Port", typeof(ushort), "Port to connect to")]
    [Parameter("ClientCertPath", typeof(string), "Path to client certification file","")]
    [Parameter("ClientKeyPath", typeof(string), "Path to client key file","")]
    [Parameter("VerifyServer", typeof(bool), "Verify the server certificate", "false")] 
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data (default 3000)", "3000")]
	[Parameter("ConnectTimeout", typeof(int), "Max milliseconds to wait for connection (default 10000)", "10000")]
	[Parameter("Sni", typeof(string), "Sni to us for SSL connection. Will use Host by default","")]
	public class SslClientPublisher : BufferedStreamPublisher 
	{
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
        protected override NLog.Logger Logger { get { return logger; } }

        public string ClientCertPath { get; set; }
        public string ClientKeyPath { get; set; }
        public bool VerifyServer { get; set; }
        public string Host { get; set; }
		public string Sni { get; set; }
        public int ConnectTimeout { get; set; }
        public ushort Port { get; set; }

        protected TcpClient _tcp = null;
        protected EndPoint _localEp = null;
        protected EndPoint _remoteEp = null;

        private SslStream _sslStream = null;

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

            System.Diagnostics.Debug.Assert(_client == null);

            try
            {
                _sslStream = new SslStream(_tcp.GetStream(),
                           false,
                           new RemoteCertificateValidationCallback(ValidateServerCert),
                           null);
				if (string.IsNullOrEmpty(Sni))
				{
					_sslStream.AuthenticateAsClient(Host);
				}
				else
				{
					_sslStream.AuthenticateAsClient(Sni);
				}
                _client = _sslStream;
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

        public bool ValidateServerCert(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None || VerifyServer == false)
                return true;

            throw new SoftException("Certificate error: " + sslPolicyErrors);
        }

		protected override void ClientClose()
		{
			_sslStream.Close();
			_sslStream = null;
			_tcp = null;
			_remoteEp = null;
			_localEp = null;
		}
    }
}
