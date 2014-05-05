
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
    [Parameter("ApAuthTimeout", typeof(int), "How many milliseconds to wait for the AP auth state to complete (default 60000)", "60000")]
	[Parameter("PrependRadioHeader", typeof(bool), "Prepend Radiotap Header to outgoing data (default true)", "true")]
	[Parameter("SecurityMode", typeof(SecurityModes), "802.11 security mode to use", "None")]
	[Parameter("TargetMac", typeof(HexString), "MAC address of the target")]
	[Parameter("SourceMac", typeof(HexString), "MAC address of the host machine")]
	public class WifiPublisher : AirPcapPublisher
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		protected enum SecurityModes { None, WEP, WPA, WPA2 }

		protected SecurityModes SecurityMode { get; set; }
		protected HexString TargetMac { get; set; }
		protected HexString SourceMac { get; set; }
        protected int ApAuthTimeout { get; set; }
	
		byte[] beacon = null;
		byte[] auth;
		byte[] action;
		byte[] probeRequest;
		byte[] probeResponse;
		byte[] associationRequest;
		byte[] associationResponse;
		byte[] reassociationRequest;
		byte[] reassociationResponse;
		byte[] atim;
		byte[] powerSavePoll;
		byte[] readyToSend;
		byte[] clearToSend;
		byte[] acknowledgement;
		byte[] cfEnd;
		byte[] cfEndCfAck;
		byte[] keymessage1;
		byte[] keymessage2;
		byte[] keymessage3;
		byte[] keymessage4;

		static byte[] broadcast = new byte[6] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
		static byte[] target;
		static byte[] source;

        bool respondedToProbe;
        bool respondedToAuth;
        bool respondedToAssociationRequest;

		object mutex = new object();

		Thread beaconThread = null;

        #region Helper Methods

        string FormatAsMac(byte[] mac)
        {
            var str = new StringBuilder();

            foreach (var b in mac)
            {
                str.Append(string.Format("{0}:", b.ToString("x2")));
            }
            str.Remove(str.Length - 1, 1);

            return str.ToString();
        }

        bool AreEqual(byte[] buf1, int buf1Offset, int buf1Length, byte[] buf2)
        {
            if (buf1 == null || buf2 == null)
                return false;

            if (buf1Length != buf2.Length)
                return false;

            for (int i = 0; i < buf1Length; i++)
            {
                if (buf1[buf1Offset + i] != buf2[i])
                    return false;
            }

            return true;
        }

        byte[] DataModelToBuf(DataModel dm)
        {
            var bs = dm.Value;

            var buf = new BitReader(bs).ReadBytes((int)bs.Length);

            return buf;
        }

        #endregion

        public WifiPublisher(Dictionary<string, Variant> args) 
			: base(args)
		{
			ParameterParser.Parse(this, args);

			target = TargetMac.Value;
			source = SourceMac.Value;
		}

		protected override void OnOpen()
		{
		}

		protected override void OnClose()
		{
		}

       	protected override void OnStart()
		{
			base.OnStart();

            airPcap.Filter = string.Format("wlan src {0} and (wlan dst {1} or wlan dst ff:ff:ff:ff:ff:ff)",
                FormatAsMac(target), FormatAsMac(source));


            respondedToAuth = false;
            respondedToProbe = false;
            respondedToAssociationRequest = false;

			beaconThread = new Thread(BeaconThread);
			beaconThread.Start();
		}

		protected override void OnStop()
		{
			base.OnStop();

			beaconThread.Join();
			beaconThread = null;
		}

		protected override void OnInput()
		{
            var timeout = Timeout;
            var sw = new Stopwatch();
            sw.Restart();

            RawCapture packet = null;
            while (timeout > 0)
            {
                packet = airPcap.GetNextPacket();

                if (packet != null)
                {
                    break;
                }

                timeout -= (int)sw.ElapsedMilliseconds;
            }

            sw.Stop();

            if (timeout < 0)
                throw new SoftException("Didn't recieve a packet before the timeout expired.");

            _recvBuffer.SetLength(0);
            _recvBuffer.Write(packet.Data, 0, packet.Data.Length);
            _recvBuffer.Seek(0, SeekOrigin.Begin);
		}

		protected override Variant OnCall(string method, List<ActionParameter> args)
		{
			try
			{
				byte[] buf = null;
                if (args.Count > 0)
                 buf = DataModelToBuf(args[0].dataModel);

				if (method.Equals("Beacon"))
				{
					lock (mutex)
					{
						beacon = buf;
					}
				}
				else if (method.Equals("AuthenticationResponse"))
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
				else if (method.Equals("KeyMessage1"))
				{
					lock (mutex)
					{
						keymessage1 = buf;
					}
				}
				else if (method.Equals("KeyMessage2"))
				{
					lock (mutex)
					{
						keymessage2 = buf;
					}
				}
				else if (method.Equals("KeyMessage3"))
				{
					lock (mutex)
					{
						keymessage3 = buf;
					}
				}
				else if (method.Equals("KeyMessage4"))
				{
					lock (mutex)
					{
						keymessage4 = buf;
					}
				}
				else if (method.Equals("Action"))
				{
					lock (mutex)
					{
						action = buf;
					}
				}
				else if (method.Equals("ReassociationResponse"))
				{
					lock (mutex)
					{
						reassociationResponse = buf;
					}
				}
                else if (method.Equals("StartApAuth"))
                {
                    StartAuthState();
                }
			}
			catch (Exception ex)
			{
				logger.Debug("Exception: {0}", ex.Message);
				throw new PeachException(ex.Message, ex);
			}

			return null;
		}

        void SendPacket(byte[] buf)
        {

            if (buf != null)
                OnOutput(new BitStream(buf));
        }

        //Make this a call; Switch to regular input output. -- dhcp arp
        //Recive packet thread that acks on input and buffers data until input is called
        void StartAuthState()
        {
            var timeout = ApAuthTimeout;
            var sw = new Stopwatch();
            sw.Restart();

            while (timeout > 0)
            {
                var packet = airPcap.GetNextPacket();

                if (packet == null)
                {
                    timeout -= (int)sw.ElapsedMilliseconds;
                    continue;
                }

                var off = BitConverter.ToInt16(packet.Data, 2);

                var type = (packet.Data[off] & 0x0F) >> 2;
                var subtype = packet.Data[off] >> 4;

                if (type == 0)
                {
                    switch (subtype)
                    {
                        case 0:

                            if (!respondedToAssociationRequest)
                            {
                                SendPacket(acknowledgement);
                                SendPacket(associationResponse);
                                respondedToAssociationRequest = true;
                            }
                            //If Auth?
                            //KeyMessageState
                            //lock (mutex)
                            //{
                            // AuthState=True
                            // message = 1
                            //}
                            //SendPacket(keymessage1);
                            if (SecurityMode == SecurityModes.None)
                                return;

                            break;
                        case 2:
                            SendPacket(reassociationResponse);
                            break;
                        case 4:
                            if (!respondedToProbe)
                            {
                                SendPacket(probeResponse);
                                respondedToProbe = true;
                            }
                            break;
                        case 11:
                            if (!respondedToAuth)
                            {
                                SendPacket(acknowledgement);
                                SendPacket(auth);
                                respondedToAuth = true;
                            }
                            break;
                        case 13:
                            SendPacket(acknowledgement);
                            break;
                        default:
                            break;
                    }
                }
                else if (type == 1)
                {
                    switch (subtype)
                    {
                        case 10:
                            SendPacket(acknowledgement);
                            break;
                        case 11:
                            SendPacket(clearToSend);
                            break;
                        default:
                            break;
                    }
                }
                else if (type == 2)
                {
                    switch (subtype)
                    {
                        case 4:
                            SendPacket(acknowledgement);
                            break;
                        default:
                            SendPacket(acknowledgement);
                            break;
                    }
                }

                timeout -= (int)sw.ElapsedMilliseconds;
                //_recvBuffer.SetLength(0);
               // _recvBuffer.Write(packet.Data, 0, packet.Data.Length);
                //_recvBuffer.Seek(0, SeekOrigin.Begin);
            }

            sw.Stop();

            if (timeout < 0)
                throw new SoftException("Unable to complete 802.11 association.");
        }

        void BeaconThread()
        {
            while (true)
            {
                try
                {
                    //	lock (mutex)
                    {
                        if (beacon != null)
                        {
                            airPcap.SendPacket(beacon);
                            Thread.Sleep(125);
                        }
                    }
                }
                catch (SharpPcap.PcapException)
                {
                }
            }
        }
	}
}
