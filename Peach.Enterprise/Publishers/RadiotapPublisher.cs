
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using NLog;

using SharpPcap;
using SharpPcap.LibPcap;
//using SharpPcap.AirPcap;
using SharpPcap.WinPcap;
using PacketDotNet;


namespace Peach.Enterprise.Publishers
{
	[Publisher("Radiotap", true)]
	[Description("Uses the WIFI radiotap interface to inject and capture 802.11 packets")]
	[Parameter("Interface", typeof(string), "Interface to use", "wlan0")]
	[Parameter("CaptureMode", typeof(DeviceMode), "Capture mode to use (Promisucous, Normal)", "Promisucous")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	[Parameter("PrependRadioHeader", typeof(bool), "Prepend radiotap Header to outgoing data (default true)", "true")]
	public class RadiotapPublisher : Peach.Core.Publisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		protected string Interface { get; set; }
		protected DeviceMode CaptureMode { get; set; }
		protected int Timeout { get; set; }
		protected bool PrependRadioHeader { get; set; }

		protected LibPcapLiveDevice pcap;

		protected MemoryStream _recvBuffer = null;

		public RadiotapPublisher(Dictionary<string, Variant> args)
            : base(args)
        {
			ParameterParser.Parse(this, args);
		}

		protected override void OnStart()
		{
			var devices = LibPcapLiveDeviceList.Instance;

			foreach (var device in devices)
				if (device.Name == Interface)
					pcap = device;

			if (pcap == null)
				throw new PeachException("Radiotap Publisher was unable to find '" + Interface + "' interface.");

			_recvBuffer = new MemoryStream();
		}

		protected override void OnStop()
		{
			_recvBuffer.Close();
			_recvBuffer = null;
		}

		protected override void OnOpen()
		{
			pcap.Open(CaptureMode, 1);
			pcap.OnPacketArrival += new SharpPcap.PacketArrivalEventHandler(pcap_OnPacketArrival);
			pcap.StartCapture();
		}

		protected override void OnClose()
		{
			pcap.StopCapture();
			pcap.OnPacketArrival -= pcap_OnPacketArrival;
			pcap.Close();
		}

		protected override void OnOutput(BitwiseStream data)
		{
			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(data));

			try
			{
				var packet = new byte[data.Length];
				var pos = data.Position;
				data.Read(packet, 0, packet.Length);
				data.Seek(pos, System.IO.SeekOrigin.Begin);

				lock (pcap)
				{
					pcap.SendPacket(packet);
				}
			}
			catch (Exception ex)
			{
				throw new SoftException(ex.Message, ex);
			}
		}

        protected virtual void pcap_OnPacketArrival(object sender, SharpPcap.CaptureEventArgs e)
		{
			Packet packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

			_recvBuffer.SetLength(0);
			_recvBuffer.Write(packet.Bytes, 0, packet.Bytes.Length);
			_recvBuffer.Seek(0, SeekOrigin.Begin);
		}

		#region Read Stream

		public override bool CanRead
		{
			get { return _recvBuffer.CanRead; }
		}

		public override bool CanSeek
		{
			get { return _recvBuffer.CanSeek; }
		}

		public override long Length
		{
			get { return _recvBuffer.Length; }
		}

		public override long Position
		{
			get { return _recvBuffer.Position; }
			set { _recvBuffer.Position = value; }
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _recvBuffer.Seek(offset, origin);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _recvBuffer.Read(buffer, offset, count);
		}

		#endregion

	}
}
