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
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	[Parameter("PcapTimeout", typeof(int), "Pcap internal read timeout (default 10)", "10")]
	[Parameter("MinMTU", typeof(uint), "Minimum allowable MTU property value", DefaultMinMtu)]
	[Parameter("MaxMTU", typeof(uint), "Maximum allowable MTU property value", DefaultMaxMtu)]
	[Parameter("Filter", typeof(string), "Input filter in libpcap format", "")]
	[ObsoleteParameter("Protocol", "The RawEther publisher parameter 'Protocol' is no longer used.")]
	public class RawEtherPublisher : EthernetPublisher
	{
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
