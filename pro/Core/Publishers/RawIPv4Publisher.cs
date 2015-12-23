
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
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NLog;
using Peach.Core;
using Mono.Unix;
using Mono.Unix.Native;
using System.Runtime.InteropServices;
using System.IO;
using Peach.Core.IO;

namespace Peach.Pro.Core.Publishers
{
	internal static class RawHelpers
	{
		public const int IpHeaderLen = 20;

		public static void SetLength(byte[] buffer, int offset, int count)
		{
			if (count < IpHeaderLen)
				return;

			// Get in host order
			ushort ip_len = BitConverter.ToUInt16(buffer, offset + 2);
			ip_len += (ushort)(((ushort)(buffer[offset] & 0x0f)) << 2);
			// Set in network order
			buffer[offset + 2] = (byte)(ip_len >> 8);
			buffer[offset + 3] = (byte)(ip_len);
		}
	}

	public abstract class RawSocketPublisher : Publisher
	{
		int _fd;
		byte[] _localAddr;
		byte[] _remoteAddr;
		MemoryStream _rxStream = new MemoryStream(64 * 1024 * 2);

		public string Host { get; set; }
		public IPAddress Interface { get; set; }
		public int Protocol { get; set; }
		public int Timeout { get; set; }

		#region P/Invoke

		protected static int SOL_IP = (int)SocketOptionLevel.IP;

		protected static int AF_INET = (int)AddressFamily.InterNetwork;

		protected static int AF_INET6
		{
			get
			{
				switch (Platform.GetOS())
				{
				case Platform.OS.Windows:
					return (int)AddressFamily.InterNetworkV6;
				case Platform.OS.Linux:
					return 10;
				case Platform.OS.OSX:
					return 30;
				default:
					throw new NotSupportedException();
				}
			}
		}

		protected static int IP_HDRINCL
		{
			get
			{
				switch (Platform.GetOS())
				{
				case Platform.OS.Windows:
					return (int)SocketOptionName.HeaderIncluded;
				case Platform.OS.Linux:
					return 3;
				case Platform.OS.OSX:
					return 2;
				default:
					throw new NotSupportedException();
				}
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		protected struct sockaddr_in 
		{
			public ushort sin_family;
			public ushort sin_port;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public byte[] sin_addr;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public byte[] sin_padding;
		}

		[DllImport("libc", SetLastError = true)]
		protected static extern int socket(int domain, int type, int protocol);

		[DllImport("libc")]
		protected static extern int setsockopt(int socket, int level, int optname, IntPtr opt, int optlen);

		[DllImport("libc")]
		protected static extern int bind(int socket, ref sockaddr_in addr, int addrlen);

		[DllImport("libc")]
		protected static extern int sendto(int socket, IntPtr buf, int len, int flags, ref sockaddr_in dest_addr, int addrlen);

		[DllImport("libc")]
		protected static extern int recvfrom(int socket, IntPtr buf, int len, int flags, ref sockaddr_in from_addr, ref int fromlen);

		#endregion

		protected RawSocketPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnStart()
		{
			IPEndPoint ep;
			IPAddress local;
			try
			{
				ep = ResolveHost();

				local = Interface;
				if (Interface == null)
					local = GetLocalIp(ep);
			}
			catch (Exception ex)
			{
				Logger.Error("Unable to start publisher for {0}:{1}. {2}.", Host, Protocol, ex.Message);
				throw new SoftException(ex);
			}

			_remoteAddr = ep.Address.GetAddressBytes();
			_localAddr = local.GetAddressBytes();
		}

		protected override void OnOpen()
		{
			_fd = OpenSocket();

			var sa = new sockaddr_in 
			{
				sin_family = (ushort)AF_INET,
				sin_addr = _localAddr,
			};

			var ret = bind(_fd, ref sa, Marshal.SizeOf(sa));
			UnixMarshal.ThrowExceptionForLastErrorIf(ret);
		}

		protected override void OnClose()
		{
			if (_fd != -1)
			{
				Syscall.close(_fd);
				_fd = -1;
			}
		}

		protected override void OnInput()
		{
			var fds = new Pollfd[1];
			fds[0].fd = _fd;
			fds[0].events = PollEvents.POLLIN;

			var expires = Environment.TickCount + Timeout;

			for (;;)
			{
				try
				{
					var wait = Math.Max(0, expires - Environment.TickCount);
					fds[0].revents = 0;

					var ret = Syscall.poll(fds, wait);
					if (UnixMarshal.ShouldRetrySyscall(ret))
						continue;

					UnixMarshal.ThrowExceptionForLastErrorIf(ret);
					if (ret == 0)
						throw new TimeoutException();

					var sa = new sockaddr_in();
					var salen = Marshal.SizeOf(sa);

					_rxStream.Seek(0, SeekOrigin.Begin);
					_rxStream.SetLength(_rxStream.Capacity);

					var buf = _rxStream.GetBuffer();

					var ptr = GCHandle.Alloc(buf, GCHandleType.Pinned);
					var len = recvfrom(_fd, ptr.AddrOfPinnedObject(), buf.Length, 0, ref sa, ref salen);
					ptr.Free();

					if (UnixMarshal.ShouldRetrySyscall(len))
						continue;
					UnixMarshal.ThrowExceptionForLastErrorIf(len);

					_rxStream.SetLength(len);

					FilterInput(buf, 0, len);

					break;
				}
				catch (Exception ex)
				{
					if (ex is TimeoutException)
						Logger.Debug("Packet not received from {0} in {1}ms, timing out.", Interface, Timeout);
					else
						Logger.Error("Unable to receive packet from {0}. {1}", Interface, ex.Message);
					throw new SoftException(ex);
				}
			}
		}

		protected override void OnOutput(BitwiseStream data)
		{
			var buffer = new byte[data.Length];
			var len = data.Read(buffer, 0, buffer.Length);

			FilterOutput(buffer, 0, len);

			var fds = new Pollfd[1];
			fds[0].fd = _fd;
			fds[0].events = PollEvents.POLLOUT;

			var expires = Environment.TickCount + Timeout;

			for (;;)
			{
				try
				{
					var wait = Math.Max(0, expires - Environment.TickCount);
					fds[0].revents = 0;

					var ret = Syscall.poll(fds, wait);
					if (UnixMarshal.ShouldRetrySyscall(ret))
						continue;

					UnixMarshal.ThrowExceptionForLastErrorIf(ret);
					if (ret == 0)
						throw new TimeoutException();

					if (ret != 1)
						continue;

					if ((fds[0].revents & PollEvents.POLLNVAL) != 0)
						throw new Exception("Invalid request: fd not open");

					if ((fds[0].revents & PollEvents.POLLOUT) == 0)
						continue;

					var sa = new sockaddr_in 
					{
						sin_family = (ushort)AF_INET,
						sin_addr = _remoteAddr,
					};

					var ptr = GCHandle.Alloc(buffer, GCHandleType.Pinned);
					ret = sendto(_fd, ptr.AddrOfPinnedObject(), len, 0, ref sa, Marshal.SizeOf(sa));
					ptr.Free();

					if (UnixMarshal.ShouldRetrySyscall(ret))
						continue;
					UnixMarshal.ThrowExceptionForLastErrorIf(ret);

					if (ret != len)
						throw new Exception("Only sent {0} of {1} byte packet.".Fmt(ret, len));

					break;
				}
				catch (Exception ex)
				{
					if (ex is TimeoutException)
						Logger.Debug("Packet not sent to {0} in {1}ms, timing out.", Interface, Timeout);
					else
						Logger.Error("Unable to send packet to {0}. {1}", Interface, ex.Message);
					throw new SoftException(ex);
				}
			}
		}

		private IPEndPoint ResolveHost()
		{
			IPAddress[] entries = Dns.GetHostAddresses(Host);
			foreach (var ip in entries)
			{
				if (ip.ToString() != Host)
					Logger.Debug("Resolved host \"{0}\" to \"{1}\".", Host, ip);

				if (Interface == null && ip.IsIPv6LinkLocal && ip.ScopeId == 0)
					throw new PeachException("IPv6 scope id required for resolving link local address: '{0}'.".Fmt(Host));

				return new IPEndPoint(ip, 0);
			}

			throw new PeachException("Could not resolve the IP address of host \"" + Host + "\".");
		}

		private IPAddress GetLocalIp(IPEndPoint remote)
		{
			using (Socket s = new Socket(remote.AddressFamily, SocketType.Dgram, ProtocolType.Udp))
			{
				try
				{
					s.Connect(remote.Address, 22);
				}
				catch (SocketException)
				{
					if (remote.Address.IsMulticast())
						return remote.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any;

					throw;
				}
				IPEndPoint local = s.LocalEndPoint as IPEndPoint;
				return local.Address;
			}
		}

		protected abstract int OpenSocket();

		protected virtual void FilterInput(byte[] buffer, int offset, int count)
		{
		}

		protected virtual void FilterOutput(byte[] buffer, int offset, int count)
		{
		}

		#region Read Stream
		public override bool CanRead
		{
			get { return _rxStream.CanRead; }
		}

		public override bool CanSeek
		{
			get { return _rxStream.CanSeek; }
		}

		public override long Length
		{
			get { return _rxStream.Length; }
		}

		public override long Position
		{
			get { return _rxStream.Position; }
			set { _rxStream.Position = value; }
		}

		public override long Seek(long offset, System.IO.SeekOrigin origin)
		{
			return _rxStream.Seek(offset, origin);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _rxStream.Read(buffer, offset, count);
		}
		#endregion
	}

	/// <summary>
	/// Allows for input/output of raw IP packets.
	/// Protocol is the IP protocol number to send/receive.
	/// This publisher does not expect an IP header in the output buffer.
	/// The IP header is always included in the input buffer.
	/// </summary>
	/// <remarks>
	/// Mac raw sockets don't support TCP or UDP receptions.
	/// See the "b. FreeBSD" section at: http://sock-raw.org/papers/sock_raw
	/// </remarks>
	[Publisher("RawV4")]
	[Alias("Raw")]
	[Alias("raw.Raw")]
	[Parameter("Host", typeof(string), "Hostname or IP address of remote host")]
	[Parameter("Interface", typeof(IPAddress), "IP of interface to bind to", "")]
	[Parameter("Protocol", typeof(int), "IP protocol to use")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
//	[Parameter("MinMTU", typeof(uint), "Minimum allowable MTU property value", DefaultMinMTU)]
//	[Parameter("MaxMTU", typeof(uint), "Maximum allowable MTU property value", DefaultMaxMTU)]
	public class RawV4Publisher : RawSocketPublisher
	{
		private static NLog.Logger _logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return _logger; } }

		public RawV4Publisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override int OpenSocket()
		{
			int fd = socket(AF_INET, (int)SocketType.Raw, Protocol);
			UnixMarshal.ThrowExceptionForLastErrorIf(fd);

			var opt = 0;
			var ptr = GCHandle.Alloc(opt, GCHandleType.Pinned);
			var ret = setsockopt(fd, SOL_IP, IP_HDRINCL, ptr.AddrOfPinnedObject(), Marshal.SizeOf(opt));
			ptr.Free();
			UnixMarshal.ThrowExceptionForLastErrorIf(ret);

			return fd;
		}

		protected override void FilterInput(byte[] buffer, int offset, int count)
		{
			if (Platform.GetOS() != Platform.OS.OSX)
				return;

			// On OSX, ip_len is in host order and does not include the ip header
			// http://cseweb.ucsd.edu/~braghava/notes/freebsd-sockets.txt
			RawHelpers.SetLength(buffer, offset, count);
		}
	}

	/// <summary>
	/// Allows for input/output of raw IP packets.
	/// Protocol is the IP protocol number to send/receive.
	/// This publisher expects an IP header in the output buffer.
	/// The IP header is always included in the input buffer.
	/// </summary>
	/// <remarks>
	/// Mac raw sockets don't support TCP or UDP receptions.
	/// See the "b. FreeBSD" section at: http://sock-raw.org/papers/sock_raw
	/// </remarks>
	[Publisher("RawIPv4")]
	[Alias("RawIp")]
	[Alias("raw.RawIp")]
	[Parameter("Host", typeof(string), "Hostname or IP address of remote host")]
	[Parameter("Interface", typeof(IPAddress), "IP of interface to bind to", "")]
	[Parameter("Protocol", typeof(int), "IP protocol to use")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
//	[Parameter("MinMTU", typeof(uint), "Minimum allowable MTU property value", DefaultMinMTU)]
//	[Parameter("MaxMTU", typeof(uint), "Maximum allowable MTU property value", DefaultMaxMTU)]
	public class RawIPv4Publisher : RawSocketPublisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public RawIPv4Publisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override int OpenSocket()
		{
			int fd = socket(AF_INET, (int)SocketType.Raw, Protocol);
			UnixMarshal.ThrowExceptionForLastErrorIf(fd);

			var opt = 1;
			var ptr = GCHandle.Alloc(opt, GCHandleType.Pinned);
			var ret = setsockopt(fd, SOL_IP, IP_HDRINCL, ptr.AddrOfPinnedObject(), Marshal.SizeOf(opt));
			ptr.Free();
			UnixMarshal.ThrowExceptionForLastErrorIf(ret);

			return fd;
		}

		protected override void FilterInput(byte[] buffer, int offset, int count)
		{
			if (Platform.GetOS() != Platform.OS.OSX)
				return;

			// On OSX, ip_len is in host order and does not include the ip header
			// http://cseweb.ucsd.edu/~braghava/notes/freebsd-sockets.txt
			RawHelpers.SetLength(buffer, offset, count);
		}

		protected override void FilterOutput(byte[] buffer, int offset, int count)
		{
			if (Platform.GetOS() != Platform.OS.OSX)
				return;

			if (count < RawHelpers.IpHeaderLen)
				return;

			// On OSX, ip_len and ip_off need to be in host order
			// http://cseweb.ucsd.edu/~braghava/notes/freebsd-sockets.txt

			byte tmp;

			// Swap ip_len
			tmp = buffer[offset + 2];
			buffer[offset + 2] = buffer[offset + 3];
			buffer[offset + 3] = tmp;

			// Swap ip_off
			tmp = buffer[offset + 6];
			buffer[offset + 6] = buffer[offset + 7];
			buffer[offset + 7] = tmp;
		}
	}
}
