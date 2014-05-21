
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

using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Generators;

using SharpPcap;
using SharpPcap.AirPcap;
using SharpPcap.WinPcap;
using PacketDotNet;

namespace Peach.Enterprise.Publishers
{
	[Publisher("Wifi",true)]
	[Parameter("DeviceNumber", typeof(int), "The AirPcap device to use.", "0")]
    [Parameter("CaptureMode", typeof(OpenFlags), "Capture mode to use (Promisucous, Normal)", "MaxResponsiveness")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
    [Parameter("ApAuthTimeout", typeof(int), "How many milliseconds to wait for the AP auth state to complete (default 120000)", "120000")]
	[Parameter("PrependRadioHeader", typeof(bool), "Prepend Radiotap Header to outgoing data (default true)", "true")]
	[Parameter("SecurityMode", typeof(SecurityModes), "802.11 security mode to use", "None")]
	[Parameter("TargetMac", typeof(HexString), "MAC address of the target")]
	[Parameter("SourceMac", typeof(HexString), "MAC address of the host machine")]
    [Parameter("Password", typeof(string), "WPA/WEP Password", "")]
    [Parameter("Ssid", typeof(string), "Ssid of the network", "")]
    [Parameter("Channel", typeof(uint), "Wireless channel to send/listen for packets on", "11")]
	public class WifiPublisher : AirPcapPublisher
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		protected enum SecurityModes { None, WEP, WPA, WPA2Aes, WPA2Tkip }
        protected enum DhcpStates { None, FirstReply, SentOffer}

		protected SecurityModes SecurityMode { get; set; }
        protected DhcpStates DhcpState { get; set; }
		protected HexString TargetMac { get; set; }
		protected HexString SourceMac { get; set; }
        protected int ApAuthTimeout { get; set; }
        protected uint Channel { get; set; }
        protected string Password { get; set; }
        protected string Ssid { get; set; }
	
		DataModel beacon = null;
        DataModel auth = null;
		DataModel actionBlockAckReq = null;
        DataModel actionBlockAckRep = null;
        DataModel probeRequest = null;
        DataModel probeResponse = null;
        DataModel associationRequest = null;
        DataModel associationResponse = null;
        DataModel reassociationRequest = null;
        DataModel reassociationResponse = null;
        DataModel atim = null;
        DataModel powerSavePoll = null;
        DataModel readyToSend = null;
        DataModel clearToSend = null;
        DataModel acknowledgement = null;
        DataModel cfEnd = null;
        DataModel cfEndCfAck = null;
        DataModel blockAck = null;
        DataModel keymessage1 = null;
        DataModel keymessage2 = null;
        DataModel keymessage3 = null;
        DataModel keymessage4 = null;
        DataModel dhcpNak = null;
        DataModel dhcpAck = null;
        DataModel dhcpOffer = null;
        DataModel arp = null;

        //WPA2 Data
        Wpa wpa = null;
        byte[] aNonce = null;
        byte[] sNonce = null;

		static byte[] broadcast = new byte[6] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
		static byte[] target;
		static byte[] source;

        bool respondedToProbe;
        bool respondedToAuth;
        bool respondedToAssociationRequest;
        bool receiveData;

        UInt16 sequenceNumber = 0;

		object mutex = new object();

		Thread beaconThread = null;

		void compile()
		{
			SendPacket(probeRequest);
			SendPacket(associationRequest);
			SendPacket(reassociationRequest);
			SendPacket(atim);
			SendPacket(powerSavePoll);
			SendPacket(readyToSend);
			SendPacket(cfEnd);
			SendPacket(keymessage2);
			SendPacket(keymessage4);
			SendPacket(cfEndCfAck);
		}


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

            if (buf1.Length < buf1Offset + 6)
                return false;

            //Looking at the first 5 bytes due to rts
            for (int i = 0; i < buf1Length - 1; i++)
            {
                if (buf1[buf1Offset + i] != buf2[i])
                    return false;
            }

            return true;
        }

        byte[] DataModelToBuf(DataModel dm)
        {
            var bs = dm.Value;
            bs.Seek(0, SeekOrigin.Begin);

            var buf = new BitReader(bs).ReadBytes((int)bs.Length);

            return buf;
        }

        void ResetState()
        {
            aNonce = null;
            sNonce = null;

            sequenceNumber = 0;

            wpa = new Wpa();

            respondedToAuth = false;
            respondedToProbe = false;
            respondedToAssociationRequest = false;

            DhcpState = DhcpStates.None;
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

            airPcap.Channel = Channel;
            airPcap.MacFlags = AirPcapMacFlags.MonitorModeOn;
            airPcap.AirPcapLinkType = AirPcapLinkTypes._802_11;


           // airPcap.Filter = string.Format("wlan src {0} and (wlan dst {1} or wlan dst ff:ff:ff:ff:ff:ff) or wlan host {0}",
              //  FormatAsMac(target), FormatAsMac(source));

            ResetState();

            beaconThread = new Thread(BeaconThread);
            beaconThread.Start();
		}

        protected override void OnStop()
        {
            base.OnStop();

            if (beacon != null)
            {
                beaconThread.Join(100);
                beaconThread = null;
            }
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

                if (packet != null && receiveData)
                {
                    var off = BitConverter.ToInt16(packet.Data, 2);

                    var type = (packet.Data[off] & 0x0F) >> 2;
                    var subtype = packet.Data[off] >> 4;

                    if (type == 2 && subtype == 0)
                    {
                        var ipv4 = IPv4Packet.ParsePacket(packet.LinkLayerType, packet.Data);

                        if (ipv4.PayloadPacket != null || !receiveData)
                            break;
                    }
                }

                if (packet != null && !receiveData)
                    break;

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
				DataModel buf = null;
                if (args.Count > 0)
                 buf = args[0].dataModel;

				if (method.Equals("Beacon"))
				{
					lock (mutex)
					{
						beacon = buf;

                        if (SecurityMode == SecurityModes.WPA2Aes || SecurityMode == SecurityModes.WPA2Tkip)
                        {
                            DataElement ssidElm = args[0].dataModel.find("SsidValue");
                            byte[] ssid;
                            if (ssidElm != null)
                            {
                                var temp = (BitStream)ssidElm.InternalValue;
                                ssid = new BitReader(temp).ReadBytes((int)temp.Length);
                            }
                            else
                            {
                                ssid = System.Text.Encoding.ASCII.GetBytes(Ssid);
                            }

                            var p = System.Text.Encoding.ASCII.GetBytes(Password);
                            wpa.GeneratePmk(p, ssid, 4096);
                        }
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
                else if (method.Equals("ActionBlockAckRequest"))
				{
					lock (mutex)
					{
						actionBlockAckReq = buf;
					}
				}
                else if (method.Equals("ActionBlockAckResponse"))
                {
                    lock (mutex)
                    {
                        actionBlockAckRep = buf;
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
                else if(method.Equals("ReceiveData"))
                {
                    receiveData = true;
                }
                else if (method.Equals("BlockAck"))
                {
                    lock (mutex)
                    {
                        blockAck = buf;
                    }
                }
                else if(method.Equals("DhcpNak"))
                {
                    lock (mutex)
                    {
                        dhcpNak = buf;
                    }
                }
                else if (method.Equals("DhcpAck"))
                {
                    lock (mutex)
                    {
                        dhcpAck = buf;
                    }
                }
                else if (method.Equals("DhcpOffer"))
                {
                    lock (mutex)
                    {
                        dhcpOffer = buf;
                    }
                }
                else if(method.Equals("Arp"))
                {
                    lock (mutex)
                    {
                        arp = buf;
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

        void SendPacket(DataModel model)
        {
            var buf = DataModelToBuf(model);

            lock (mutex)
            {
                sequenceNumber++;
            }

            if (buf != null)
                OnOutput(new BitStream(buf));
        }
         //Anonce is from AP
        //SNonce is from Client
        // PTK is generated from PMK, Anonce, SNonce
        // MIC is mac  of PTK

        //Pkcs5S2ParametersGenerator -> PMK
        //PTK = KDF-PTKLen(PMK-R1, "FT-PTK", SNonce || ANonce || BSSID || STA-ADDR)

        //PTK <- PRF-X(PMK,"Pairwise key expansion", (min(AA,SA) + Max(AA,SA) +Min(ANonce,SNonce) + Max(ANonce,SNonce))

        //ccmp = 128
        //tkip = 256

        void CalculateSequenceNumber(BitwiseStream bs, int off, int pos)
        {
            bs.Seek(off + 22, SeekOrigin.Begin);

            byte[] tmp = new byte[2];
            bs.Read(tmp, 0, 2);

            var cur = BitConverter.ToInt16(tmp, 0);
           
            UInt16 seq = (UInt16)(sequenceNumber + 1);
            seq = (UInt16)(seq << 4);
            seq = (UInt16)(seq & cur);

            bs.Seek(off + 22, SeekOrigin.Begin);

            var fin = BitConverter.GetBytes(seq);

            bs.Write(fin, 0, 2);

            bs.Seek(pos, SeekOrigin.Begin);
        }


        void StartAuthState()
        {
            var timeout = ApAuthTimeout;
            var sw = new Stopwatch();

            SendPacket(beacon);

            while (timeout > 0)
            {
                sw.Restart();

                var packet = airPcap.GetNextPacket();

                if (packet == null)
                {
                    timeout -= (int)sw.ElapsedMilliseconds;
                    continue;
                }

                int off;

                if (airPcap.AirPcapLinkType == AirPcapLinkTypes._802_11_PLUS_RADIO)
                    off = BitConverter.ToInt16(packet.Data, 2);
                else
                    off = 0;

                var type = (packet.Data[off] & 0x0F) >> 2;
                var subtype = packet.Data[off] >> 4;

                // Check to make sure it's from the correct target
                if (!AreEqual(packet.Data, off + 10, 6, target))
                       continue;
 
                if (!AreEqual(packet.Data, off + 4, 6, source) && !AreEqual(packet.Data, off + 4, 6, broadcast))
                    continue;

                _recvBuffer.SetLength(0);
                _recvBuffer.Write(packet.Data, 0, packet.Data.Length);
                _recvBuffer.Seek(0, SeekOrigin.Begin);

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

                                // Stop beacon thread so we can manage the sequence number
                                if (beacon != null)
                                {
                                    beaconThread.Join(0);
                                    beaconThread = null;
                                }

                                airPcap.Close();
                               
                                airPcap.Open(OpenFlags.MaxResponsiveness, 1);


                                if (SecurityMode == SecurityModes.None)
                                {
                                    SendPacket(acknowledgement);
                                    SendPacket(actionBlockAckReq);
                                }
                                else if (SecurityMode == SecurityModes.WPA2Aes || SecurityMode == SecurityModes.WPA2Tkip)
                                {
                                    DataElement aNonce = keymessage1.find("WpaKeyNonce");
                                    if (aNonce != null)
                                    {
                                        var bs = (BitStream)aNonce.DefaultValue;
                                        this.aNonce = new BitReader(bs).ReadBytes((int)bs.Length);
                                    }

                                    SendPacket(keymessage1);
                                }
                            }

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

                            var t = -1;

                            if (packet.Data.Length >= off + 25)
                             t = packet.Data[off + 25];

                            // check if the action is a request
                            if (t == 0)
                            {
                                byte token  = 0x00;
                                if (packet.Data.Length >= off + 26)
                                 token = packet.Data[off + 26];

                                DataElement DialogToken = actionBlockAckRep.find("DialogToken");
                                if (DialogToken != null)
                                {
                                    DialogToken.DefaultValue = new Variant(token);
                                }
                                
                                SendPacket(actionBlockAckRep);
                                break;
                            }

                            break;
                        default:
                            break;
                    }
                }
                else if (type == 1)
                {
                    switch (subtype)
                    {
                        case 8:
                            DataElement elm = blockAck.find("BlockAckStartingSequenceControl");
                            if (packet.Data.Length >= off + 22 && elm != null)
                            {
                                var tmp = new byte[2];
                                System.Array.Copy(packet.Data, off + 20, tmp, 0, 2);
                                elm.DefaultValue = new Variant(tmp);
                            }

                            SendPacket(blockAck);
                            break;
                        case 11:
                            SendPacket(clearToSend);
                            SendPacket(blockAck);

                            break;
                        default:
                            break;
                    }
                }
                else if (type == 2)
                {
                    DataElement elm;
                    switch (subtype)
                    {
                        case 4:
                            SendPacket(acknowledgement);
                            break;
                        case 8:
                            SendPacket(acknowledgement);

                            if (packet.Data.Length > off + 34 && BitConverter.ToUInt16(packet.Data, off + 32) == 0x8e88)
                            {
                                if (sNonce == null)
                                {
                                    if (packet.Data.Length >= off + 83)
                                    {
                                        sNonce = new byte[32];
                                        System.Array.Copy(packet.Data, off + 51, sNonce, 0, 32);

                                        wpa.GeneratePtk(source, target, aNonce, sNonce);
                                    }

                                    elm = keymessage3.find("WpaKeyMic");
                                    DataElement Authentication = keymessage3.find("Authentication");
                                    if (elm != null && Authentication != null)
                                    {
                                        var mac = new HMac(new Sha1Digest());
                                        var ret = new byte[mac.GetMacSize()];
                                        var bs = Authentication.Value;
                                        var pos = bs.Position;

                                        CalculateSequenceNumber(bs, off, (int)pos);
                                       
                                        var buf = new BitReader(bs).ReadBytes((int)bs.Length);
                                        var key = new KeyParameter(wpa.Kck);


                                        mac.Init(key);
                                        mac.BlockUpdate(buf, 0, buf.Length);
                                        mac.DoFinal(ret, 0);


                                        var r = new byte[16];
                                        System.Array.Copy(ret, 0, r, 0, 16);
                                        elm.DefaultValue = new Variant(r);
                                    }

                                    SendPacket(keymessage3);
                                }

                                break;
                            }

                            var udp = UdpPacket.ParsePacket(packet.LinkLayerType, packet.Data);

                            if (udp.PayloadPacket == null)
                            {
                                break;
                            }

                            var dhcp = new Packet(packet.Data);
                            dhcp.ParseDhcp(26);

                            var tmp = new byte[4];
                            if (packet.Data.Length > off + 70)
                            {
                                System.Array.Copy(packet.Data, off + 66, tmp, 0, 4);
                            }

                            switch (dhcp.Type)
                            {
                                case 1:
                                    elm = dhcpOffer.find("TransactionId");
                                    if (elm != null)
                                        elm.DefaultValue = new Variant(tmp);

                                    SendPacket(arp);
                                    SendPacket(dhcpOffer);

                                    DhcpState = DhcpStates.SentOffer;

                                    return;
                                case 3:
                                    if (DhcpState == DhcpStates.None)
                                    {
                                        elm = dhcpNak.find("TransactionId");
                                        if (elm != null)
                                            elm.DefaultValue = new Variant(tmp);

                                        SendPacket(dhcpNak);
                                        DhcpState = DhcpStates.FirstReply;
                                    }
                                    else
                                    {
                                        elm = dhcpAck.find("TransactionId");
                                        if (elm != null)
                                            elm.DefaultValue = new Variant(tmp);

                                        SendPacket(dhcpAck);

                                        return;
                                    }

                                    break;
                                default:
                                    break;
                            }

                            break;
                        default:
                            break;
                    }
                }

                timeout -= (int)sw.ElapsedMilliseconds;
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
                    if (beacon != null)
                    {
                        SendPacket(beacon);
                        Thread.Sleep(125);
                    }
                }
               catch (SharpPcap.DeviceNotReadyException)
               {
                    return;
               }
            }
        }

        #region Helper Classes

        class Packet
        {
            byte[] packet;

            public int Length { get; set; }
            public int Type { get; set; }

            public Packet(byte[] packet)
            {
                this.packet = packet;
                Length = packet.Length;
            }

            public void ParseDhcp(int len)
            {
                Type = -1;
                if (len > Length)
                    return;

                //radio header len
                len += BitConverter.ToInt16(packet, 2);

                //8 bytes for llc
                len += 8;

                if (len > Length)
                    return;

                //ipv4
                if (packet[len] != 0x45)
                    return;
    
                len += (packet[len] >> 4) * 5;

                //udp
                len += 8;

                if (len + 242 > Length)
                    return;

                Type = packet[len + 242];
            }
        }

        class Wpa
        {
            MemoryStream ms;
            byte[] pmk;

            public Wpa()
            {
                ms = new MemoryStream();
            }

            public byte[] GeneratePmk(byte[] password, byte[] salt, int iterations)
            {
                //var ret = new MemoryStream();

                Pkcs5S2ParametersGenerator gen = new Pkcs5S2ParametersGenerator();

                gen.Init(password, salt, iterations);
                KeyParameter macParameters = (KeyParameter)gen.GenerateDerivedMacParameters(256);

                pmk = macParameters.GetKey();

                return pmk;
            }

            public void GeneratePtk(byte[] pmk, byte[] aa, byte[] spa, byte[] aNonce, byte[] sNonce)
            {

                var label = System.Text.Encoding.ASCII.GetBytes("Pairwise key expansion");
                var b = new MemoryStream();
                b.Write(Min(aa, spa), 0, aa.Length);
                b.Write(Max(aa, spa), 0, aa.Length);
                b.Write(Min(aNonce, sNonce), 0, aNonce.Length);
                b.Write(Max(aNonce, sNonce), 0, aNonce.Length);
                b.Seek(0, SeekOrigin.Begin);

                PRF(pmk, label, b.ToArray(), 512);
            }

            public void GeneratePtk(byte[] aa, byte[] spa, byte[] aNonce, byte[] sNonce)
            {

                var label = System.Text.Encoding.ASCII.GetBytes("Pairwise key expansion");
                var b = new MemoryStream();
                b.Write(Min(aa, spa), 0, aa.Length);
                b.Write(Max(aa, spa), 0, aa.Length);
                b.Write(Min(aNonce, sNonce), 0, aNonce.Length);
                b.Write(Max(aNonce, sNonce), 0, aNonce.Length);
                b.Seek(0, SeekOrigin.Begin);

                PRF(pmk, label, b.ToArray(), 512);
            }

            public byte[] Kck
            {
                get
                {
                    var ret = new byte[16];
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.Read(ret, 0, 16);

                    return ret;
                }
            }

            public int Length
            {
                get
                {
                    return (int)ms.Length;
                }
            }

            public byte[] Kek
            {
                get
                {
                    var ret = new byte[16];
                    ms.Seek(16, SeekOrigin.Begin);
                    ms.Read(ret, 0, 16);

                    return ret;
                }
            }

            public byte[] Tk
            {
                get
                {
                    var ret = new byte[16];
                    ms.Seek(32, SeekOrigin.Begin);
                    ms.Read(ret, 0, 16);

                    return ret;
                }
            }

            public byte[] AuthMic
            {
                get
                {
                    var ret = new byte[8];
                    ms.Seek(48, SeekOrigin.Begin);
                    ms.Read(ret, 0, 8);

                    return ret;
                }
            }

            public byte[] SupMic
            {
                get
                {
                    var ret = new byte[8];
                    ms.Seek(56, SeekOrigin.Begin);
                    ms.Read(ret, 0, 8);

                    return ret;
                }
            }

            public byte[] ToArray()
            {
                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            }

            public void PRF(byte[] key, byte[] label, byte[] b, int size)
            {
                var k = new KeyParameter(key);

                HMac mac = new HMac(new Sha1Digest());
                mac.Init(k);

                ms.Seek(0, SeekOrigin.Begin);

                for (byte i = 0; i < (size + 159) / 160; i++)
                {
                    var sha = new byte[mac.GetMacSize()];

                    mac.BlockUpdate(label, 0, label.Length);
                    mac.Update(0x00);
                    mac.BlockUpdate(b, 0, b.Length);
                    mac.Update(i);

                    mac.DoFinal(sha, 0);
                    mac.Reset();

                    ms.Write(sha, 0, sha.Length);
                }

                ms.Seek(0, SeekOrigin.Begin);
                ms.SetLength(size / 8);
            }

            byte[] Max(byte[] a, byte[] b)
            {
                if (a.Length != b.Length)
                    throw new ArgumentException("The two arrays are not the same size.");

                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] > b[i])
                        return a;
                    if (b[i] > a[i])
                        return b;
                }

                return a;
            }

            byte[] Min(byte[] a, byte[] b)
            {
                if (a.Length != b.Length)
                    throw new ArgumentException("The two arrays are not the same size.");

                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] < b[i])
                        return a;
                    if (b[i] < a[i])
                        return b;
                }

                return a;
            }

        }

        #endregion
    }
}
