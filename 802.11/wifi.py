
'''
AirPcap publisher for wifi fuzzing.

@author: Michael Eddington
@version: $Id$
'''

#
# Copyright (c) Michael Eddington
#
# Permission is hereby granted, free of charge, to any person obtaining a copy 
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights 
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
# copies of the Software, and to permit persons to whom the Software is 
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in	
# all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
# SOFTWARE.
#

# Authors:
#   Michael Eddington (mike@phed.org)

# $Id$

from Peach.fixup import Fixup

import urllib, urllib2, httplib, time

class AMTSequenceIncrementFixup(Fixup):
    '''
    Allows a field to emit a sequential value without adding additional test cases.
    This is useful for sequence numbers common in network protocols.
    '''
    
    num = 1;

    def __init__(self):
        Fixup.__init__(self)        
            
    def fixup(self):
        AMTSequenceIncrementFixup.num = (AMTSequenceIncrementFixup.num + 1) % 256
        return AMTSequenceIncrementFixup.num

import sys, os, threading, time, struct
from Peach.publisher import *

try:
	from ctypes import *
	
	class pcap_pkthdr(Structure):
		'''
		struct pcap_pkthdr {
			struct timeval ts;	/* time stamp */
			bpf_u_int32 caplen;	/* length of portion present */
			bpf_u_int32 len;	/* length this packet (off wire) */
		};
		'''
		_fields_ = [
			('ts', c_uint64),
			('caplen', c_uint),
			('len', c_uint)
			]
	
except:
	pass

class bpf_program(Structure):
	_fields_ = [
		("bf_len", c_uint),
		("bpf_insn", c_void_p)
	]

class AmtWifi(Publisher):
	'''
	AirPcap I/O inteface.  Supports sending beacons and standard I/O.
	'''
	
	PCAP_ERRBUF_SIZE = 256
	AIRPCAP_LT_802_11 = 1				# plain 802.11 link type. Every packet in the buffer contains the raw 802.11 frame, including MAC FCS.
	AIRPCAP_LT_802_11_PLUS_RADIO = 2	# 802.11 plus radiotap link type. Every packet in the buffer contains a radiotap header followed by the 802.11 frame. MAC FCS is included.
	AIRPCAP_LT_UNKNOWN = 3				# Unknown link type. You should see it only in case of error.
	AIRPCAP_LT_802_11_PLUS_PPI = 4		# 802.11 plus PPI header link type. Every packet in the buffer contains a PPI header followed by the 802.11 frame. MAC FCS is included.
	
	def __init__(self, channel = 5,
				mac = "\xca\xce\xca\xce\xca\xce",
				device = "\\\\.\\airpcap00",
				target_mac = None):
		Publisher.__init__(self)
		self.withNode = True
		self.mac = mac
		self.target_mac_hex = target_mac
		self.device = device
		self.channel = channel
		self.pcap = None
		self.air = None
		self.beacon = None
		self.beaconThread = None
		self.beaconStopEvent = None
		self.probe = None
		self.association = None
		
		# Don't initalize here to avoid a deepcopy
		# issue!
		#self.beaconStopEvent = threading.Event()
		#self.beaconStopEvent.clear()
	
	def start(self):
		if self.beaconStopEvent == None:
			self.beaconStopEvent = threading.Event()
		
		errbuff = c_char_p("A"*self.PCAP_ERRBUF_SIZE) # Must pre-alloc memory for error message
		self.pcap = cdll.wpcap.pcap_open_live(self.device, 65536, 1, 1, errbuff)
		
		if self.pcap == 0:
			raise Exception( errbuff.value )
		
		self.air = cdll.wpcap.pcap_get_airpcap_handle(self.pcap)
		cdll.airpcap.AirpcapSetDeviceChannel(self.air, self.channel)
		cdll.airpcap.AirpcapSetLinkType(self.air, self.AIRPCAP_LT_802_11)
		
		filter_prog = bpf_program()
		filter_expression = "wlan src %s and (wlan dst ca:ce:ca:ce:ca:ce or wlan dst ff:ff:ff:ff:ff:ff)" % self.target_mac_hex
		print filter_expression
		dead_pcap = cdll.wpcap.pcap_open_dead(105, 65536)
		r = cdll.wpcap.pcap_compile(dead_pcap, pointer(filter_prog), filter_expression, 0, 0xFFFFFFFF)
		if r == -1:
			cdll.wpcap.pcap_perror(dead.pcap, '')
		cdll.wpcap.pcap_setfilter(self.pcap, pointer(filter_prog))
		self.beaconStopEvent.clear()
		cdll.wpcap.pcap_close(dead_pcap)
	
	def stop(self):
		if self.beaconThread != None:
			self.beaconStopEvent.set()
			self.beaconThread.join()
			self.beaconThread = None
			self.beaconStopEvent.clear()
			self.beacon = None
		if self.pcap != None:
			cdll.wpcap.pcap_close(self.pcap)
			self.pcap = None
	
	def sendWithNode(self, data, dataNode):
		cdll.wpcap.pcap_sendpacket(self.pcap, data, len(data))

	def send(self, data):
		cdll.wpcap.pcap_sendpacket(self.pcap, data, len(data))
	
	def wifi_off(self, host, user, passw):
		passmgr = urllib2.HTTPPasswordMgrWithDefaultRealm();
		passmgr.add_password(None, "http://"+host+":16992", user, passw)
		
		auth_handler = urllib2.HTTPDigestAuthHandler(passmgr)
		opener = urllib2.build_opener(auth_handler)
		urllib2.install_opener(opener)
		
		## Turn updates off
		
		req = urllib2.Request("http://" + host + ":16992/wlan.htm")
		fd = urllib2.urlopen(req)
		data = fd.read()
		fd.close()
		
		data = data[data.find('NAME="t" value="') + len('NAME="t" value="'):]
		data = data[:data.find('"')]
		data = "t=%s&WlanToggleOption=0_disabled" % (urllib.quote(data))
		
		req = urllib2.Request("http://" + host + ":16992/togglewlan",
							data, {"Content-Type" : "application/x-www-form-urlencoded"})
		fd = urllib2.urlopen(req)
		data = fd.read()
		fd.close()

	def wifi_on(self, host, user, passw):
		## Turn Updates on
		
		passmgr = urllib2.HTTPPasswordMgrWithDefaultRealm();
		passmgr.add_password(None, "http://"+host+":16992", user, passw)
		
		auth_handler = urllib2.HTTPDigestAuthHandler(passmgr)
		opener = urllib2.build_opener(auth_handler)
		urllib2.install_opener(opener)

		req = urllib2.Request("http://" + host + ":16992/wlan.htm")
		fd = urllib2.urlopen(req)
		data = fd.read()
		fd.close()
		
		data = data[data.find('NAME="t" value="') + len('NAME="t" value="'):]
		data = data[:data.find('"')]
		data = "t=%s&WlanToggleOption=2_sx_s0" % (urllib.quote(data))
		
		req = urllib2.Request("http://" + host + ":16992/togglewlan",
							  data, {"Content-Type" : "application/x-www-form-urlencoded"})
		fd = urllib2.urlopen(req)
		data = fd.read()
		fd.close()
	
	def receive(self, size = None):
		'''
		Receive some data.
		
		@type	size: integer
		@param	size: Number of bytes to return
		@rtype: string
		@return: data received
		'''
		
		cdll.wpcap.pcap_next_ex.argtypes = [
			c_void_p,
			POINTER(POINTER(pcap_pkthdr)),
			POINTER(POINTER(c_ubyte))
			]
		
		header = pointer(pcap_pkthdr())
		header.len = 0
		header.caplen = 0
		pkt_data = POINTER(c_ubyte)()

		authResponses = []
		probeResponses = []
		assocResponses = []
		authCnt = 0
		probeCnt = 0
		assocCnt = 0

		print "Pre-generating packets"

		for i in range(256):
			authResponses.append(self.authNode.getValue())
			probeResponses.append(self.probeNode.getValue())
			assocResponses.append(self.associationNode.getValue())

		self.wifi_on('10.0.1.28', 'admin', 'Admin!98')

		print "Starting Wi-Fi state loop"
		last_ptype = None
		last_sn = None
		ptype = None
				
		test_start = time.time()
		
		try:
			while True:
				if time.time() - test_start > 10:
					print "Timeout!"
					break
				res = cdll.wpcap.pcap_next_ex(self.pcap, byref(header), byref(pkt_data))
				#print "pcap_next_ex", res
				if res < 0:
					# error reading packets
					error = cdll.wpcap.pcap_geterr(self.pcap)
					print "!!! ERROR:", error
					raise Exception(error)
				
				elif res != 1:
					# timeout hit
					continue
				
				# Convert byte array to binary string
				#data = r''.join([chr(b) for b in pkt_data[:header.contents.len]])
				#dest_mac = data[4:10]
				#src_mac = data[10:16]
				#ptype = data[0]
				#ptype_int = ord(ptype)
				
				# If frame is replay, skip
				#if pkt_data[1] == 0x08:
				#	continue
				
				#ptype = chr(pkt_data[0])
				ptype_int = pkt_data[0]
				
				sn = pkt_data[19:2]
				print "sn", sn
				#if sn == last_sn:
				#	continue
				last_sn = sn
				
				print ">> Pkt type: %2x -- Flags: %2x" % (ptype_int, pkt_data[1])
				
				# Is probe request?
				if ptype_int == 0x40:
					print ">> Sending Probe Response"
					self.send(probeResponses[probeCnt])
					probeCnt += 1
					if probeCnt == len(probeResponses):
						probeCnt = 0
				
				# Auth request
				elif ptype_int == 0xb0:
					print ">> Sending Auth Response"
					self.send(authResponses[authCnt])
					authCnt += 1
					if authCnt == len(authResponses):
						authCnt = 0
	
				# Is Association request?
				elif ptype_int == 0x00:
					print ">> Sending Association Response"
					self.send(assocResponses[assocCnt])
					assocCnt += 1
					if assocCnt == len(assocResponses):
						assocCnt = 0
						
					break  #ok, good enough
			
			print 'Test compete in %f s.' % (time.time() - test_start)
			
			if hasattr(self, "publisherBuffer"):
				self.publisherBuffer.haveAllData = True
			
			self.wifi_off('10.0.1.28', 'admin', 'Admin!98')
			return ''
		
		except:
			print "EXCEPTION BITCHES!!!", sys.exc_info()
	
	def _sendBeacon(self):
		while not self.beaconStopEvent.isSet():
			self.beacon = self.beaconNode.getValue()
			cdll.wpcap.pcap_sendpacket(self.pcap, self.beacon, len(self.beacon))
			time.sleep(0.1)
	
	def _startBeacon(self):
		print "STARTING BEACON THREAD"

		if self.beacon == None:
			return
		if self.beaconThread != None:
			return
		
		self.beaconThread = threading.Thread(target=self._sendBeacon)
		self.beaconThread.start()
	
	def callWithNode(self, method, args, argNodes):
	#def call(self, method, args):
		
		if method == 'beacon':
			self.beacon = args[0]
			self.beaconNode = argNodes[0]
			self._startBeacon()
		elif method == 'probe':
			print ">> Setting probe"
			self.probe = args[0]
			self.probeNode = argNodes[0]
			self.hexPrint(self.probe)
		elif method == 'authentication':
			print ">> Setting authentication"
			self.auth = args[0]
			self.authNode = argNodes[0]
			self.hexPrint(self.auth)
		elif method == 'association':
			print ">> Setting association"
			self.association = args[0]
			self.associationNode = argNodes[0]
			self.hexPrint(self.association)
		
	
	def connect(self):
		'''
		Called to connect or open a connection/file.
		'''
		pass
		
	def close(self):
		'''
		Close current stream/connection.
		'''
		pass

# end


