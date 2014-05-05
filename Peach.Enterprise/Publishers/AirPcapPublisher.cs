
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
//   Jordyn Puryear (jordyn@dejavusecurity.com)

// $Id$

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
using SharpPcap.AirPcap;
using PacketDotNet;


namespace Peach.Enterprise.Publishers
{
	[Publisher("AirPcap", true)]
	[Parameter("DeviceNumber", typeof(int), "The AirPcap device to use.", "0")]
	[Parameter("CaptureMode", typeof(DeviceMode), "Capture mode to use (Promisucous, Normal)", "Promiscuous")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	[Parameter("PrependRadioHeader", typeof(bool), "Prepend Radiotap Header to outgoing data (default true)", "true")]
	public class AirPcapPublisher : Peach.Core.Publisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		protected int DeviceNumber { get; set; }
		protected DeviceMode CaptureMode { get; set; }
		protected int Timeout { get; set; }
		protected bool PrependRadioHeader { get; set; }

		protected ICaptureDevice airPcap;

		protected MemoryStream _recvBuffer = null;
		
		public AirPcapPublisher(Dictionary<string, Variant> args)
            : base(args)
        {
			ParameterParser.Parse(this, args);

			var devices = AirPcapDeviceList.Instance;

			if (devices.Count < 1 || devices.Count < (DeviceNumber - 1))
				throw new PeachException("The requested AirPcap device could not be found");

			airPcap = devices[DeviceNumber];
		}

		protected override void OnStart()
		{
			_recvBuffer = new MemoryStream();

			airPcap.Open(CaptureMode, 1);
		}

		protected override void OnStop()
		{
			airPcap.Close();

			_recvBuffer.Close();
		}

		protected override void OnOpen()
		{
			airPcap.OnPacketArrival += new SharpPcap.PacketArrivalEventHandler(airPcap_OnPacketArrival);
			airPcap.StartCapture();
		}

		protected override void OnClose()
		{
			airPcap.StopCapture();
			airPcap.OnPacketArrival -= airPcap_OnPacketArrival;
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

				lock (airPcap)
				{
					airPcap.SendPacket(packet);
				}
			}
			catch (Exception ex)
			{
				throw new SoftException(ex.Message, ex);
			}
		}

        protected virtual void airPcap_OnPacketArrival(object sender, SharpPcap.CaptureEventArgs e)
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
