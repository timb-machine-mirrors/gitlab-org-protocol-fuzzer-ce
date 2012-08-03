'''
OpenPGP Radix64 encoding.
'''

import base64
import os
import binascii

from Peach.transformer import Transformer




class Radix64Transformer(Transformer):
	'''
	Radix64 Transformer.
	'''

# crc24() ruthlessly copied from pgpmsg.py:
# Copyright (C) 2003  Jens B. Jorgensen <jbj1@ultraemail.net>
	def crc24(self, s):
		crc24_init = 0xb704ce
		crc24_poly = 0x1864cfb
		crc = crc24_init

		for i in list(s):
			crc = crc ^ (ord(i) << 16)
			for j in range(0, 8):
				crc = crc << 1
				if crc & 0x1000000:
					crc = crc ^ crc24_poly

		return crc & 0xffffff

	def __init__(self):
		Transformer.__init__(self)
		
	def realEncode(self, data):
		encoded = base64.encodestring(data)
		crcint = self.crc24(data)
		crchex = hex(crcint)[2:]
		crcstr = binascii.unhexlify(crchex)
		crc = '=' + base64.encodestring(crcstr)
		print 'encoded: ' + encoded + crc
		return encoded + crc
	
	def realDecode(self, data):
		print 'decoding' + data
		lines = []
		for line in [x.rstrip() for x in data.split(os.linesep)]:
			lines.append(line)

		decoded = base64.decodestring(''.join(self.lines[:len(lines)-1]))

		print 'decoded: ' + decoded

		return decoded

