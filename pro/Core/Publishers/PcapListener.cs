using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using SharpPcap;
using SharpPcap.LibPcap;
using SharpPcap.WinPcap;
using PacketDotNet;
using NLog;

using Peach.Core;

namespace Peach.Pro.Publishers
{
	/// <summary>
	/// Listen and queue incoming packets for use by Publisher
	/// </summary>
	/// <remarks>
	/// This class is intended for use by raw publishers as the receiving
	/// mechanism allowing filters to limit the received packets.
	/// </remarks>
	public class PcapListener
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected NLog.Logger Logger { get { return logger; } }

		ICaptureDevice _device;

		/// <summary>
		/// Queue of received packets. This is a thread safe queue.
		/// </summary>
		public ConcurrentQueue<RawCapture> PacketQueue = null;

		public PcapListener(System.Net.NetworkInformation.PhysicalAddress macAddress)
		{
			var devices = CaptureDeviceList.Instance;

			foreach (var device in devices)
			{
				device.Open();
				if (device.MacAddress.ToString() == macAddress.ToString())
				{
					_device = device;
					break;
				}
				device.Close();
			}

			if (_device == null)
				throw new ArgumentException("Unable to locate network device with mac '{0}'.", 
					macAddress.ToString());

			_device.OnPacketArrival += _device_OnPacketArrival;
		}

		public PcapListener(string deviceName)
		{
			var devices = CaptureDeviceList.Instance;

			foreach (var device in devices)
			{
				if (device.Name == deviceName)
				{
					_device = device;
					break;
				}
			}

			if (_device == null)
				throw new ArgumentException("Unable to locate network device '{0}'.", deviceName);

			_device.OnPacketArrival += _device_OnPacketArrival;
		}

		public PcapListener(System.Net.IPAddress Interface)
		{
			var globalip = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
			System.Net.NetworkInformation.PhysicalAddress macAddress = null;

			foreach (var adapter in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
			{
				if (adapter.Supports(System.Net.NetworkInformation.NetworkInterfaceComponent.IPv4) == false)
					continue;

				if (adapter.GetIPProperties().UnicastAddresses.Where(v => v.Address.ToString() == Interface.ToString()).Count() > 0)
				{
					macAddress = adapter.GetPhysicalAddress();
					break;
				}
			}

			if (macAddress == null)
				throw new PeachException(string.Format("Unable to locate adapter for interface '{0}'.", Interface));

			var devices = CaptureDeviceList.Instance;

			foreach (var device in devices)
			{
				device.Open();
				if (device.MacAddress.ToString() == macAddress.ToString())
				{
					_device = device;
					break;
				}
				device.Close();
			}

			if (_device == null)
				throw new ArgumentException("Unable to locate network device with mac '{0}'.",
					macAddress.ToString());

			_device.OnPacketArrival += _device_OnPacketArrival;
		}

		/// <summary>
		/// Capture filter. Follows the libpcap format.
		/// </summary>
		public string Filter { get { return _device.Filter; } set { _device.Filter = value; } }

		/// <summary>
		/// Start capturing packets
		/// </summary>
		public void Start()
		{
			Logger.Debug("Starting capture");
			PacketQueue = new ConcurrentQueue<RawCapture>();

			_device.Open(DeviceMode.Promiscuous);
			_device.StartCapture();
		}

		/// <summary>
		/// Stop capturing packets
		/// </summary>
		public void Stop()
		{
			Logger.Debug("Stopping capture");
			_device.StopCapture();
			_device.Close();
			_device = null;

			PacketQueue = null;
		}

		/// <summary>
		/// Clear queue
		/// </summary>
		public void Clear()
		{
			PacketQueue = new ConcurrentQueue<RawCapture>();
		}

		void _device_OnPacketArrival(object sender, CaptureEventArgs e)
		{
			PacketQueue.Enqueue(e.Packet);
			Logger.Debug("Queuing packet");
		}

		/// <summary>
		/// Send raw packet at the lowest layer supported by interface.
		/// </summary>
		/// <remarks>
		/// This interface works on both Windows and Linux, allowing true raw sockets
		/// on Windows via the winpcap service.
		/// </remarks>
		/// <param name="data">Packet to send</param>
		public void SendPacket(byte[] data)
		{
			_device.SendPacket(data);
		}

		/// <summary>
		/// Try and get an IPv4 packet from the captured data.
		/// </summary>
		/// <param name="capture"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public static bool TryAsIpv4(RawCapture capture, out byte [] data)
		{
			try
			{
				var packet = Packet.ParsePacket(LinkLayers.Ethernet, capture.Data);
				var ip = (IPv4Packet)packet.PayloadPacket;

				data = new byte[ip.Bytes.Length];
				Array.Copy(ip.Bytes, data, data.Length);

				return true;
			}
			catch
			{
				data = null;
				return false;
			}
		}

		/// <summary>
		/// Try and get an IPv6 packet from the captured data.
		/// </summary>
		/// <param name="capture"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public static bool TryAsIpv6(RawCapture capture, out byte[] data)
		{
			try
			{
				var packet = Packet.ParsePacket(LinkLayers.Ethernet, capture.Data);
				var ip = (IPv6Packet)packet.PayloadPacket;

				data = new byte[ip.Bytes.Length];
				Array.Copy(ip.Bytes, data, data.Length);

				return true;
			}
			catch
			{
				data = null;
				return false;
			}
		}

		/// <summary>
		/// Try and get a TCP packet from the captured data.
		/// </summary>
		/// <param name="capture"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public static bool TryAsTcp(RawCapture capture, out byte[] data)
		{
			try
			{
				var packet = Packet.ParsePacket(LinkLayers.Ethernet, capture.Data);
				var ip = packet.PayloadPacket;
				var tcp = (TcpPacket)ip.PayloadPacket;

				data = new byte[ip.Bytes.Length];
				Array.Copy(ip.Bytes, data, data.Length);

				return true;
			}
			catch
			{
				data = null;
				return false;
			}
		}
	}
}
