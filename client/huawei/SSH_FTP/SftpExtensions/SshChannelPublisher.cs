using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Renci.SshNet;
using Renci.SshNet.Sftp;

using System.IO;
using System.Diagnostics;
using NLog;

using Peach.Core;
using Peach.Core.IO;
using Peach.Core.Publishers;

namespace SftpExtensions
{
    public class OtherSftpClient : BaseClient
    {
        private SshSubsystemSession sftpSession;
        private TimeSpan _operationTimeout;

        public OtherSftpClient(ConnectionInfo connectionInfo)
            : base(connectionInfo)
        {
        }

        // default value is -1 ms, for infinite time-out period
        public TimeSpan OperationTimeout
        {
            get
            {
                //CheckDisposed();
                return _operationTimeout;
            }
            set
            {
                //CheckDisposed();
                _operationTimeout = value;
            }
        }

        protected override void OnConnected()
        {
            base.OnConnected();

            this.sftpSession = new SshSubsystemSession(this.Session, this.OperationTimeout, this.ConnectionInfo.Encoding);
            this.sftpSession.Connect();
        }

        protected override void OnDisconnecting()
        {
            base.OnDisconnecting();

            // disconnects and disposes the SFTP session
            // disposal is necessary since we create a new SFTP session on each connect
            if (this.sftpSession != null)
            {
                this.sftpSession.Disconnect();
                this.sftpSession.Dispose();
                this.sftpSession = null;
            }
        }

        // true to release both managed and unmanaged resources
        // false to release only unmanaged resources
        protected override void Dispose(bool disposing)
        {
            if (this.sftpSession != null)
            {
                this.sftpSession.Dispose();
                this.sftpSession = null;
            }

            base.Dispose(disposing);
        }

        public void Send(byte[] buffer)
        {
            if (this.sftpSession != null)
            {
                this.sftpSession.SendData(buffer);
            }
        }

        public byte[] NextBuffer()
        {
            byte[] buffer = null;
            if (this.sftpSession != null)
            {
                buffer = this.sftpSession.GetNextBuffer();
            }
            if (buffer != null)
            {
                return buffer;
            }
            else
            {
                return null;
            }
        }
    }
    class SshSubsystemSession : SubsystemSession
    {

        private ConcurrentQueue<byte[]> queue;

        public SshSubsystemSession(Session session, TimeSpan operationTimeout, System.Text.Encoding encoding)
            : base(session, "sftp", operationTimeout, encoding)
        {
            this.queue = new ConcurrentQueue<byte[]>();
        }

        protected override void Dispose(bool disposing)
        {
            this.queue = null;
            base.Dispose(disposing);
        }

        protected override void OnChannelOpen()
        {
            Console.WriteLine("Opened a channel.");
        }

        protected override void OnDataReceived(uint dataTypeCode, byte[] data)
        {
            this.queue.Enqueue(data);
        }

        public byte[] GetNextBuffer()
        {
            byte[] buffer;
            bool success = this.queue.TryDequeue(out buffer);
            if (success)
            {
                return buffer;
            }
            else
            {
                return null;
            }
        }
    }

    [Publisher("SshChannelPublisher", true)]
    [Parameter("Host", typeof(string), "Hostname")]
    [Parameter("Username", typeof(string), "Username")]
    [Parameter("Password", typeof(string), "Password", "")]
    [Parameter("KeyPath", typeof(string), "Path to ssh key", "")]
    public class SshChannelPublisher : StreamPublisher
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string KeyPath { get; set; }

        private OtherSftpClient otherSftpClient;
        private ConnectionInfo connInfo;
        private AuthenticationMethod authMethod;

        public SshChannelPublisher(Dictionary<string, Variant> args)
            : base(args)
        {
            ParameterParser.Parse(this, args);

            // both user/pass and key-based SSH auth
            if (Password == null && KeyPath == null && KeyPath != "")
                throw new PeachException("Either Password or KeyPath is required.");

			if (!string.IsNullOrEmpty(Password))
            {
                this.authMethod = new PasswordAuthenticationMethod(Username, Password);
            }
			else if (!string.IsNullOrEmpty(KeyPath))
			{
				var key = new PrivateKeyFile(KeyPath);
				this.authMethod = new PrivateKeyAuthenticationMethod(Username, key);
			}
			else
			{
				throw new PeachException("Error: Must supply one of 'Password' or 'KeyPath' to SshChannelPublisher.");
			}

            if (this.authMethod != null)
            {
                this.connInfo = new ConnectionInfo(Host, Username, this.authMethod);
                this.otherSftpClient = new OtherSftpClient(this.connInfo);
            }

            stream = new MemoryStream();
        }

        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
        protected override NLog.Logger Logger
        {
            get { return logger; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        protected override void OnOpen()
        {
            this.otherSftpClient.Connect();
        }

        protected override void OnClose()
        {
            this.otherSftpClient.Disconnect();
        }

        protected override void OnOutput(BitwiseStream data)
        {
            var bitReader = new BitReader(data);
            var buffer = new byte[data.Length];
            bitReader.ReadBytes(buffer.Length);
            this.otherSftpClient.Send(buffer);
        }

        protected override void OnInput()
        {
            byte[] buffer = this.otherSftpClient.NextBuffer();
            if (buffer != null)
            {
                var position = stream.Position;
                stream.Seek(0, SeekOrigin.End);
                stream.Write(buffer, 0, buffer.Length);
                stream.Position = position;
            }
        }
    }
}