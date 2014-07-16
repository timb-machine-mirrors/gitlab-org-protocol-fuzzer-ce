using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
	[Parameter("Interface", typeof(string), "Interface to use", "wlan0")]
	[Parameter("CaptureMode", typeof(DeviceMode), "Capture mode to use (Promiscuous, Normal)", "Promiscuous")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	[Parameter("ApAuthTimeout", typeof(int), "How many milliseconds to wait for the AP auth state to complete (default 120000)", "120000")]
	[Parameter("SecurityMode", typeof(SecurityModes), "802.11 security mode to use", "None")]
	[Parameter("TargetMac", typeof(HexString), "MAC address of the target")]
	[Parameter("SourceMac", typeof(HexString), "MAC address of the host machine")]
	[Parameter("Password", typeof(string), "WPA/WEP Password", "")]
	[Parameter("Ssid", typeof(string), "Ssid of the network", "")]
	[Parameter("Channel", typeof(uint), "Wireless channel to send/listen for packets on", "11")]
	public class WifiPublisher : RadiotapPublisher
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
		DataModel arpResponse = null;

		//WPA2 Data
		Wpa wpa = null;
		byte[] aNonce = null;
		byte[] sNonce = null;

		static byte[] broadcast = new byte[6] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
		static byte[] target;
		static byte[] source;

		bool receiveData;

		UInt16 sequenceNumber = 0;

		object mutex = new object();

		Thread beaconThread = null;
		bool beaconThreadStop = false;
		
		Thread associateThread = null;
		bool associateThreadStop = false;
		AutoResetEvent associateThreadReady = new AutoResetEvent(false);
		SoftException associateException = null;
		
		void compile()
		{
			SendPacket(probeRequest, false, false);
			SendPacket(associationRequest, false, false);
			SendPacket(reassociationRequest, false, false);
			SendPacket(atim, false, false);
			SendPacket(powerSavePoll, false, false);
			SendPacket(readyToSend, false, false);
			SendPacket(cfEnd, false, false);
			SendPacket(keymessage2, false, false);
			SendPacket(keymessage4, false, false);
			SendPacket(cfEndCfAck, false, false);
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
			associateException = null;
			associateThreadStop = false;
			beaconThreadStop = false;
			
			aNonce = null;
			sNonce = null;

			sequenceNumber = 0;

			wpa = new Wpa();

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

			ResetState();

			beaconThread = new Thread(BeaconThread);
			beaconThread.Start();
		}

		protected override void OnStop()
		{
			if (beaconThread != null)
			{
				beaconThreadStop = true;
				
				if(!beaconThread.Join(1000))
				{
					beaconThread.Abort();
					beaconThread.Join();
				}
				
				beaconThread = null;
			}
			
			if (associateThread != null)
			{
				associateThreadStop = true;
				
				if(!associateThread.Join(1000))
				{
					associateThread.Abort();
					associateThread.Join();
				}
				
				associateThread = null;
			}
			
			base.OnStop();
		}

		protected override void OnInput()
		{
			var timeout = Timeout;
			var sw = new Stopwatch();
			sw.Restart();

			RawCapture packet = null;
			while (timeout > 0)
			{
				packet = pcap.GetNextPacket();

				if (packet != null && receiveData)
				{
					var off = 0;

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
				throw new SoftException("Didn't receive a packet before the timeout expired.");

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
					associateThread = new Thread(StartAuthState);
					associateThread.Start();
					associateThreadReady.WaitOne();
				}
				else if (method.Equals("WaitForAssociation"))
				{
					associateThread.Join();
					
					if(associateException != null)
						throw associateException;
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
				else if(method.Equals("ArpResponse"))
				{
					lock (mutex)
					{
						arpResponse = buf;
					}
				}
				else if(method.Equals("Cache"))
				{
					// Cache values
					var value = acknowledgement.Value;
					value = auth.Value;
					value = associationResponse.Value;
					value = dhcpNak.Value;
					value = dhcpAck.Value;
					value = dhcpOffer.Value;
				}
				else
				{
					logger.Error("Unknown call method: "+method);
				}
			}
			catch (SoftException)
			{
				throw;
			}
			catch (Exception ex)
			{
				logger.Debug("Exception: {0}", ex.Message);
				throw new PeachException(ex.Message, ex);
			}

			return null;
		}

		void SendPacket(DataModel model, bool updateSequence, bool retry)
		{
			// update squence number
			if(updateSequence)
			{
				lock (mutex)
				{
					sequenceNumber++;
				}
				
				try
				{
					var field = ((model["MacHeaderFrame"] as DataElementContainer)
						["FragmentSequenceNumber"] as DataElementContainer)
						["SequenceNumber"];
					field.DefaultValue = new Variant(sequenceNumber);
				}
				catch
				{}
			}
			
			var data = model.Value;
			
			// Clear receive queue
			//RawCapture packet;
			//while(captureQueue.TryDequeue(out packet));
			
			// Send data
			OnOutput(data);
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

		void CalculateSequenceNumber(BitwiseStream bs, int off = 0, int pos = 22)
		{
			bs.Seek(off + pos, SeekOrigin.Begin);

			byte[] tmp = new byte[2];
			bs.Read(tmp, 0, 2);

			var cur = BitConverter.ToInt16(tmp, 0);
           
			UInt16 seq = (UInt16)(sequenceNumber + 1);
			seq = (UInt16)(seq << 4);
			seq = (UInt16)(seq & cur);

			bs.Seek(off + pos, SeekOrigin.Begin);

			var fin = BitConverter.GetBytes(seq);

			bs.Write(fin, 0, 2);

			bs.Seek(pos, SeekOrigin.Begin);
		}
		
		System.Diagnostics.Stopwatch receiveStopWatch = new System.Diagnostics.Stopwatch();
		public void ReceivePacket(out RawCapture packet, out int off, out int type, out int subtype)
		{
			packet = null;
			receiveStopWatch.Restart();
			
			while((int)receiveStopWatch.ElapsedMilliseconds < ApAuthTimeout)
			{
				packet = pcap.GetNextPacket();
				
				if (packet == null)
					continue;
				
				// skip radiotap header
				off = BitConverter.ToInt16(packet.Data, 2);
				
				// Check to make sure it's from the correct target
				if (!AreEqual(packet.Data, off + 10, 6, target))
					continue;
				
				if (!AreEqual(packet.Data, off + 4, 6, source) && !AreEqual(packet.Data, off + 4, 6, broadcast))
					continue;
				
				type = (packet.Data[off] & 0x0F) >> 2;
				subtype = packet.Data[off] >> 4;
				
				return;
			}
			
			throw new SoftException("Unable to complete 802.11 association (timeout).");
			
			// Is this needed?
			//_recvBuffer.SetLength(0);
			//_recvBuffer.Write(packet.Data, 0, packet.Data.Length);
			//_recvBuffer.Seek(0, SeekOrigin.Begin);
		}
		
		void EmptyPacketQueue()
		{
			while(true)
			{
				if(pcap.GetNextPacket() == null)
					return;
			}
		}
		
		enum WifiState
		{
			Beacon,
			ProbeResponse,
			//AuthResponse, - Same as ProbeResponse state
			AssociateResponse,
			//ActionRequest,
			Unknown
		}

		void StartAuthState()
		{
			int off;
			int type;
			int subtype;
			bool associated = false;
			WifiState wifiState = WifiState.ProbeResponse;
			
			RawCapture packet = null;
			EmptyPacketQueue();
			
			try
			{
				while (!associateThreadStop)
				{
					switch(wifiState)
					{
						case WifiState.ProbeResponse:
							logger.Debug("WifiState.ProbeResponse");
							
							associateThreadReady.Set();
							
							while(!associateThreadStop)
							{
								ReceivePacket(out packet, out off, out type, out subtype);
								
								if(type == 0 && subtype == 4 && !AreEqual(packet.Data, off + 4, 6, broadcast))
								{
									SendPacket(probeResponse, true, true);
								}
								else if(type == 0 && subtype == 4)
								{
									SendPacket(probeResponse, true, false);
								}
								else if(type == 0 && subtype == 11)
								{
									SendPacket(acknowledgement, false, false);
									
									EmptyPacketQueue();
									
									SendPacket(auth, false, true);
									wifiState = WifiState.AssociateResponse;
									
									// Stop sending beacon until next iteration
									beacon = null;
									
									break;
								}
							}
							
							break;
						
						case WifiState.AssociateResponse:
							logger.Debug("WifiState.AssociateResponse");
							
							do
							{
								ReceivePacket(out packet, out off, out type, out subtype);
							}
							while(!(type == 0 && subtype == 0) && !associateThreadStop);
							
							SendPacket(acknowledgement, false, false);
							EmptyPacketQueue();
							SendPacket(associationResponse, false, true);
							
							if (SecurityMode == SecurityModes.None)
							{
								SendPacket(actionBlockAckReq, false, true);
							}
							else if (SecurityMode == SecurityModes.WPA2Aes || SecurityMode == SecurityModes.WPA2Tkip)
							{
								pcap.Close();
								pcap.Open(CaptureMode, 1);
								
								DataElement aNonce = keymessage1.find("WpaKeyNonce");
								if (aNonce != null)
								{
									var bs = (BitStream)aNonce.DefaultValue;
									this.aNonce = new BitReader(bs).ReadBytes((int)bs.Length);
								}
								
								SendPacket(keymessage1, true, true);
							}
							
							wifiState = WifiState.Unknown;
							
							break;
						
						case WifiState.Unknown:
							logger.Debug("WifiState.Unknown");
							
							while(!associateThreadStop)
							{
								ReceivePacket(out packet, out off, out type, out subtype);
								
								logger.Debug("T: "+ type+" S: "+subtype);
								
								if (type == 0)	// Mgmt Frame
								{
									switch (subtype)
									{
										case 2:
											SendPacket(reassociationResponse, true, true);
											break;
										
										case 13:	// Action
											SendPacket(acknowledgement, false, false);
											
											// Respond to requests
											if (packet.Data[off + 25] == 0)
											{
												try
												{
													(((actionBlockAckRep["Body"] as DataElementContainer)
														["FixedParameters"] as DataElementContainer)
														["BlockAck"] as DataElementContainer)
														["DialogToken"].DefaultValue = new Variant(packet.Data[off + 26]);
													
													(actionBlockAckRep["MacHeaderFrame"] as DataElementContainer)
														["BlockAckStartingSequenceControl"].DefaultValue = new Variant(3);
												}
												catch
												{}
												
												SendPacket(actionBlockAckRep, false, true);
											}
											
											break;
										default:
											break;
									}
								}
								else if (type == 1)	// Control Frame
								{
									switch (subtype)
									{
										case 8:		// Block ACK Request
											
											if (packet.Data.Length >= off + 22)
											{
												try
												{
													var tmp = new byte[2];
													System.Array.Copy(packet.Data, off + 20, tmp, 0, 2);
													
													(blockAck["MacHeaderFrame"] as DataElementContainer)
														["BlockAckStartingSequenceControl"].DefaultValue = new Variant(tmp);
												}
												catch
												{}
											}
											
											SendPacket(blockAck, false, false);
											break;
										
										case 11:	// Request-to-send
											SendPacket(clearToSend, false, false);
											SendPacket(blockAck, false, false);
											
											break;
										default:
											break;
									}
								}
								else if (type == 2) // Data Frame
								{
									// Once we are here, association passed
									associated = true;
									
									DataElement elm;
									switch (subtype)
									{
										case 4:	// Null function (No Data)
											
											SendPacket(acknowledgement, false, false);
											break;
										
										case 8:	// Actual data!!
											
											SendPacket(acknowledgement, false, false);
												
											var rdr = new BitReader(new BitStream(packet.Data));
											rdr.BaseStream.Position = off + 32;
											rdr.BigEndian();
											var llc = rdr.ReadUInt16();
											
											if (packet.Data.Length > off + 34 && llc == 0x888e)
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
							
													SendPacket(keymessage3, false, true);
												}
							
												break;
											}
											
											// Respond to ARP
											if(llc == 0x0806 && DhcpState == DhcpStates.SentOffer)
											{
												SendPacket(arpResponse, true, true);
												
												// At this point we are done!
												return;
												//break;
											}
											
											// Only respond to IPv4 packets
											if (llc != 0x0800)
												break;
											
											var dhcp = new Packet(packet.Data);
											dhcp.ParseDhcp(26);
							
											var tmp = new byte[4];
											if (dhcp.packet.Length > 8)
											{
												System.Array.Copy(dhcp.packet, 4, tmp, 0, 4);
											}
							
											switch (dhcp.Type)
											{
												case 1:
													elm = dhcpOffer.find("TransactionId");
													if (elm != null)
														elm.DefaultValue = new Variant(tmp);
													
													SendPacket(arp, true, true);
													SendPacket(dhcpOffer, true, true);
													
													DhcpState = DhcpStates.SentOffer;
													
													break;
												
												case 3:
													if (DhcpState == DhcpStates.None)
													{
														elm = dhcpNak.find("TransactionId");
														if (elm != null)
															elm.DefaultValue = new Variant(tmp);
							
														SendPacket(dhcpNak, true, true);
														DhcpState = DhcpStates.FirstReply;
													}
													else
													{
														elm = dhcpAck.find("TransactionId");
														if (elm != null)
															elm.DefaultValue = new Variant(tmp);
							
														if (DhcpState == DhcpStates.SentOffer || DhcpState == DhcpStates.None)
														{
															SendPacket(dhcpAck, true, true);
															
															//return;
															break;
														}
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
							}
							
							break;
					}
				}
			}
			catch(SoftException se)
			{
				if(associated)
					return;
				
				associateException = se;
				
				// NOTE: This is a thread, don't exception!
			}
		}

		void BeaconThread()
		{   
			while (!beaconThreadStop)
			{
				try
				{
					if (beacon != null)
					{
						lock (mutex)
						{
							sequenceNumber++;
						}
						
						// update squence number
						try
						{
							var field = ((beacon["MacHeaderFrame"] as DataElementContainer)
								["FragmentSequenceNumber"] as DataElementContainer)
								["SequenceNumber"];
							field.DefaultValue = new Variant(sequenceNumber);
						}
						catch
						{}
						
						pcap.SendPacket(DataModelToBuf(beacon));
						//SendPacket(beacon, true);
						Thread.Sleep(125);
					}
				}
				catch (SharpPcap.DeviceNotReadyException de)
				{
					logger.Error(de.Message);
					associateException = new SoftException(de.Message, de);
					associateThreadStop = true;
					return;
				}
				catch(SharpPcap.PcapException pe)
				{
					logger.Error(pe.Message);
					associateException = new SoftException(pe.Message, pe);
					associateThreadStop = true;
					return;
				}
			}
			
			beaconThreadStop = false;
		}

		#region Helper Classes

		class Packet
		{
			public byte[] packet;

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
				{
					logger.Warn("lan > Length #1");
					return;
				}

				//radio header len
				len += BitConverter.ToInt16(packet, 2);

				//8 bytes for llc
				len += 8;

				if (len > Length)
				{
					logger.Warn("len > Length #2");
					return;
				}

				//ipv4
				if (packet[len] != 0x45)
				{
					logger.Warn("packet[len] != 0x45");
					return;
				}
    
				len += (packet[len] >> 4) * 5;

				//udp Check if port is 67 or 68
				var port = BitConverter.ToInt16(packet, len);
				if (BitConverter.IsLittleEndian)
					port = System.Net.IPAddress.NetworkToHostOrder(port);

				if (port != 0x0044 && port != 0x0043)
				{
					logger.Warn("port != 0x0044 && port != 0x0043");
					return;
				}

				var dhcpPackLen = BitConverter.ToInt16(packet, len+4);
				if(BitConverter.IsLittleEndian)
					dhcpPackLen = System.Net.IPAddress.NetworkToHostOrder(dhcpPackLen);
				len += 8;

				if (len + 242 > Length)
				{
					logger.Warn("len + 242 > Length");
					return;
				}

				var tmp = new byte[dhcpPackLen-4];
				Type = packet[len + 242];
				
				System.Array.Copy(packet, len, tmp, 0, packet.Length - len);
				packet = tmp;
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
