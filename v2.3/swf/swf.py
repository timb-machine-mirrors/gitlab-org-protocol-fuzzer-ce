'''
Custom Peach extensions required for SWF pit

Copyright (c) Adobe Systems Inc.
Michael Eddington
11/04/2009
'''

import struct, sys,traceback
from Peach.Engine.dom import *
from bits import BitBuffer

class Rect(Custom):
	'''
	A custom type
	'''
	
	def handleParsing(self, node):
		'''
		Handle any custom parsing of the XML such as
		attributes.
		'''
		
		self.n = Number("n", self)
		self.n.size = 8
		self.n.signed = False
		
		self.Xmin = Number("Xmin", self)
		self.Xmin.size = 32
		self.Xmin.signed = True
		
		self.Xmax = Number("Xmax", self)
		self.Xmax.size = 32
		self.Xmax.signed = True
		
		self.Ymin = Number("Ymin", self)
		self.Ymin.size = 32
		self.Ymin.signed = True
		
		self.Ymax = Number("Ymax", self)
		self.Ymax.size = 32
		self.Ymax.signed = True
		
		self.append(self.n)
		self.append(self.Xmin)
		self.append(self.Xmax)
		self.append(self.Ymin)
		self.append(self.Ymax)
	
	def handleIncomingSize(self, node, data, pos, parent):
		'''
		Return initial read size for this type.
		'''
		
		# Always at least a single byte
		return (2,1)
	
	def handleIncoming(self, cntx, data, pos, parent, doingMinMax = False):
		'''
		Handle data cracking.
		'''
		
		try:
			bits = BitBuffer(data.data[pos:])
			self.n.defaultValue = bits.readUB(5)
			self.Xmin.defaultValue = bits.readSB(self.n.defaultValue)
			self.Xmax.defaultValue = bits.readSB(self.n.defaultValue)
			self.Ymin.defaultValue = bits.readSB(self.n.defaultValue)
			self.Ymax.defaultValue = bits.readSB(self.n.defaultValue)
			
			bitpos = bits.tell()
			while bitpos % 8 != 0:
				bitpos += 1
			
			pos = pos + (bitpos/8)
			return (2, pos)
		
		except IndexError:
			return (4, pos)
	
	def int(self, val):
		try:
			return int(val)
		except:
			return 0
		
	def getInternalValue(self, sout = None):
		'''
		Return the internal value of this date element.  This
		value comes before any modifications such as packing,
		padding, truncating, etc.
		
		For Numbers this is the python int value.
		'''
		
		value = None
		
		# Override value?
		if self.currentValue != None:
			value = self.currentValue
		
		else:
			n = self.int(self.n.getInternalValue())
			
			Xmin = self.int(self.Xmin.getInternalValue())
			Xmax = self.int(self.Xmax.getInternalValue())
			Ymin = self.int(self.Ymin.getInternalValue())
			Ymax = self.int(self.Ymax.getInternalValue())
			
			bits = BitBuffer("")
			
			# Update N, unless we are mutating N
			if n == self.n.defaultValue:
				s = [
					bits.bitCountForSB(Xmin),
					bits.bitCountForSB(Xmax),
					bits.bitCountForSB(Ymin),
					bits.bitCountForSB(Ymax)
					]
				
				n = 0
				for i in s:
					if i > n:
						n = i
			
			bits.writebits(n, 5)
			bits.writebits(Xmin, n)
			bits.writebits(Xmax, n)
			bits.writebits(Ymin, n)
			bits.writebits(Ymax, n)
			
			value = bits.getvalue()
		
		# Write to buffer
		if sout != None:
			sout.write(value, self.getFullDataName())
		
		# Return value
		return value
	
	def getLength(self):
		'''
		Get the length of this element.
		'''
		
		return len(self.getValue())
	
	def getRawValue(self, sout = None):
		try:
			value = self.getInternalValue()
		except:
			value = ''
		
		# Write to buffer
		if sout != None:
			sout.write(value, self.getFullDataName())
		
		return value


class Matrix(Custom):
	'''
	A custom type
	'''
	
	def handleParsing(self, node):
		pass
	
	def handleIncomingSize(self, node, data, pos, parent):
		'''
		Return initial read size for this type.
		'''
		
		# Always at least a single byte
		return (2,1)

	def _number(self, name, size, defaultValue, signed = False):
		if size == 0:
			size = 8
		
		n = Number(name, self)
		n.name = name
		n.size = size
		n.defaultValue = defaultValue
		n.signed = False
		
		return n

	def hexPrint(self, src):
		'''
		WHen in --debug publishers should output there IO
		stuffs using hexPrint.
		'''

		FILTER=''.join([(len(repr(chr(x)))==3) and chr(x) or '.' for x in range(256)])
		N=0; result=''
		length=16
		while src:
			s,src = src[:length],src[length:]
			hexa = ' '.join(["%02X"%ord(x) for x in s])
			s = s.translate(FILTER)
			result += "%04X   %-*s   %s\n" % (N, length*3, hexa, s)
			N+=length
		print result
		
	numSizes = [8, 16, 24, 32, 64]
	def getNumberSize(self, bits):
		
		for s in self.numSizes:
			if bits <= s:
				return s
		
		raise Exception("Number larger than we know what todo with!!")
		
	def handleIncoming(self, cntx, data, pos, parent, doingMinMax = False):
		'''
		Handle data cracking.
		'''
		
		bits = BitBuffer(data.data[pos:])
		
		print "MATRIX:",self.getFullname()
		self.hexPrint(data.data[pos:pos+20])
		
		try:
			self.HasScale = self._number("HasScale", 8, bits.readUB(1))
			self.append(self.HasScale)
			
			if self.HasScale.defaultValue == 1:
				self.NScaleBits = self._number("NScaleBits", 8, bits.readUB(5))
				self.ScaleX = self._number("ScaleX", 32,
					bits.readFB(self.NScaleBits.defaultValue), True)
				self.ScaleY = self._number("ScaleY", 32,
					bits.readFB(self.NScaleBits.defaultValue), True)
				
				print "bits.tell()",bits.tell()
				print "NScaleBits:",self.NScaleBits.defaultValue
				print "ScaleY:",self.ScaleY.getInternalValue()
				
				self.append(self.NScaleBits)
				self.append(self.ScaleX)
				self.append(self.ScaleY)
			else:
				self.NScaleBits = None
				self.ScaleX = None
				self.ScaleY = None
			
			self.HasRotate = self._number("HasRotate", 8, bits.readUB(1))
			print "HasRotate:",self.HasRotate.defaultValue
			if self.HasRotate.defaultValue == 1:
				self.NRotateBits = self._number("NRotateBits", 8, bits.readUB(5))
				self.RotateSkew0 = self._number("RotateSkew0", 32,
					bits.readFB(self.NRotateBits.defaultValue), True)
				self.RotateSkew1 = self._number("RotateSkew1", 32,
					bits.readFB(self.NRotateBits.defaultValue), True)
				
				self.append(self.NRotateBits)
				self.append(self.RotateSkew0)
				self.append(self.RotateSkew1)
			else:
				self.NRotateBits = None
				self.RotateSkew0 = None
				self.RotateSkew1 = None
			
			self.NTranslateBits = self._number("NTranslateBits", 8, bits.readUB(5))
			self.append(self.NTranslateBits)
			if self.NTranslateBits.defaultValue > 0:
				self.TranslateX = self._number("TranslateX", 32,
					bits.readSB(self.NTranslateBits.defaultValue), True)
				self.TranslateY = self._number("TranslateY", 32,
					bits.readSB(self.NTranslateBits.defaultValue), True)
				
				self.append(self.TranslateX)
				self.append(self.TranslateY)
			else:
				self.TranslateX = None
				self.TranslateY = None
			
			bitpos = bits.tell()
			while bitpos % 8 != 0:
				bitpos += 1
			
			newpos = pos + (bitpos/8)
			
			return (2, newpos)
			
		except IndexError:
			return (4, pos)

	def int(self, num):
		try:
			return int(num)
		except:
			return 0

	def getInternalValue(self, sout = None):
		'''
		Return the internal value of this date element.  This
		value comes before any modifications such as packing,
		padding, truncating, etc.
		
		For Numbers this is the python int value.
		'''
		
		value = ""
		
		try:
			# Override value?
			if self.currentValue != None:
				value = self.currentValue
			
			else:
				bits = BitBuffer()
				
				bits.writeUB(self.HasScale.getInternalValue(), 1)
				
				if self.NScaleBits != None:
					NScaleBits = self.NScaleBits.getInternalValue()
					ScaleX = self.ScaleX.getInternalValue()
					ScaleY = self.ScaleY.getInternalValue()
					
					if bits.bitCountForFB(ScaleX) > bits.bitCountForFB(ScaleY):
						NScaleBits = bits.bitCountForFB(ScaleX)
					else:
						NScaleBits = bits.bitCountForFB(ScaleY)
					
					bits.writeUB(NScaleBits, 5)
					bits.writeFB(ScaleX, NScaleBits)
					bits.writeFB(ScaleY, NScaleBits)
				
				bits.writeUB(self.HasRotate.getInternalValue(), 1)
				if self.NRotateBits != None:
					NRotateBits = self.NRotateBits.getInternalValue()
					RotateSkew0 = self.RotateSkew0.getInternalValue()
					RotateSkew1 = self.RotateSkew1.getInternalValue()
					
					if bits.bitCountForFB(RotateSkew0) > bits.bitCountForFB(RotateSkew1):
						NRotateBits = bits.bitCountForFB(RotateSkew0)
					else:
						NRotateBits = bits.bitCountForFB(RotateSkew1)
					
					bits.writeFB(NRotateBits, 5)
					bits.writeFB(RotateSkew0, NRotateBits)
					bits.writeFB(RotateSkew1, NRotateBits)
				
				NTranslateBits = self.NTranslateBits.getInternalValue()
				if self.TranslateX != None:
					TranslateX = self.TranslateX.getInternalValue()
					TranslateY = self.TranslateY.getInternalValue()
					
					if bits.bitCountForFB(TranslateX) > bits.bitCountForFB(TranslateY):
						NTranslateBits = bits.bitCountForFB(TranslateX)
					else:
						NTranslateBits = bits.bitCountForFB(TranslateY)
					
					bits.writeUB(NTranslateBits, 5)
					bits.writeSB(TranslateX, NTranslateBits)
					bits.writeSB(TranslateY, NTranslateBits)
				
				else:
					bits.writeUB(NTranslateBits, 5)
				
				value = bits.getvalue()
		
		except:
			print "OUT_MATRIX: Exception!",sys.exc_info()
		
		# Write to buffer
		if sout != None:
			sout.write(value, self.getFullDataName())
		
		# Return value
		return value
	
	def getLength(self):
		'''
		Get the length of this element.
		'''
		
		return len(self.getValue())
	
	def getRawValue(self, sout = None):
		try:
			value = self.getInternalValue()
		except:
			value = ''
		
		# Write to buffer
		if sout != None:
			sout.write(value, self.getFullDataName())
		
		return value


class EncodedU32(Custom):
	'''
	A custom type
	'''
	
	def handleParsing(self, node):
		'''
		Handle any custom parsing of the XML such as
		attributes.
		'''
		
		self.num = Number("Num", self)
		self.num.size = 32
		self.num.endian = 'little'
		
		pass
	
	def handleIncomingSize(self, node, data, pos, parent):
		'''
		Return initial read size for this type.
		'''
		
		# Always at least a single byte
		return (2,1)
	
	def handleIncoming(self, cntx, data, pos, parent, doingMinMax = False):
		'''
		Handle data cracking.
		'''
		
		try:
			while(True):
				i = struct.unpack("B", data[pos])[0]
				pos += 1
				if i & 0x80 == 0:
					break
				
				i &= 0x7F
				
				j = struct.unpack("B", data[pos])[0]
				pos += 1
				i |= (j & 0x7f) << 7
				if j & 0x80 == 0:
					break
				
				k = struct.unpack("B", data[pos])[0]
				pos += 1
				i |= (k & 0x7f) << 14
				if k & 0x80 == 0:
					break
				
				l = struct.unpack("B", data[pos])[0]
				pos += 1
				i |= (l & 0x7f) << 0x15
				#if l & 0x80 == 0:
				#	break
				
				break
		except:
			raise
			# out of data
			if not cntx.haveAllData:
				raise NeedMoreData(1, "")
			else:
				return (4, pos)
		
		self.num.defaultValue = str(i)
		
		return (2, pos)
	
	def getInternalValue(self, sout = None):
		'''
		Return the internal value of this date element.  This
		value comes before any modifications such as packing,
		padding, truncating, etc.
		
		For Numbers this is the python int value.
		'''
		
		value = None
		
		# Override value?
		if self.currentValue != None:
			value = self.currentValue
		
		else:
			# Default value
			value = self.num.defaultValue
		
		# Write to buffer
		if sout != None:
				sout.write(value, self.getFullDataName())
		
		# Return value
		return value
	
	def getLength(self):
		'''
		Get the length of this element.
		'''
		
		return len(self.getValue())
	
	def getRawValue(self, sout = None):
		value = self.num.getInternalValue()
		
		# Apply mbint pack
		ret = ""
		i = long(value)
		while ((i & 0xffffff80L) != 0L):
			ret += struct.pack("B", ((i & 0x7f) | 0x80))
			i = i >> 7
		
		ret += struct.pack("B", i)
		
		# Write to buffer
		if sout != None:
			sout.write(ret, self.getFullDataName())
		
		return ret

class CXFORM(Custom):
	'''
	A custom type
	'''
	
	def handleParsing(self, node):
		pass
	
	def handleIncomingSize(self, node, data, pos, parent):
		'''
		Return initial read size for this type.
		'''
		
		# Always at least a single byte
		return (2,1)

	def _number(self, name, size, defaultValue, signed = False):
		if size == 0:
			size = 8
		
		n = Number(name, self)
		n.size = size
		n.defaultValue = defaultValue
		n.signed = signed
		
		return n

	numSizes = [8, 16, 24, 32, 64]
	def getNumberSize(self, bits):
		
		for s in self.numSizes:
			if bits <= s:
				return s
		
		raise Exception("Number larger than we know what todo with!!")
		
	def handleIncoming(self, cntx, data, pos, parent, doingMinMax = False):
		'''
		Handle data cracking.
		'''
		
		try:
			bits = BitBuffer(data.data[pos:])
			
			self.HasAddTerms = self._number("HasAddTerms", 8, bits.readUB(1))
			self.HasMultTerms = self._number("HasMultTerms", 8, bits.readUB(1))
			self.Nbits = self._number("Nbits", 8, bits.readUB(4))
			#bits.readbits(8-(1+1+4))
			
			self.append(self.HasAddTerms)
			self.append(self.HasMultTerms)
			self.append(self.Nbits)
			
			if self.HasMultTerms.defaultValue == 1:
				self.RedMultTerm = self._number("RedMultTerm", 32,
					bits.readSB(self.Nbits.defaultValue), True)
				self.GreenMultTerm = self._number("GreenMultTerm", 32,
					bits.readSB(self.Nbits.defaultValue), True)
				self.BlueMultTerm = self._number("BlueMultTerm", 32,
					bits.readSB(self.Nbits.defaultValue), True)
				
				self.append(self.RedMultTerm)
				self.append(self.GreenMultTerm)
				self.append(self.BlueMultTerm)
			
			else:
				self.RedMultTerm = None
				self.GreenMultTerm = None
				self.BlueMultTerm = None
			
			if self.HasAddTerms.defaultValue == 1:
				self.RedAddTerm = self._number("RedAddTerm", 32,
					bits.readSB(self.Nbits.defaultValue), True)
				self.GreenAddTerm = self._number("GreenAddTerm", 32,
					bits.readSB(self.Nbits.defaultValue), True)
				self.BlueAddTerm = self._number("BlueAddTerm", 32,
					bits.readSB(self.Nbits.defaultValue), True)
				
				self.append(self.RedAddTerm)
				self.append(self.GreenAddTerm)
				self.append(self.BlueAddTerm)
			
			else:
				self.RedAddTerm = None
				self.GreenAddTerm = None
				self.BlueAddTerm = None
		except:
			return (4,pos)
		
		bitpos = bits.tell()
		while bitpos % 8 != 0:
			bitpos += 1
		
		pos = pos + (bitpos/8)
		return (2, pos)
	
	def getInternalValue(self, sout = None):
		'''
		Return the internal value of this date element.  This
		value comes before any modifications such as packing,
		padding, truncating, etc.
		
		For Numbers this is the python int value.
		'''
		
		value = None
		
		# Override value?
		if self.currentValue != None:
			value = self.currentValue
		
		else:
			bits = BitBuffer("")
			
			if hasattr(self, "HasAddTerms"):
				bits.writeUB(self.HasAddTerms, 1)
				bits.writeUB(self.HasMultTerms, 1)
				
				rgbs = []
				Nbits = self.Nbits.getInternalValue()
				
				if self.HasMultTerms.defaultValue == 1 and self.RedMultTerm != None:
					RedMultTerm = self.RedMultTerm.getInternalValue()
					GreenMultTerm = self.GreenMultTerm.getInternalValue()
					BlueMultTerm = self.BlueMultTerm.getInternalValue()
					rgbs.append(RedMultTerm)
					rgbs.append(GreenMultTerm)
					rgbs.append(BlueMultTerm)
					
				if self.HasAddTerms.defaultValue == 1 and self.RedAddTerm != None:
					RedAddTerm = self.RedAddTerm.getInternalValue()
					GreenAddTerm = self.GreenAddTerm.getInternalValue()
					BlueAddTerm = self.BlueAddTerm.getInternalValue()
					rgbs.append(RedAddTerm)
					rgbs.append(GreenAddTerm)
					rgbs.append(BlueAddTerm)
				
				# Update N, unless we are mutating N
				if Nbits == self.Nbits.defaultValue:
					if bits.bitCountForSB(max(rgbs)) > bits.bitCountForSB(min(rgbs)):
						Nbits = bits.bitCountForSB(max(rgbs))
					else:
						Nbits = bits.bitCountForSB(min(rgbs))
					
					self.Nbits.defaultValue = Nbits
				
				bits.writeUB(Nbits, 4)
				
				#bits.writebits(0, 8-(1+1+4))
				
				if self.HasMultTerms.defaultValue == 1 and self.RedMultTerm != None:
					self.writeSB(RedMultTerm, Nbits)
					self.writeSB(GreenMultTerm, Nbits)
					self.writeSB(BlueMultTerm, Nbits)
				
				if self.HasAddTerms.defaultValue == 1 and self.RedAddTerm != None:
					self.writeSB(RedAddTerm, Nbits)
					self.writeSB(GreenAddTerm, Nbits)
					self.writeSB(BlueAddTerm, Nbits)
			
			value = bits.getvalue()
		
		# Write to buffer
		if sout != None:
			sout.write(value, self.getFullDataName())
		
		# Return value
		return value
	
	def getLength(self):
		'''
		Get the length of this element.
		'''
		
		return len(self.getValue())
	
	def getRawValue(self, sout = None):
		try:
			value = self.getInternalValue()
		except:
			value = ''
		
		# Write to buffer
		if sout != None:
			sout.write(value, self.getFullDataName())
		
		return value

class CXFORMWITHALPHA(Custom):
	'''
	A custom type
	'''
	
	def handleParsing(self, node):
		pass
	
	def handleIncomingSize(self, node, data, pos, parent):
		'''
		Return initial read size for this type.
		'''
		
		# Always at least a single byte
		return (2,1)

	def _number(self, name, size, defaultValue, signed = False):
		if size == 0:
			size = 8
		
		n = Number(name, self)
		n.size = size
		n.defaultValue = defaultValue
		n.signed = signed
		
		return n

	numSizes = [8, 16, 24, 32, 64]
	def getNumberSize(self, bits):
		
		for s in self.numSizes:
			if bits <= s:
				return s
		
		raise Exception("Number larger than we know what todo with!!")
		
	def handleIncoming(self, cntx, data, pos, parent, doingMinMax = False):
		'''
		Handle data cracking.
		'''
		
		try:
			bits = BitBuffer(data.data[pos:])
			
			self.HasAddTerms = self._number("HasAddTerms", 8, bits.readUB(1))
			self.HasMultTerms = self._number("HasMultTerms", 8, bits.readUB(1))
			self.Nbits = self._number("Nbits", 8, bits.readUB(4))
			#bits.readbits(8-(1+1+4))
			
			self.append(self.HasAddTerms)
			self.append(self.HasMultTerms)
			self.append(self.Nbits)
			
			if self.HasMultTerms.defaultValue == 1:
				self.RedMultTerm = self._number("RedMultTerm", 32,
					bits.readSB(self.Nbits.defaultValue), True)
				self.GreenMultTerm = self._number("GreenMultTerm", 32,
					bits.readSB(self.Nbits.defaultValue), True)
				self.BlueMultTerm = self._number("BlueMultTerm", 32,
					bits.readSB(self.Nbits.defaultValue), True)
				self.AlphaMultTerm = self._number("AlphaMultTerm", 32,
					bits.readSB(self.Nbits.defaultValue), True)
				
				self.append(self.RedMultTerm)
				self.append(self.GreenMultTerm)
				self.append(self.BlueMultTerm)
				self.append(self.AlphaMultTerm)
				
			else:
				self.RedMultTerm = None
				self.GreenMultTerm = None
				self.BlueMultTerm = None
				self.AlphaMultTerm = None
			
			if self.HasAddTerms.defaultValue == 1:
				self.RedAddTerm = self._number("RedAddTerm", 32,
					bits.readSB(self.Nbits.defaultValue), True)
				self.GreenAddTerm = self._number("GreenAddTerm", 32,
					bits.readSB(self.Nbits.defaultValue), True)
				self.BlueAddTerm = self._number("BlueAddTerm", 32,
					bits.readSB(self.Nbits.defaultValue), True)
				self.AlphaAddTerm = self._number("AlphaAddTerm", 32,
					bits.readSB(self.Nbits.defaultValue), True)
				
				self.append(self.RedAddTerm)
				self.append(self.GreenAddTerm)
				self.append(self.BlueAddTerm)
				self.append(self.AlphaAddTerm)
				
			else:
				self.RedAddTerm = None
				self.GreenAddTerm = None
				self.BlueAddTerm = None
				self.AlphaAddTerm = None
			
		except:
			return (4, pos)
		
		bitpos = bits.tell()
		while bitpos % 8 != 0:
			bitpos += 1
		
		pos = pos + (bitpos/8)
		return (2, pos)
		
	def getInternalValue(self, sout = None):
		'''
		Return the internal value of this date element.  This
		value comes before any modifications such as packing,
		padding, truncating, etc.
		
		For Numbers this is the python int value.
		'''
		
		value = None
		
		# Override value?
		if self.currentValue != None:
			value = self.currentValue
		
		else:
			try:
				bits = BitBuffer()
				
				if hasattr(self, "HasAddTerms"):
					bits.writeUB(int(self.HasAddTerms.getInternalValue()), 1)
					bits.writeUB(int(self.HasMultTerms.getInternalValue()), 1)
					
					rgbs = []
					Nbits = int(self.Nbits.getInternalValue())
					
					if self.HasMultTerms.defaultValue == 1 and self.RedMultTerm != None:
						RedMultTerm = int(self.RedMultTerm.getInternalValue())
						GreenMultTerm = int(self.GreenMultTerm.getInternalValue())
						BlueMultTerm = int(self.BlueMultTerm.getInternalValue())
						AlphaMultTerm = int(self.AlphaMultTerm.getInternalValue())
						rgbs.append(RedMultTerm)
						rgbs.append(GreenMultTerm)
						rgbs.append(BlueMultTerm)
						rgbs.append(AlphaMultTerm)
						
					if self.HasAddTerms.defaultValue == 1 and self.RedAddTerm != None:
						RedAddTerm = int(self.RedAddTerm.getInternalValue())
						GreenAddTerm = int(self.GreenAddTerm.getInternalValue())
						BlueAddTerm = int(self.BlueAddTerm.getInternalValue())
						AlphaAddTerm = int(self.AlphaAddTerm.getInternalValue())
						rgbs.append(RedAddTerm)
						rgbs.append(GreenAddTerm)
						rgbs.append(BlueAddTerm)
						rgbs.append(AlphaAddTerm)
					
					# Update N, unless we are mutating N
					if Nbits == self.Nbits.defaultValue:
						
						if len(rgbs) > 0 and bits.bitCountForSB(max(rgbs)) > bits.bitCountForSB(min(rgbs)):
							Nbits = bits.bitCountForSB(max(rgbs))
						elif len(rgbs) > 0:
							Nbits = bits.bitCountForSB(min(rgbs))
						else:
							Nbits = 0
						
						self.Nbits.defaultValue = Nbits
					
					bits.writeUB(Nbits, 4)
					
					if self.HasMultTerms.defaultValue == 1 and self.RedMultTerm != None:
						bits.writeSB(RedMultTerm, Nbits)
						bits.writeSB(GreenMultTerm, Nbits)
						bits.writeSB(BlueMultTerm, Nbits)
						bits.writeSB(AlphaMultTerm, Nbits)
					
					if self.HasAddTerms.defaultValue == 1 and self.RedAddTerm != None:
						bits.writeSB(RedAddTerm, Nbits)
						bits.writeSB(GreenAddTerm, Nbits)
						bits.writeSB(BlueAddTerm, Nbits)
						bits.writeSB(AlphaAddTerm, Nbits)
				
				value = bits.getvalue()
			
			except:
				traceback.print_exc()
		
		# Write to buffer
		if sout != None:
			sout.write(value, self.getFullDataName())
		
		# Return value
		return value
	
	def getLength(self):
		'''
		Get the length of this element.
		'''
		
		return len(self.getValue())
	
	def getRawValue(self, sout = None):
		try:
			value = self.getInternalValue()
		except:
			value = ''
		
		# Write to buffer
		if sout != None:
			sout.write(value, self.getFullDataName())
		
		return value


class SHAPERECORDARRAY(Custom):
	'''
	Custom type for Adobe SWF files.  Lots of bit slicing with if/then.
	'''
	
	def handleParsing(self, node):
		'''
		Handle any custom parsing of the XML such as
		attributes.
		'''
		
		self.incomingValue = None
	
	def handleIncomingSize(self, node, data, pos, parent):
		'''
		Return initial read size for this type.
		'''
		
		# Always at least a single byte
		return (2,1)
		
	def hexPrint(self, src):
		'''
		WHen in --debug publishers should output there IO
		stuffs using hexPrint.
		'''

		FILTER=''.join([(len(repr(chr(x)))==3) and chr(x) or '.' for x in range(256)])
		N=0; result=''
		length=16
		while src:
			s,src = src[:length],src[length:]
			hexa = ' '.join(["%02X"%ord(x) for x in s])
			s = s.translate(FILTER)
			result += "%04X   %-*s   %s\n" % (N, length*3, hexa, s)
			N+=length
		print result
		
	def handleIncoming(self, cntx, data, pos, parent, doingMinMax = False, bits = None):
		'''
		Handle data cracking.
		'''
		
		try:
			for child in self:
				if isinstance(obj, SHAPERECORD):
					del self[obj.name]
			
			print "SHAPREECORDARRAY:", self.getFullname()
			print "SHAPERECORDARRAY: Starting at pos %d" % pos
			print "SHAPERECORDARRAY: data length %d" % len(data.data)
			
			count = 0
			newpos = pos
			
			bits = BitBuffer(data.data[pos:])
			
			while True:
				
				bitpos = bits.tell()
				while bitpos % 8 != 0:
					bitpos += 1
					
				print "SHAPERECORDARRAY: Doing child %d at tell %d pos %d" % (count, bits.tell(),
																			  pos + bitpos/8)
				
				elem = SHAPERECORD("ShapeRecord-%d" % count, self)
				elem.arrayPosition = count
				elem.arrayMinOccurs = 1
				elem.pos = newpos
				elem.possiblePos = newpos
				
				rating, newpos = elem.handleIncoming(cntx, data, newpos, self, doingMinMax, bits)
				
				if rating > 2:
					print "SHAPERECORDARRAY: Child %d failed to parse" % count
					return (4, pos)
				
				self.append(elem)
				
				try:
					if elem.TypeFlag.defaultValue == elem.StateNewStyles.defaultValue == \
						elem.StateLineStyle.defaultValue == elem.StateFillStyle1.defaultValue == \
						elem.StateFillStyle0.defaultValue == elem.StateMoveTo.defaultValue == 0:
						
						# This is the last element
						print "SHAPERECORDARRAY: Child %d is end element" % count
						break
				except:
					pass
				
				count += 1
			
			for child in self:
				if isinstance(child, SHAPERECORD):
					child.arrayMaxOccurs = count
			
			bitpos = bits.tell()
			while bitpos % 8 != 0:
				bitpos += 1
			
			newpos = pos + (bitpos/8)
		
		except MemoryError:
			return (4,pos)
		except IndexError:
			return (4,pos)
		
		print "SHAPERECORDARRAY: Done, pos == %d" % newpos
		#self.incomingValue = data.data[pos:newpos]
		
		return (2, newpos)
	
	def getInternalValue(self, sout = None, bits = None):
		'''
		Return the internal value of this date element.  This
		value comes before any modifications such as packing,
		padding, truncating, etc.
		
		For Numbers this is the python int value.
		'''
		
		value = ""
		
		# Override value?
		if self.currentValue != None:
			value = self.currentValue
		
		else:
			
			for child in self:
				if isinstance(child, SHAPERECORD):
					value += child.getInternalValue(sout)
			
		# Write to buffer
		if sout != None:
			sout.write(value, self.getFullDataName())
		
		# Return value
		return value
	
	def getLength(self):
		'''
		Get the length of this element.
		'''
		
		return len(self.getValue())
	
	def getRawValue(self, sout = None):
		try:
			value = self.getInternalValue()
		except:
			raise
			value = ''
		
		# Write to buffer
		if sout != None:
			sout.write(value, self.getFullDataName())
		
		return value


class SHAPERECORD(Custom):
	'''
	Custom type for Adobe SWF files.  Lots of bit slicing with if/then.
	'''
	
	FillBits = None
	LineBits = None
	
	def handleParsing(self, node):
		pass
	
	def handleIncomingSize(self, node, data, pos, parent):
		'''
		Return initial read size for this type.
		'''
		
		# Always at least a single byte
		return (2,1)

	def _number(self, name, size, defaultValue, signed = False):
		if size == 0:
			size = 8
		
		n = Number(name, self)
		n.size = size
		n.defaultValue = defaultValue
		n.signed = signed
		
		self.append(n)
		
		return n

	def handleIncoming(self, cntx, data, pos, parent, doingMinMax = False, bits = None):
		'''
		Handle data cracking.
		'''
		#FillBits = 0
		#LineBits = 0
		
		if bits == None:
			bits = BitBuffer(data.data[pos:])
		
		try:
			self.TypeFlag = self._number("TypeFlag", 8, bits.readUB(1))
			self.append(self.TypeFlag)
			
			print "TypeFlag:",self.TypeFlag.defaultValue
			
			# Edge Records
			if self.TypeFlag.defaultValue == 1:
				
				# StraightEdgeRecord
				if bits.readUB(1) == 1:
					self.StraightFlag = self._number("StraightFlag", 8, 1)
					self.NumBits = self._number("NumBits", 8, bits.readUB(4))
					self.GeneralLineFlag = self._number("GeneralLineFlag", 8, bits.readUB(1))
					
					print "StraightFlag:",self.StraightFlag.defaultValue
					print "NumBits:",self.NumBits.defaultValue
					print "GeneralLineFlag:",self.GeneralLineFlag.defaultValue
					
					self.append(self.StraightFlag)
					self.append(self.NumBits)
					self.append(self.GeneralLineFlag)
					
					# General Line
					if self.GeneralLineFlag.defaultValue == 1:
						self.DeltaX = self._number("DeltaX", 32,
							bits.readSB(self.NumBits.defaultValue+2), True)
						self.DeltaY = self._number("DeltaY", 32,
							bits.readSB(self.NumBits.defaultValue+2), True)
						
						print "DeltaX:",self.DeltaX.defaultValue
						print "DeltaY:",self.DeltaY.defaultValue
						
						self.append(self.DeltaX)
						self.append(self.DeltaY)
					
					# Vert/Horz Line
					else:
						self.VertLineFlag = self._number("VertLineFlag", 8, bits.readSB(1), True)
						self.append(self.VertLineFlag)
						
						if self.VertLineFlag.defaultValue == 1:
							self.DeltaY = self._number("DeltaY", 32,
								bits.readSB(self.NumBits.defaultValue+2), True)
							self.append(self.DeltaY)
							
							print "VertLineFlag:",self.VertLineFlag.defaultValue
							print "DeltaY:",self.DeltaY.defaultValue
						
						else:
							self.DeltaX = self._number("DeltaX", 32,
								bits.readSB(self.NumBits.defaultValue+2), True)
							self.append(self.DeltaX)
							
							print "VertLineFlag:",self.VertLineFlag.defaultValue
							print "DeltaX:",self.DeltaX.defaultValue
				
				# Curved Edge Record
				else:
					self.StraightFlag = self._number("StraightFlag", 8, 0)
					self.NumBits = self._number("NumBits", 8, bits.readUB(4))
					
					print "StraightFlag:",self.StraightFlag.defaultValue
					print "NumBits:",self.NumBits.defaultValue
					
					self.ControlDeltaX = self._number("ControlDeltaX", 32,
						bits.readSB(self.NumBits.defaultValue+2), True)
					self.ControlDeltaY = self._number("ControlDeltaY", 32,
						bits.readSB(self.NumBits.defaultValue+2), True)
					self.AnchorDeltaX = self._number("AnchorDeltaX", 32,
						bits.readSB(self.NumBits.defaultValue+2), True)
					self.AnchorDeltaY = self._number("AnchorDeltaY", 32,
						bits.readSB(self.NumBits.defaultValue+2), True)
					
					print "ControlDeltaX:",self.ControlDeltaX.defaultValue
					print "ControlDeltaY:",self.ControlDeltaY.defaultValue
					print "AnchorDeltaX:",self.AnchorDeltaX.defaultValue
					print "AnchorDeltaY:",self.AnchorDeltaY.defaultValue
					
				bitpos = bits.tell()
				while bitpos % 8 != 0:
					bitpos += 1
				
				pos = pos + (bitpos/8)
				
			else:
				# EndShapeRecord
				if bits.readUB(5) == 0:
					self.EndOfShape = self._number("EndOfShape", 8, 0)
					self.append(self.EndOfShape)
					
					bitpos = bits.tell()
					while bitpos % 8 != 0:
						bitpos += 1
					
					pos = pos + (bitpos/8)
					
				# StyleChangeRecord
				else:
					bits.seek(-5, 1)
				
					self.StateNewStyles = self._number("StateNewStyles", 8, bits.readUB(1))
					self.StateLineStyle = self._number("StateLineStyle", 8, bits.readUB(1))
					self.StateFillStyle1 = self._number("StateFillStyle1", 8, bits.readUB(1))
					self.StateFillStyle0 = self._number("StateFillStyle0", 8, bits.readUB(1))
					self.StateMoveTo = self._number("StateMoveTo", 8, bits.readUB(1))
					
					print "StateNewStyles:",self.StateNewStyles.defaultValue
					print "StateLineStyle:",self.StateLineStyle.defaultValue
					print "StateFillStyle1:",self.StateFillStyle1.defaultValue
					print "StateFillStyle0:",self.StateFillStyle0.defaultValue
					print "StateMoveTo:",self.StateMoveTo.defaultValue
					
					self.append(self.StateNewStyles)
					self.append(self.StateLineStyle)
					self.append(self.StateFillStyle1)
					self.append(self.StateFillStyle0)
					self.append(self.StateMoveTo)
					
					if self.StateMoveTo.defaultValue == 1:
						self.MoveBits = self._number("MoveBits", 8, bits.readUB(5))
						self.MoveDeltaX = self._number("MoveDeltaX", 32,
							bits.readSB(self.MoveBits.defaultValue), True)
						self.MoveDeltaY = self._number("MoveDeltaY", 32,
							bits.readSB(self.MoveBits.defaultValue), True)
						
						print "MoveBits:",self.MoveBits.defaultValue
						print "MoveDeltaX:",self.MoveDeltaX.defaultValue
						print "MoveDeltaY:",self.MoveDeltaY.defaultValue
						
						self.append(self.MoveBits)
						self.append(self.MoveDeltaX)
						self.append(self.MoveDeltaY)
					
					if self.StateFillStyle0.defaultValue == 1:
						try:
							self.FillBits = self.parent.parent['Flags']['NumFillBits'].getInternalValue()
						except:
							self.FillBits = int(self.find('NumFillBits').getInternalValue())
						self.FillStyle0 = self._number("FillStyle0", 32, bits.readUB(self.FillBits))
						
						print "FillBits",self.FillBits
						print "FillStyle0",self.FillStyle0.defaultValue
						
						self.append(self.FillStyle0)
						
					if self.StateFillStyle1.defaultValue == 1:
						try:
							self.FillBits = self.parent.parent['Flags']['NumFillBits'].getInternalValue()
						except:
							self.FillBits = int(self.find('NumFillBits').getInternalValue())
						
						print "FillBits",self.FillBits
						self.FillStyle1 = self._number("FillStyle1", 32, bits.readUB(self.FillBits))
						print "FillStyle1",self.FillStyle1.defaultValue
						
						self.append(self.FillStyle1)
					
					if self.StateLineStyle.defaultValue == 1:
						try:
							self.LineBits = self.parent.parent['Flags']['NumLineBits'].getInternalValue()
						except:
							self.LineBits = int(self.find('NumLineBits').getInternalValue())
						
						print "LineBits",self.LineBits
						self.LineStyle = self._number("LineStyle", 32, bits.readUB(self.LineBits))
						print "LineStyle",self.LineStyle.defaultValue
						
						self.append(self.LineStyle)
					
					#if CurrentTag in [22, 32] and self.StateNewStyles.defaultValue == 1:
					if self.StateNewStyles.defaultValue == 1:
							
						bitpos = bits.tell()
						while bitpos % 8 != 0:
							bitpos += 1
						
						# Make a copy of FILLSTYLEARRAY
						self.FillStyles = self.getRoot()['FILLSTYLEARRAY'].copy(self)
						self.FillStyles.name = 'FillStyles'
						self.FillStyles.ref = "FILLSTYLEARRAY"
						self.append(self.FillStyles)
						
						cntx.optmizeModelForCracking(self.FillStyles, True)
						
						# Call back into cracker to handle Block
						(rating, newpos) = cntx._handleNode(self.FillStyles, data, pos + (bitpos/8), self, doingMinMax)
						if rating > 2:
							return (rating, newpos)
						
						# Make a copy of LINESTYLEARRAY
						self.LineStyles = self.getRoot()['LINESTYLEARRAY'].copy(self)
						self.LineStyles.name = 'LineStyles'
						self.LineStyles.ref = "LINESTYLEARRAY"
						self.append(self.LineStyles)
						
						cntx.optmizeModelForCracking(self.LineStyles, True)
						
						# Call back into cracker to handle Block
						(rating, newpos) = cntx._handleNode(self.FillStyles, data, newpos, self, doingMinMax)
						if rating > 2:
							return (rating, newpos)
						
						bits = BitBuffer(data.data[newpos:])
						self.NumFillBits = self._number("NumFillBits", 32, bits.readUB(4))
						self.NumLineBits = self._number("NumLineBits", 32, bits.readUB(4))
						#self.FillBits = self.NumFillBits.defaultValue
						#self.LineBits = self.NumLineBits.defaultValue
						
						self.append(self.NumFillBits)
						self.append(self.NumLineBits)
						
						pos = newpos + 1
						
					else:
						bitpos = bits.tell()
						while bitpos % 8 != 0:
							bitpos += 1
						
						pos = pos + (bitpos/8)
		
		except IndexError:
			return (4, pos)
		
		return (2, pos)
	
	def int(self, num):
		try:
			return int(num)
		except:
			return 0
	
	def getInternalValue(self, sout = None, bits = None):
		'''
		Return the internal value of this date element.  This
		value comes before any modifications such as packing,
		padding, truncating, etc.
		
		For Numbers this is the python int value.
		'''
		
		value = None
		
		# Override value?
		if self.currentValue != None:
			value = self.currentValue
		
		else:
			bits = BitBuffer("")
			
			bits.writeUB(self.TypeFlag.getInternalValue(), 1)
			
			if hasattr(self, 'StraightFlag'):
				bits.writeUB(self.StraightFlag.getInternalValue(), 1)
			
			if hasattr(self, 'NumBits'):
				if self.NumBits.getInternalValue() == self.NumBits.defaultValue:
					# Calc new value
					v = []
					if hasattr(self, 'DeltaX'):
						v.append(self.int(self.DeltaX.getInternalValue()))
					if hasattr(self, 'DeltaY'):
						v.append(self.int(self.DeltaY.getInternalValue()))
					if hasattr(self, 'ControlDeltaX'):
						v.append(self.int(self.ControlDeltaX.getInternalValue()))
					if hasattr(self, 'ControlDeltaY'):
						v.append(self.int(self.ControlDeltaY.getInternalValue()))
					if hasattr(self, 'AnchorDeltaX'):
						v.append(self.int(self.AnchorDeltaX.getInternalValue()))
					if hasattr(self, 'AnchorDeltaY'):
						v.append(self.int(self.AnchorDeltaY.getInternalValue()))
					
					if bits.bitCountForSB(max(v)) > bits.bitCountForSB(min(v)):
						self.NumBits.defaultValue = bits.bitCountForSB(max(v)) - 2
					else:
						self.NumBits.defaultValue = bits.bitCountForSB(min(v)) - 2
					
					if self.NumBits.defaultValue < 1: self.NumBits.defaultValue = 1
					
				bits.writeUB(self.NumBits.getInternalValue(), 4)
				
			if hasattr(self, 'GeneralLineFlag'):
				bits.writeUB(self.GeneralLineFlag.getInternalValue(), 1)
			if hasattr(self, 'VertLineFlag'):
				bits.writeUB(self.VertLineFlag.getInternalValue(), 1)
			if hasattr(self, 'DeltaX'):
				bits.writeSB(self.DeltaX.getInternalValue(), self.NumBits.getInternalValue()+2)
			if hasattr(self, 'DeltaY'):
				bits.writeSB(self.DeltaY.getInternalValue(), self.NumBits.getInternalValue()+2)
			if hasattr(self, 'ControlDeltaX'):
				bits.writeSB(self.ControlDeltaX.getInternalValue(), self.NumBits.getInternalValue()+2)
			if hasattr(self, 'ControlDeltaY'):
				bits.writeSB(self.ControlDeltaY.getInternalValue(), self.NumBits.getInternalValue()+2)
			if hasattr(self, 'AnchorDeltaX'):
				bits.writeSB(self.AnchorDeltaX.getInternalValue(), self.NumBits.getInternalValue()+2)
			if hasattr(self, 'AnchorDeltaY'):
				bits.writeSB(self.AnchorDeltaY.getInternalValue(), self.NumBits.getInternalValue()+2)
			
			# Type = 0
			
			if hasattr(self, 'StateNewStyles'):
				bits.writeUB(self.StateNewStyles.getInternalValue(), 1)
			if hasattr(self, 'StateLineStyle'):
				bits.writeUB(self.StateLineStyle.getInternalValue(), 1)
			if hasattr(self, 'StateFillStyle1'):
				bits.writeUB(self.StateFillStyle1.getInternalValue(), 1)
			if hasattr(self, 'StateFillStyle0'):
				bits.writeUB(self.StateFillStyle0.getInternalValue(), 1)
			if hasattr(self, 'StateMoveTo'):
				bits.writeUB(self.StateMoveTo.getInternalValue(), 1)
			if hasattr(self, 'MoveBits'):
				if self.MoveBits.defaultValue == self.MoveBits.getInternalValue():
					if bits.bitCountForSB(self.int(self.MoveDeltaX.getInternalValue())) > bits.bitCountForSB(self.int(self.MoveDeltaY.getInternalValue())):
						self.MoveBits.defaultValue = bits.bitCountForSB(self.int(self.MoveDeltaX.getInternalValue()))
					else:
						self.MoveBits.defaultValue = bits.bitCountForSB(self.int(self.MoveDeltaY.getInternalValue()))
				
				bits.writeUB(self.MoveBits.getInternalValue(), 5)
				
			if hasattr(self, 'MoveDeltaX'):
				bits.writeSB(self.MoveDeltaX.getInternalValue(), self.MoveBits.getInternalValue())
			if hasattr(self, 'MoveDeltaY'):
				bits.writeSB(self.MoveDeltaY.getInternalValue(), self.MoveBits.getInternalValue())
			if hasattr(self, 'FillStyle0'):
				bits.writeUB(self.FillStyle0.getInternalValue(), self.FillBits)
			if hasattr(self, 'FillStyle1'):
				bits.writeUB(self.FillStyle1.getInternalValue(), self.FillBits)
			if hasattr(self, 'LineStyle'):
				bits.writeUB(self.LineStyle.getInternalValue(), self.LineBits)
			if hasattr(self, 'FillStyles'):
				for b in self.FillStyles.getValue():
					bits.writebyte(b)
			if hasattr(self, 'LineStyles'):
				for b in self.LineStyles.getValue():
					bits.writebyte(b)
			if hasattr(self, 'NumFillBits'):
				bits.writeUB(int(self.NumFillBits.getInternalValue()), 4)
			if hasattr(self, 'NumLineBits'):
				bits.writeUB(int(self.NumLineBits.getInternalValue()), 4)
		
		value = bits.getvalue()
		
		# Write to buffer
		if sout != None:
			sout.write(value, self.getFullDataName())
		
		# Return value
		return value
	
	def getLength(self):
		'''
		Get the length of this element.
		'''
		
		return len(self.getValue())
	
	def getRawValue(self, sout = None):
		try:
			value = self.getInternalValue()
		except:
			value = ''
		
		# Write to buffer
		if sout != None:
			sout.write(value, self.getFullDataName())
		
		return value

# end
