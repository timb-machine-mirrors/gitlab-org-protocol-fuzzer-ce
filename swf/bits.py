
import re, struct

class BitBuffer:
	'''
	Access buffer as bit stream.  Support the normal
	reading from left to right of bits as well as the reverse
	right to left.
	
	Class seems to work fairly well :)
	'''
	
	def __init__(self, buf='', bigEndian = True):
		self.buf=[ord(x) for x in buf]
		
		self.pos=0
		self.len=len(buf)*8
		
		self.closed = False
		self.softspace = 0
		
		self.bigEndian = bigEndian

	def close(self):
		"""Let me think... Closes and flushes the toilet!"""
		if not self.closed:
			self.closed = True
			del self.buf, self.pos, self.len, self.softspace

	def seek(self, pos, mode = 0):
		"""Set new position"""
		
		if mode == 1:
			pos += self.pos
		elif mode == 2:
			pos += self.len
		self.pos = max(0, pos)

	def tell(self):
		"""Tell current position"""
		
		return self.pos

	def truncate(self, size=None):
		if self.closed:
			raise ValueError, "I/O operation on closed file"
		if size is None:
			size = self.pos
		elif size < 0:
			raise IOError(EINVAL, "Negative size not allowed")
		elif size < self.pos:
			self.pos = size

		self.len=size
		self.buf = self.buf[:(size//8)+(size%8 != 0)]
		if self.buf!=[]: self.buf[-1]=self.buf[-1] & (1<<(size%8))-1
		
	def writebits(self, n, bitlen):
		"""Writes bits"""
		
		if self.closed:
			raise ValueError, "I/O operation on closed file"
		
		n &= (1L << bitlen)-1
		
		newpos = self.pos + bitlen
		
		startBPos=self.pos%8
		startBlock=self.pos//8
		
		endBPos=newpos%8
		endBlock=newpos//8+(endBPos != 0)
		
		#print startBlock, startBPos, endBlock, endBPos
		
		while len(self.buf) < endBlock: self.buf += [0]
		
		pos = startBPos
		
		if not self.bigEndian:
			
			while bitlen > 0:
				bitsLeft=8-(pos%8)
				if bitsLeft > bitlen: bitsLeft=bitlen
				
				mask=(1<<bitsLeft)-1
				
				self.buf[startBlock+(pos//8)] ^= self.buf[startBlock+(pos//8)]&(mask<<(pos%8))
				self.buf[startBlock+(pos//8)] |= int(n&mask)<<(pos%8)
				
				n >>= bitsLeft
				bitlen -= bitsLeft
				
				pos+=bitsLeft
			
			self.pos = newpos
			if self.pos > self.len:
				self.len=self.pos
		
		else:
			
			while bitlen > 0:
				bitsLeft=8-(pos%8)
				if bitsLeft > bitlen: bitsLeft=bitlen
				
				mask=(1<<bitsLeft)-1
				shift = (8 - self.bitlen(self.binaryFormatter(mask, 8))) - (pos - (pos/8*8))
				
				byte = n >> bitlen - self.bitlen(self.binaryFormatter(mask, 8))
				
				self.buf[startBlock+(pos//8)] |= ((byte & mask)<<shift)
				
				bitlen -= bitsLeft
				pos+=bitsLeft
			
			self.pos = newpos
			if self.pos > self.len:
				self.len=self.pos

	def binaryFormatter(self, num, bits):
		'''
		Create a string in binary notation
		'''
		ret = ""
		for i in range(bits-1, -1, -1):
			ret += str((num >> i) & 1)
		
		assert len(ret) == bits
		return ret
	
	def bitlen(self, s):
		return len(s) - s.find('1')
	
	def readbits(self, bitlen):
		"""
		Reads bits based on endianness
		"""
		
		if self.closed:
			raise ValueError, "I/O operation on closed file"
		
		newpos = self.pos + bitlen
		orig_bitlen = bitlen
		
		startBPos=self.pos%8
		startBlock=self.pos//8
		
		endBPos=newpos%8
		endBlock=newpos//8+(endBPos != 0)
		
		ret=0
		
		pos=startBPos
		
		while bitlen > 0:
			bitsLeft = 8 - (pos % 8)
			bitsToLeft = pos - (pos/8*8)
			if bitsLeft > bitlen: bitsLeft=bitlen
			
			mask=(1<<bitsLeft)-1
			
			byte = self.buf[startBlock+(pos//8)]
			
			if not self.bigEndian:
				# Reverse all bits
				newByte = 0
				for i in range(8):
					bit = byte & 0x01
					byte = byte >> 1
					newByte = newByte << 1
					newByte |= bit
				byte = newByte
			
			byte= byte >> (8 - bitsLeft) - bitsToLeft
			
			shift = self.bitlen(self.binaryFormatter(mask, 8))
			ret = ret << shift
			ret |= byte & mask
			
			shift  += bitsLeft
			bitlen -= bitsLeft
			pos    += bitsLeft
		
		# Reverse requested bits
		if not self.bigEndian:
			newByte = 0
			for i in range(orig_bitlen):
				bit = ret & 0x01
				ret = ret >> 1
				newByte = newByte << 1
				newByte |= bit
			ret = newByte
		
		self.pos = newpos
		return ret	
	
	def getvalue(self):
		"""Get the buffer"""
		
		return ''.join(map(chr, self.buf))
	
	def write(self, s):
		for c in str(s):
			self.writebits(ord(c), 8)

	def read(self, i):
		ret=[]
		for i in range(i):
			ret.append( chr(self.readbits(8)) )

		return ''.join(ret)

	def writebit(self, b):
		"""Writes Bit (1bit)"""
		
		self.writebits(b, 1)

	def readbit(self):
		"""Reads Bit (1bit)"""
		
		return self.readbits(1)

	def writebyte(self, i):
		"""Writes Byte (8bits)"""
		
		self.writebits(i, 8)

	def readbyte(self):
		"""Reads Byte (8bits)"""
		
		return self.readbits(8)

	def writeword(self, i):
		"""Writes Word (16bits)"""
		
		self.writebits(i, 16)

	def readword(self):
		"""Reads Word (16bits)"""
		
		return self.readbits(16)

	def writedword(self, i):
		"""Writes DWord (32bits)"""
		
		self.writebits(i, 32)

	def readdword(self):
		"""Reads DWord (32bits)"""
		
		return self.readbits(32)

	def writeSB(self, num, bits):
		'''
		Write a signed number
		'''
		
		num = self._int(num)
		s = self.binaryFormatter(num, self._getNumSizeSigned(num))
		
		if num < 0:
			m = re.search("^1*(1.*)", s)
			val = m.group(1)
			val = ("1" * (bits - len(val))) + val
		
		else:
			m = re.search("^0*(.*)", s)
			val = "0" + m.group(1)
			val = ("0"* (bits - len(val))) + val
		
		for c in val:
			if c == '0':
				self.writebit(0)
			else:
				self.writebit(1)
	
	def readSB(self, bits):
		'''
		Read an SWF signed number
		'''
		
		s = ""
		for i in range(bits):
			b = self.readbit()
			if b == 0:
				s += "0"
			else:
				s += "1"
		
		# Negative number?
		fill = '0'
		if s[0] == '1':
			fill = '1'
		
		if len(s) < 8:
			s = (fill * (8 - len(s))) + s
			return self._unpackSignedString(s, ">b")
		elif len(s) < 16:
			s = (fill * (16 - len(s))) + s
			return self._unpackSignedString(s, ">h")
		elif len(s) < 32:
			s = (fill * (32 - len(s))) + s
			return self._unpackSignedString(s, ">i")
		elif len(s) < 64:
			s = (fill * (64 - len(s))) + s
			return self._unpackSignedString(s, ">q")
		else:
			raise Exception("Numbers longer than 64bits not supported.")
	
	def _unpackSignedString(self, str, fmt):
		
		b = BitBuffer('', True)
		for c in str:
			if c == '0':
				b.writebit(0)
			else:
				b.writebit(1)
		
		return struct.unpack(fmt, b.getvalue())[0]

	def _int(self, num):
		try:
			return int(num)
		except:
			return 0

	def writeUB(self, num, bits):
		'''
		Write an unsigned number
		'''
		num = self._int(num)
		self.writebits(num, bits)
	
	def readUB(self, bits):
		'''
		Read an unsigned number
		'''
		return self.readbits(bits)
	
	def writeFB(self, num, bits):
		'''
		Write a fixed-point number
		'''
		num = self._int(num)
		self.writeSB(num, bits)
	
	def readFB(self, bits):
		'''
		Read a fixed-point number
		'''
		return self.readSB(bits)

	def _getNumSizeUnsigned(self, num):
		'''
		returns 8, 16, 32, or 64
		'''
		if num <= 0xff:
			return 8
		elif num <= 0xffff:
			return 16
		elif num <= 0xffffffff:
			return 32
		elif num <= 0xffffffffffffffff:
			return 64
	
	def _getNumSizeSigned(self, num):
		'''
		returns 8, 16, 32, or 64
		'''
		if num >= -128 and num <= 127:
			return 8
		elif num >= -32768 and num <= 32767:
			return 16
		elif num >= -2147483648 and num <= 2147483647:
			return 32
		elif num >= -9223372036854775808 and num <= 9223372036854775807:
			return 64

	def bitCountForSB(self, num):
		'''
		Return the least number of bits
		to store a signed number.
		'''
		
		# 1111111111111110 --> 1110
		# 1111111111111110 --> 10 ?
		s = self.binaryFormatter(num, self._getNumSizeSigned(num))
		
		if num < 0:
			m = re.search("^1*(1.*)", s)
			ret = len(m.group(1))
		
		else:
			m = re.search("^0*(.*)", s)
			ret = len("0" + m.group(1))
		
		if ret == 0: ret = 1
		
		return ret
	
	def bitCountForUB(self, num):
		'''
		Return the least number of bits
		to store an unsigned number.
		'''
		s = self.binaryFormatter(num, self._getNumSizeUnsigned(num))
		
		m = re.search("^0*(.*)", s)
		ret = len(m.group(1))
		
		if ret == 0: ret = 1
		
		return ret
	
	def bitCountForFB(self, num):
		'''
		Return the least number of bits
		to store a fixed-point value.
		'''
		
		return self.bitCountForSB(num)


































































