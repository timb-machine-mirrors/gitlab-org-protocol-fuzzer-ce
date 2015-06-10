using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.IO;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("RawEther")]
	[Alias("raw.RawEther")]
	[Parameter("Interface", typeof(string), "Name of interface to bind to")]
	[Parameter("Protocol", typeof(EtherProto), "Ethernet protocol to use", "ETH_P_ALL")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	[Parameter("PcapTimeout", typeof(int), "Pcap internal read timeout (default 10)", "10")]
	[Parameter("MinMTU", typeof(uint), "Minimum allowable MTU property value", DefaultMinMtu)]
	[Parameter("MaxMTU", typeof(uint), "Maximum allowable MTU property value", DefaultMaxMtu)]
	[Parameter("Filter", typeof(string), "Input filter in libpcap format", "")]
	public class RawEtherPublisher : EthernetPublisher
	{
		#region Obsolete Functions

		// ReSharper disable InconsistentNaming
		public enum EtherProto : ushort
		{
			// These are the defined Ethernet Protocol ID's.
			ETH_P_LOOP = 0x0060, // Ethernet Loopback packet
			ETH_P_PUP = 0x0200, // Xerox PUP packet
			ETH_P_PUPAT = 0x0201, // Xerox PUP Addr Trans packet
			ETH_P_IP = 0x0800, // Internet Protocol packet
			ETH_P_X25 = 0x0805, // CCITT X.25
			ETH_P_ARP = 0x0806, // Address Resolution packet
			ETH_P_BPQ = 0x08FF, // G8BPQ AX.25 Ethernet Packet  [ NOT AN OFFICIALLY REGISTERED ID ]
			ETH_P_IEEEPUP = 0x0a00, // Xerox IEEE802.3 PUP packet
			ETH_P_IEEEPUPAT = 0x0a01, // Xerox IEEE802.3 PUP Addr Trans packet
			ETH_P_DEC = 0x6000, // DEC Assigned proto
			ETH_P_DNA_DL = 0x6001, // DEC DNA Dump/Load
			ETH_P_DNA_RC = 0x6002, // DEC DNA Remote Console
			ETH_P_DNA_RT = 0x6003, // DEC DNA Routing
			ETH_P_LAT = 0x6004, // DEC LAT
			ETH_P_DIAG = 0x6005, // DEC Diagnostics
			ETH_P_CUST = 0x6006, // DEC Customer use
			ETH_P_SCA = 0x6007, // DEC Systems Comms Arch
			ETH_P_TEB = 0x6558, // Trans Ether Bridging
			ETH_P_RARP = 0x8035, // Reverse Addr Res packet
			ETH_P_ATALK = 0x809B, // Appletalk DDP
			ETH_P_AARP = 0x80F3, // Appletalk AARP
			ETH_P_8021Q = 0x8100, // 802.1Q VLAN Extended Header
			ETH_P_IPX = 0x8137, // IPX over DIX
			ETH_P_IPV6 = 0x86DD, // IPv6 over bluebook
			ETH_P_PAUSE = 0x8808, // IEEE Pause frames. See 802.3 31B
			ETH_P_SLOW = 0x8809, // Slow Protocol. See 802.3ad 43B
			ETH_P_WCCP = 0x883E, // Web-cache coordination protocol defined in draft-wilson-wrec-wccp-v2-00.txt
			ETH_P_PPP_DISC = 0x8863, // PPPoE discovery messages
			ETH_P_PPP_SES = 0x8864, // PPPoE session messages
			ETH_P_MPLS_UC = 0x8847, // MPLS Unicast traffic
			ETH_P_MPLS_MC = 0x8848, // MPLS Multicast traffic
			ETH_P_ATMMPOA = 0x884c, // MultiProtocol Over ATM
			ETH_P_LINK_CTL = 0x886c, // HPNA, wlan link local tunnel
			ETH_P_ATMFATE = 0x8884, // Frame-based ATM Transport over Ethernet
			ETH_P_PAE = 0x888E, // Port Access Entity (IEEE 802.1X)
			ETH_P_AOE = 0x88A2, // ATA over Ethernet
			ETH_P_8021AD = 0x88A8, // 802.1ad Service VLAN
			ETH_P_TIPC = 0x88CA, // TIPC
			ETH_P_8021AH = 0x88E7, // 802.1ah Backbone Service Tag
			ETH_P_1588 = 0x88F7, // IEEE 1588 Timesync
			ETH_P_FCOE = 0x8906, // Fibre Channel over Ethernet
			ETH_P_TDLS = 0x890D, // TDLS
			ETH_P_FIP = 0x8914, // FCoE Initialization Protocol
			ETH_P_QINQ1 = 0x9100, // deprecated QinQ VLAN [ NOT AN OFFICIALLY REGISTERED ID ]
			ETH_P_QINQ2 = 0x9200, // deprecated QinQ VLAN [ NOT AN OFFICIALLY REGISTERED ID ]
			ETH_P_QINQ3 = 0x9300, // deprecated QinQ VLAN [ NOT AN OFFICIALLY REGISTERED ID ]
			ETH_P_EDSA = 0xDADA, // Ethertype DSA [ NOT AN OFFICIALLY REGISTERED ID ]
			ETH_P_AF_IUCV = 0xFBFB, // IBM af_iucv [ NOT AN OFFICIALLY REGISTERED ID ]

			// Non DIX types. Won't clash for 1500 types.
			ETH_P_802_3 = 0x0001, // Dummy type for 802.3 frames
			ETH_P_AX25 = 0x0002, // Dummy protocol id for AX.25
			ETH_P_ALL = 0x0003, // Every packet (be careful!!!)
			ETH_P_802_2 = 0x0004, // 802.2 frames
			ETH_P_SNAP = 0x0005, // Internal only
			ETH_P_DDCMP = 0x0006, // DEC DDCMP: Internal only
			ETH_P_WAN_PPP = 0x0007, // Dummy type for WAN PPP frames
			ETH_P_PPP_MP = 0x0008, // Dummy type for PPP MP frames
			ETH_P_LOCALTALK = 0x0009, // Localtalk pseudo type
			ETH_P_CAN = 0x000C, // Controller Area Network
			ETH_P_PPPTALK = 0x0010, // Dummy type for Atalk over PPP
			ETH_P_TR_802_2 = 0x0011, // 802.2 frames
			ETH_P_MOBITEX = 0x0015, // Mobitex (kaz@cafe.net)
			ETH_P_CONTROL = 0x0016, // Card specific control frames
			ETH_P_IRDA = 0x0017, // Linux-IrDA
			ETH_P_ECONET = 0x0018, // Acorn Econet
			ETH_P_HDLC = 0x0019, // HDLC frames
			ETH_P_ARCNET = 0x001A, // 1A for ArcNet :-)
			ETH_P_DSA = 0x001B, // Distributed Switch Arch.
			ETH_P_TRAILER = 0x001C, // Trailer switch tagging
			ETH_P_PHONET = 0x00F5, // Nokia Phonet frames
			ETH_P_IEEE802154 = 0x00F6, // IEEE802.15.4 frame
			ETH_P_CAIF = 0x00F7, // ST-Ericsson CAIF protocol
		}
		// ReSharper restore InconsistentNaming

		public EtherProto Protocol { get; protected set; }

		#endregion

		public string Interface { get; set; }
		public int PcapTimeout { get; set; }
		public string Filter { get; set; }

		protected override string DeviceName
		{
			get
			{
				return _deviceName;
			}
		}

		static readonly NLog.Logger ClassLogger = LogManager.GetCurrentClassLogger();

		protected override NLog.Logger Logger { get { return ClassLogger; } }

		readonly Queue<RawCapture> _queue = new Queue<RawCapture>();

		LibPcapLiveDevice _device;
		string _deviceName;

		public RawEtherPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			stream = new MemoryStream();
		}

		static IEnumerable<LibPcapLiveDevice> Devices
		{
			get
			{
				try
				{
					// Must use New() so we can have multiple publishers using the same pcap device
					var devs = CaptureDeviceList.New().OfType<LibPcapLiveDevice>().ToList();

					if (devs.Count == 0)
						throw new PeachException("No pcap devices found. Ensure appropriate permissions for using libpcap.");

					return devs;
				}
				catch (DllNotFoundException ex)
				{
					throw new PeachException("An error occurred getting the pcap device list.  Ensure libpcap is installed and try again.", ex);
				}
			}
		}

		private void OnPacketArrival(object sender, CaptureEventArgs e)
		{
			lock (_queue)
			{
				_queue.Enqueue(e.Packet);
				Monitor.Pulse(_queue);
			}
		}

		private byte[] GetNextPacket()
		{
			lock (_queue)
			{
				if (_queue.Count == 0 && !Monitor.Wait(_queue, Timeout))
					return null;

				return _queue.Dequeue().Data;
			}
		}

		protected override void OnStart()
		{
			_device = Devices.FirstOrDefault(d => d.Interface.FriendlyName == Interface);

			if (_device == null)
				throw new PeachException("Unable to locate pcap device named '{0}'.".Fmt(Interface));

			string error;
			if (!PcapDevice.CheckFilter(Filter, out error))
				throw new PeachException("The specified pcap filter string '{0}' is invalid.".Fmt(Filter));

			_device.Open(DeviceMode.Promiscuous, PcapTimeout);
			_device.Filter = Filter;
			_device.OnPacketArrival += OnPacketArrival;
			_device.StartCapture();

			_deviceName = _device.Interface.FriendlyName;

			base.OnStart();
		}

		protected override void OnStop()
		{
			base.OnStop();

			_device.StopCapture();
			_device.OnPacketArrival -= OnPacketArrival;
			_device.Close();
			_device = null;
			_deviceName = null;
		}

		protected override void OnOpen()
		{
			lock (_queue)
			{
				// Just need to clear any previously collected packets
				// StartCapture and StopCapture are slow so don't change
				// our capture state on each iteration
				_queue.Clear();
			}
		}

		protected override void OnClose()
		{
			// Don't need to do anything here
		}

		protected override void OnInput()
		{
			stream.Position = 0;
			stream.SetLength(0);

			var buf = GetNextPacket();

			if (buf == null)
			{
				var msg = "Timeout waiting for input from interface '{0}'.".Fmt(Interface);

				Logger.Debug(msg);

				if (!NoReadException)
					throw new SoftException(msg);

				return;
			}

			stream.Write(buf, 0, buf.Length);
			stream.Position = 0;

			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(stream));
		}

		protected override void OnOutput(BitwiseStream data)
		{
			data.Seek(0, SeekOrigin.Begin);

			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(data));

			var len = (int)Math.Min(data.Length, MaxMTU);
			var buf = new byte[len];

			len = data.Read(buf, 0, buf.Length);

			try
			{
				_device.SendPacket(buf);
			}
			catch (Exception ex)
			{
				throw new SoftException(ex.Message, ex);
			}

			if (len < data.Length)
				throw new SoftException("Only sent {0} of {1} byte packet.".Fmt(buf, data.Length));
		}
	}
}
