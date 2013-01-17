'''
Peach Extensions for Intel AMT 6.1 Fuzzing

@author: Michael Eddington
'''

#
# Copyright (c) Deja vu Security
#

# Authors:
#   Eric Rachner (eric@dejavusecurity.com)

import operator
import zlib, hashlib, struct, binascii, array
from Peach.fixup import Fixup
from Peach.Engine.common import *

class Icmpv6ChecksumFixup(Fixup):
	'''
	ICMPv6 Chucksum Fixup implemented per RFC 2463:

	   The checksum is the 16-bit one's complement of the one's complement
	   sum of the entire ICMPv6 message starting with the ICMPv6 message
	   type field, prepended with a "pseudo-header" of IPv6 header fields,
	   as specified in [RFC 2460, section 8.1].  The Next Header value used in
	   the pseudo-header is 58.  (NOTE: the inclusion of a pseudo-header in
	   the ICMPv6 checksum is a change from IPv4; see RFC 2460 for the
	   rationale for this change.)

	   For computing the checksum, the checksum field is set to zero.	
	'''

	def __init__(self, ref):
		Fixup.__init__(self)
		self.ref = ref
	
	def _checksum(self, checksum_input):
				
		# add byte if not dividable by 2
		if len(checksum_input) & 1:
			checksum_input = checksum_input + '\0'

		# split into 16-bit word and insert into a binary array
		words = array.array('h', checksum_input)
		sum = 0

		# perform ones complement arithmetic on 16-bit words
		for word in words:
			sum += (word & 0xffff) 
		
		hi = sum >> 16 
		lo = sum & 0xffff 
		sum = hi + lo
		sum = sum + (sum >> 16)
		
		return (~sum) & 0xffff # return ones complement
	
	def fixup(self):
		self.context.defaultValue = "0"
		node = self._findDataElementByName(self.ref)

		#structure is:
		#   srcaddr (16 bytes)
		#   dstaddr (16 bytes)
		#   length  (4 bytes, length of payload)
		#   tail    (4 bytes: three nulls and a fixed type value)
		
		nodeSourceAddress = node.find("SourceAddress").getValue()
		nodeDestAddress = node.find("DestinationAddress").getValue()
		payloadLength = node.find("Icmpv6_EchoRequest").getSize()
		
		#print "nodePayloadLength = " , payloadLength
		tail = struct.pack("!I", 58)
		
		stuff = nodeSourceAddress + nodeDestAddress + struct.pack("!I",payloadLength) + tail + node.find("Icmpv6_EchoRequest").getValue()
		#print "length of stuff   = " , len(stuff)
		
		if stuff == None:
		    raise Exception("Error: Icmpv6ChecksumFixup was unable to locate [%s]" % self.ref)

		return self._checksum(stuff)
		
	
class IPv6NextHeaderFixup(Fixup):
	'''
	This fixup reconciles the "next header" fields in each of the existing optional headers
	and in the base ipv6 header such that all of them correctly indicate the type of the
	next header.
	'''

	def __init__(self, ref):
		Fixup.__init__(self)
		self.ref = ref
	
	def fixup(self):
		self.context.defaultValue = "0"
		node = self._findDataElementByName(self.ref)
		
		nodeHeaders = node.find("Headers-0")
		if nodeHeaders == None:
			# there are no optional headers; just return 58 'cause we're obviously
			# in the main header
			
			return 58
		
		try:
			
			numHeaders = nodeHeaders.getArrayCount()
			
			if node.name == "IPv6MainDataModel":
				targetHeaderIndex = 0
			
			else:
				current = node.parent.arrayPosition
				if current == numHeaders - 1:	# if we are the last optional header, return 58
					#print "we are at the last optional header"
					return 58
				else:
					targetHeaderIndex = node.parent.arrayPosition + 1
	
	
			nextHeaderType = nodeHeaders.getArrayElementAt(targetHeaderIndex).firstChild().name
			
			#0x00/00 = ipv6 hop-by-hop
			#0x2b/43 = ipv6 routing header
			#0x2c/44 = ipv6 fragment header
			#0x3c/60 = dest. options
			#0x3a/58 = icmpv6
			#0x3b/59 = no next header
			#authentication
			#ESP
			#print "node type is  : " , nextHeaderType
			#print "# of headers is " , numHeaders
			#print "node name     : " , node.name
			#print "aiming for node " , nodeHeaders.getArrayElementAt(targetHeaderIndex).name
			#print
			
			if nextHeaderType == "IPv6Header_HopByHop":
				return 0
			if nextHeaderType == "IPv6Header_DestOptions":
				return 60
			if nextHeaderType == "IPv6Header_Routing":
				return 43
			if nextHeaderType == "IPv6Header_Fragment":
				return 44
			if nextHeaderType == "Icmpv6_RouterAdvertisement":
				return 0x3a
			if nextHeaderType == "Icmpv6_RouterSolicitation":
				return 0x3a
		
		except:
			print "Warning: IPv6NextHeaderFixup Failed, returning 58."
			return 58

# end
