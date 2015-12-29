using System;
using Peach.Core;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using NLog;
using Mono.Unix;

namespace Peach.Pro.Core.OS.Linux
{
	[PlatformImpl(Platform.OS.Linux)]
	public class DatagramImpl : Unix.DatagramImpl
	{
		private static NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public DatagramImpl(string publisher)
			: base(publisher)
		{
		}

		protected override IAddress CreateAddress(IPEndPoint ep)
		{
			if (ep.AddressFamily == AddressFamily.InterNetwork)
				return new IPv4Address(ep);
			return new IPv6Address(ep);
		}

		protected override void IncludeIpHeader(int fd)
		{
			var opt = 1;
			var ptr = GCHandle.Alloc(opt, GCHandleType.Pinned);
			var ret = setsockopt(fd, IPPROTO_IP, IP_HDRINCL, ptr.AddrOfPinnedObject(), sizeof(int));
			ptr.Free();
			UnixMarshal.ThrowExceptionForLastErrorIf(ret);
		}

		protected override void SetBufferSize(int fd, int bufSize)
		{
			bufSize /= 2;

			var ptr = GCHandle.Alloc(bufSize, GCHandleType.Pinned);
			try
			{
				var ret = setsockopt(fd, SOL_SOCKET, SO_SNDBUF, ptr.AddrOfPinnedObject(), sizeof(int));
				UnixMarshal.ThrowExceptionForLastErrorIf(ret);

				ret = setsockopt(fd, SOL_SOCKET, SO_RCVBUF, ptr.AddrOfPinnedObject(), sizeof(int));
				UnixMarshal.ThrowExceptionForLastErrorIf(ret);
			}
			finally
			{
				ptr.Free();
			}
		}

		protected override void OpenMulticast(
			int fd,
			IPEndPoint localEp, 
			IPEndPoint remoteEp, 
			NetworkInterface iface, 
			string ifaceName)
		{
			var ifindex = iface.GetIPProperties().GetIPv4Properties().Index;

			// Multicast needs to bind to the group on *nix
			if (remoteEp.Address.AddressFamily == AddressFamily.InterNetwork)
			{
				int ret;

				Logger.Debug("Binding to {0}", remoteEp.Address);
				using (var sa = CreateAddress(new IPEndPoint(remoteEp.Address, localEp.Port)))
				{
					ret = bind(fd, sa.Ptr, sa.Length);
					UnixMarshal.ThrowExceptionForLastErrorIf(ret);
				}

				var mreq = new ip_mreqn
				{
					imr_multiaddr = remoteEp.Address.GetAddressBytes(),
					imr_address = localEp.Address.GetAddressBytes(),
				};

				if (localEp.Address != IPAddress.Any)
					mreq.imr_ifindex = ifindex;

				ret = setsockopt(fd, IPPROTO_IP, IP_ADD_MEMBERSHIP, ref mreq, Marshal.SizeOf(mreq));
				ThrowPeachExceptionIf(ret, "Error, failed to join group '{0}' on interface '{1}'.".Fmt(remoteEp.Address, ifaceName));

				Logger.Trace("Setting multicast interface for {0} socket to {1}.", _publisher, localEp.Address);
				var ifaddr = localEp.Address.GetAddressBytes();
				ret = setsockopt(fd, IPPROTO_IP, IP_MULTICAST_IF, ifaddr, ifaddr.Length);
				ThrowPeachExceptionIf(ret, "Error, failed to set outgoing interface to '{1}' for group '{0}'.".Fmt(remoteEp.Address, ifaceName));
			}
			else
			{
				int ret;

				Logger.Debug("Binding to {0}", IPAddress.IPv6Any);
				using (var sa = CreateAddress(new IPEndPoint(IPAddress.IPv6Any, localEp.Port)))
				{
					ret = bind(fd, sa.Ptr, sa.Length);
					UnixMarshal.ThrowExceptionForLastErrorIf(ret);
				}

				var mreq = new ipv6_mreq() 
				{
					ipv6mr_multiaddr = remoteEp.Address.GetAddressBytes(),
					ipv6mr_ifindex = ifindex,
				};

				ret = setsockopt(fd, IPPROTO_IPV6, IPV6_ADD_MEMBERSHIP, ref mreq, Marshal.SizeOf(mreq));
				ThrowPeachExceptionIf(ret, "Error, failed to join group '{0}' on interface '{1}'.".Fmt(remoteEp.Address, ifaceName));

				Logger.Trace("Setting multicast interface for {0} socket to {1}.", _publisher, localEp.Address);
				var ptr = GCHandle.Alloc(ifindex, GCHandleType.Pinned);
				ret = setsockopt(fd, IPPROTO_IPV6, IPV6_MULTICAST_IF, ptr.AddrOfPinnedObject(), sizeof(int));
				ptr.Free();

				ThrowPeachExceptionIf(ret, "Error, failed to set outgoing interface to '{1}' for group '{0}'.".Fmt(remoteEp.Address, ifaceName));
			}
		}

		#region Native definitions

		const ushort AF_INET = 2;
		const ushort AF_INET6 = 10;

		const int SOL_SOCKET = 1;
		const int SO_SNDBUF = 7;
		const int SO_RCVBUF = 8;

		const int IPPROTO_IP = 0;
		const int IP_HDRINCL = 3;
		const int IP_MULTICAST_IF = 32;
		const int IP_ADD_MEMBERSHIP = 35;

		const int IPPROTO_IPV6 = 41;
		const int IPV6_MULTICAST_IF = 17;
		const int IPV6_ADD_MEMBERSHIP = 20;

		[StructLayout(LayoutKind.Sequential)]
		class sockaddr_in
		{
			public ushort sin_family;
			public ushort sin_port;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public byte[] sin_addr;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public byte[] sin_padding;
		}

		[StructLayout(LayoutKind.Sequential)]
		class sockaddr_in6
		{
			public ushort sin6_family;
			public ushort sin6_port;
			public uint   sin6_flowinfo;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public byte[] sin6_addr;
			public uint   sin6_scope_id;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct ip_mreqn 
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public byte[] imr_multiaddr;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public byte[] imr_address;
			public int    imr_ifindex;
		};

		[StructLayout(LayoutKind.Sequential)]
		struct ipv6_mreq
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public byte[] ipv6mr_multiaddr;
			public int    ipv6mr_ifindex;
		}

		[DllImport("libc", SetLastError = true)]
		static extern int setsockopt(int socket, int level, int optname, ref ip_mreqn opt, int optlen);

		[DllImport("libc", SetLastError = true)]
		static extern int setsockopt(int socket, int level, int optname, ref ipv6_mreq opt, int optlen);

		#endregion

		#region IPvXAddress

		class IPv4Address : IAddress
		{
			IntPtr ptr;

			public IPv4Address(IPEndPoint ep)
			{
				var sa = new sockaddr_in 
				{
					sin_family = AddressFamily,
					sin_addr = ep.Address.GetAddressBytes(),
					sin_port = Unix.DatagramImpl.Swap16(ep.Port),
				};

				ptr = Marshal.AllocHGlobal(Length);
				Marshal.StructureToPtr(sa, ptr, false);
			}

			public void Dispose()
			{
				Marshal.FreeHGlobal(ptr);
			}

			public ushort AddressFamily { get { return AF_INET; } }
			public int Length { get { return Marshal.SizeOf(typeof(sockaddr_in)); } }
			public IntPtr Ptr { get { return ptr; } }

			public IPEndPoint EndPoint
			{
				get
				{
					var sa = new sockaddr_in();
					Marshal.PtrToStructure(ptr, sa);
					return new IPEndPoint(
						new IPAddress(sa.sin_addr), 
						Unix.DatagramImpl.Swap16(sa.sin_port)
					);
				}
			}
		}

		class IPv6Address : IAddress
		{
			IntPtr ptr;

			public IPv6Address(IPEndPoint ep)
			{
				var sa = new sockaddr_in6 
				{
					sin6_family = AddressFamily,
					sin6_addr = ep.Address.GetAddressBytes(),
					sin6_scope_id = (uint)ep.Address.ScopeId,
					sin6_port = Unix.DatagramImpl.Swap16(ep.Port),
				};

				ptr = Marshal.AllocHGlobal(Length);
				Marshal.StructureToPtr(sa, ptr, false);
			}

			public void Dispose()
			{
				Marshal.FreeHGlobal(ptr);
			}

			public ushort AddressFamily { get { return AF_INET6; } }
			public int Length { get { return Marshal.SizeOf(typeof(sockaddr_in6)); } }
			public IntPtr Ptr { get { return ptr; } }

			public IPEndPoint EndPoint
			{
				get
				{
					var sa = new sockaddr_in6();
					Marshal.PtrToStructure(ptr, sa);
					return new IPEndPoint(
						new IPAddress(sa.sin6_addr, sa.sin6_scope_id), 
						Unix.DatagramImpl.Swap16(sa.sin6_port)
					);
				}
			}
		}

		#endregion
	}
}
