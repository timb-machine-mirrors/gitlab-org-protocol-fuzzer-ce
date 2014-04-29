
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
using System.Threading;

using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using NLog;

using SharpPcap;
using SharpPcap.AirPcap;
using PacketDotNet;


namespace Peach.Enterprise.Publishers
{
	[Publisher("Wifi",true)]
	[Parameter("DeviceNumber", typeof(int), "The AirPcap device to use.", "0")]
	[Parameter("CaptureMode", typeof(DeviceMode), "Capture mode to use (Promisucous, Normal)", "Promiscuous")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	[Parameter("PrependRadioHeader", typeof(bool), "Prepend Radiotap Header to outgoing data (default true)", "true")]
	[Parameter("SecurityMode", typeof(SecurityModes), "802.11 security mode to use", "None")]
	[Parameter("TargetMac", typeof(HexString), "MAC address of the target", "000000000000")]
	public class WifiPublisher : AirPcapPublisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		protected enum SecurityModes { None, WEP, WPA, WPA2 }

		protected SecurityModes SecurityMode { get; set; }
		protected HexString TargetMac { get; set; }

		

		

		WifiFrame beacon = null;
		WifiFrame auth;
		WifiFrame probeRequest;
		WifiFrame probeResponse;
		WifiFrame associationRequest;
		WifiFrame associationResponse;
		WifiFrame reassociationRequest;
		WifiFrame atim;
		WifiFrame powerSavePoll;
		WifiFrame readyToSend;
		WifiFrame clearToSend;
		WifiFrame acknowledgement;
		WifiFrame cfEnd;
		WifiFrame cfEndCfAck;

		Int16 seqNum = 0;

		object mutex = new object();
		Thread beaconThread = null;


		public WifiPublisher(Dictionary<string, Variant> args) 
			: base(args)
		{
			ParameterParser.Parse(this, args);
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

		protected override void OnStart()
		{
			airPcap.Open(CaptureMode);

			beaconThread = new Thread(BeaconThread);
			beaconThread.Start();
		}

		protected override void OnStop()
		{
			airPcap.Close();

			beaconThread.Join();
			beaconThread = null;
		}

		void SendPacket(WifiFrame buf)
		{
			lock (mutex)
			{
				if (buf != null)
					airPcap.SendPacket(buf.frame);
			}
		}

		bool AreEqual(byte[] buf1, byte[] buf2)
		{
			if (buf1 == null || buf2 == null)
				return false;

			if (buf1.Length != buf2.Length)
				return false;

			for (int i = 0; i < buf1.Length; i++)
			{
				if (buf1[i] != buf2[i])
					return false;
			}

			return true;
		}

		protected override void airPcap_OnPacketArrival(object sender, SharpPcap.CaptureEventArgs e)
		{
			Packet packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

			var off = packet.Header.Length;

			if (packet.Bytes.Length < (off + 16))
				return;

			var type = packet.Bytes[off] >> 4;

			var srcAddr = new byte[6];
			System.Array.Copy(packet.Bytes, off + 10, srcAddr, 0, 6);

			if (!AreEqual(srcAddr,TargetMac.Value))
				return;

			switch(type)
			{
				case 0:
					SendPacket(associationResponse);
					break;
				case 2:
				case 3:
				case 4:
					SendPacket(probeResponse);
					break;
				case 8:
				default:
					break;
			}
		}

		void UpdateSequence(WifiFrame buf)
		{
			if (buf.off < 0)
				return;

			seqNum++;
			var tmp = BitConverter.GetBytes(seqNum);
			var off = buf.off;

			buf.frame[off + 1] = tmp[1];

			tmp[0] = (byte)(tmp[0] & 0x0F);
			buf.frame[0] = (byte)(tmp[0] & buf.frame[0]);
		}

		void BeaconThread()
		{
			while (true)
			{
				try
				{
					lock (mutex)
					{
						if (beacon != null)
						{
						//	UpdateSequence(beacon);
							airPcap.SendPacket(beacon.frame);
							Thread.Sleep(100);
						}
					}
				}
				catch (Exception ex)
				{
					throw new SoftException(ex.Message, ex);
				}

				//Needs to be a better way to do this.
				
			}
		}

		long GetSequenceOffset(DataModel dm)
		{
			long off = 0;

			foreach (var d in dm)
			{
				if (d.name == "FragmentSequenceNumber")
					break;

				off += d.Value.Length;
			}

			return off;
		}

		WifiFrame DataModelToBuf(DataModel dm)
		{
			var bs = dm.Value;
			var buf = new byte[bs.Length];
			bs.Seek(0, SeekOrigin.Begin);
			bs.Read(buf, 0, buf.Length);
			bs.Seek(0, SeekOrigin.Begin);


			//TODO Do I need to update the sequence number for every packet...
			long off = 0;
			foreach (var d in dm)
			{
				if (d.name == "FragmentSequenceNumber")
					break;

				off += d.Value.Length;
			}

			var ret = new WifiFrame(buf, off);

			return ret;
		}

		protected override Variant OnCall(string method, List<ActionParameter> args)
		{
			try
			{
				var buf = DataModelToBuf(args[0].dataModel);

				if (method.Equals("Beacon"))
				{
					lock (mutex)
					{
						beacon = buf;
					}
				}
				else if (method.Equals("Auth"))
				{
					lock (mutex)
					{
						auth = buf;
					}
				}
				else if (method.Equals("ProbeRequest"))
				{
					lock (mutex)
					{
						probeRequest = buf;
					}
				}
				else if (method.Equals("ProbeResponse"))
				{
					lock (mutex)
					{
						probeResponse = buf;
					}
				}
				else if (method.Equals("AssociationRequest"))
				{
					lock (mutex)
					{
						associationRequest = buf;
					}
				}
				else if (method.Equals("AssociationResponse"))
				{
					lock (mutex)
					{
						associationResponse = buf;
					}
				}
				else if (method.Equals("ReassociationRequest"))
				{
					lock (mutex)
					{
						reassociationRequest = buf;
					}
				}
				else if (method.Equals("Atim"))
				{
					lock (mutex)
					{
						atim = buf;
					}
				}
				else if (method.Equals("PowerSavePoll"))
				{
					lock (mutex)
					{
						powerSavePoll = buf;
					}
				}
				else if (method.Equals("ReadyToSend"))
				{
					lock (mutex)
					{
						readyToSend = buf;
					}
				}
				else if (method.Equals("ClearToSend"))
				{
					lock (mutex)
					{
						clearToSend = buf;
					}
				}
				else if (method.Equals("Acknowledgement"))
				{
					lock (mutex)
					{
						acknowledgement = buf;
					}
				}
				else if (method.Equals("CfEnd"))
				{
					lock (mutex)
					{
						cfEnd = buf;
					}
				}
				else if (method.Equals("CfEndCfAck"))
				{
					lock (mutex)
					{
						cfEndCfAck = buf;
					}
				}
			}
			catch (Exception ex)
			{
				logger.Debug("Exception: {0}", ex.Message);
				throw new PeachException(ex.Message, ex);
			}

			return null;
		}	

		private class WifiFrame
		{
			public byte[] frame { get; private set; }
			public long off { get; private set; }

			public WifiFrame(byte[] frame, long off)
			{
				this.frame = frame;
				this.off = off;
			}
		}
	}
}
