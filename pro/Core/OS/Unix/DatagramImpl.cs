using System;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.NetworkInformation;
using Peach.Core;
using Peach.Pro;
using System.Net.Sockets;
using NLog;
using Mono.Unix;
using Mono.Unix.Native;

namespace Peach.Pro.Core.OS.Unix
{
	public abstract class DatagramImpl : IDatagramImpl
	{
		public static ushort Swap16(int x)
		{
			var ret = ((x & 0xff00) >> 8) | ((x & 0x00ff) << 8);
			return (ushort)ret;
		}

		protected interface IAddress : IDisposable
		{
			ushort AddressFamily { get; }

			IntPtr Ptr { get; }

			int Length { get; }

			IPEndPoint EndPoint { get; }
		}

		private static NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		protected string _publisher;
		protected int _fd = -1;
		protected string _ifaceName;
		protected IPEndPoint _bindEp;

		public DatagramImpl(string publisher)
		{
			_publisher = publisher;
		}

		protected abstract void SetBufferSize(int fd, int bufSize);
		protected abstract void IncludeIpHeader(int fd);
		protected abstract IAddress CreateAddress(IPEndPoint ep);

		protected abstract void OpenMulticast(
			int fd,
			IPEndPoint bindEp, 
			IPEndPoint remoteEp, 
			NetworkInterface iface, 
			string ifaceName
		);

		#region IDatagramImpl implementation

		public IPEndPoint Open(
			SocketType socketType,
			byte protocol,
			bool ipHeaderInclude,
			IPEndPoint bindEp, 
			IPEndPoint remoteEp, 
			NetworkInterface iface, 
			string ifaceName,
			int bufSize)
		{
			_ifaceName = ifaceName;
			_bindEp = bindEp;

			using (var sa = CreateAddress(bindEp))
			{
				_fd = socket(sa.AddressFamily, (int)socketType, protocol);
				UnixMarshal.ThrowExceptionForLastErrorIf(_fd);

				if (ipHeaderInclude)
					IncludeIpHeader(_fd);

				SetBufferSize(_fd, bufSize);

				if (remoteEp.Address.IsMulticast())
				{
					OpenMulticast(_fd, bindEp, remoteEp, iface, ifaceName);
				}
				else
				{
					var ret = bind(_fd, sa.Ptr, sa.Length);
					UnixMarshal.ThrowExceptionForLastErrorIf(ret);
				}
			}

			using (var sa = CreateAddress(bindEp))
			{
				var salen = sa.Length;
				var ret = getsockname(_fd, sa.Ptr, ref salen);
				UnixMarshal.ThrowExceptionForLastErrorIf(ret);

				return sa.EndPoint;
			}
		}

		public void Close()
		{
			if (_fd != -1)
			{
				Syscall.close(_fd);
				_fd = -1;
			}
		}

		public void Send(IPEndPoint remoteEp, byte[] buf, int len, int timeout)
		{
			var fds = new Pollfd[1];
			fds[0].fd = _fd;
			fds[0].events = PollEvents.POLLOUT;

			var expires = Environment.TickCount + timeout;

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

					Logger.Trace("sendto(): {0}", remoteEp);

					using (var sa = CreateAddress(remoteEp))
					{
						var ptr = GCHandle.Alloc(buf, GCHandleType.Pinned);
						ret = sendto(_fd, ptr.AddrOfPinnedObject(), len, 0, sa.Ptr, sa.Length);
						ptr.Free();
					}

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
						Logger.Debug("Packet not sent to {0} in {1}ms, timing out.", _ifaceName, timeout);
					else
						Logger.Error("Unable to send packet to {0}. {1}", _ifaceName, ex.Message);
					throw new SoftException(ex);
				}
			}
		}

		public IPEndPoint Receive(IPEndPoint expected, byte[] buf, out int len, int timeout)
		{
			var fds = new Pollfd[1];
			fds[0].fd = _fd;
			fds[0].events = PollEvents.POLLIN;

			var expires = Environment.TickCount + timeout;

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

					using (var sa = CreateAddress(_bindEp))
					{
						var salen = sa.Length;

						var ptr = GCHandle.Alloc(buf, GCHandleType.Pinned);
						len = recvfrom(_fd, ptr.AddrOfPinnedObject(), buf.Length, 0, sa.Ptr, ref salen);
						ptr.Free();

						if (UnixMarshal.ShouldRetrySyscall(len))
							continue;
						UnixMarshal.ThrowExceptionForLastErrorIf(len);

						var actual = sa.EndPoint;

						if (!expected.Address.IsMulticast())
						{
							if (expected.Port == 0)
							{
								if (!IPAddress.Equals(expected.Address, actual.Address))
								{
									Logger.Debug("Ignoring received packet from {0}, want packets from {1}.", actual, expected);
									continue;
								}

								if (actual.Port != 0)
								{
									Logger.Debug("Updating expected remote address from {0} to {1}.", expected, actual);
									expected.Port = actual.Port;
								}
							}
							else if (!IPEndPoint.Equals(actual, expected))
							{
								Logger.Debug("Ignoring received packet from {0}, want packets from {1}.", actual, expected);
								continue;
							}
						}

						return actual;
					}
				}
				catch (Exception ex)
				{
					if (ex is TimeoutException)
						Logger.Debug("Packet not received from {0} in {1}ms, timing out.", _ifaceName, timeout);
					else
						Logger.Error("Unable to receive packet from {0}. {1}", _ifaceName, ex.Message);
					throw new SoftException(ex);
				}
			}
		}

		#endregion

		protected void ThrowPeachExceptionIf(int ret, string msg)
		{
			try
			{
				UnixMarshal.ThrowExceptionForLastErrorIf(ret);
			}
			catch (Exception ex)
			{
				throw new PeachException("{0} {1}".Fmt(msg, ex.Message));
			}
		}

		[DllImport("libc", SetLastError = true)]
		protected static extern int socket(int family, int type, int protocol);

		[DllImport("libc", SetLastError = true)]
		protected static extern int setsockopt(int socket, int level, int optname, byte[] opt, int optlen);

		[DllImport("libc", SetLastError = true)]
		protected static extern int setsockopt(int socket, int level, int optname, IntPtr opt, int optlen);

		[DllImport("libc", SetLastError = true)]
		protected static extern int getsockname(int socket, IntPtr addr, ref int addrlen);

		[DllImport("libc", SetLastError = true)]
		protected static extern int bind(int socket, IntPtr addr, int addrlen);

		[DllImport("libc", SetLastError = true)]
		protected static extern int sendto(int socket, IntPtr buf, int len, int flags, IntPtr dest_addr, int addrlen);

		[DllImport("libc", SetLastError = true)]
		protected static extern int recvfrom(int socket, IntPtr buf, int len, int flags, IntPtr from_addr, ref int fromlen);
	}
}
