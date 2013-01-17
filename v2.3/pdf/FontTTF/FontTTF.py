
import struct, sys,traceback
from Peach import fixup
from Peach.Engine.common import *
from Peach.Publishers.file import FileWriterLauncherGui
import os, sys

class FontTTFPublisher(FileWriterLauncherGui):
	'''
	Custom publisher extension for Reader JPXDecode fuzzer. This
	publisher will wrap the JPEG2000 image with proper PDF stuffs.
	'''

	def __init__(self, filename, windowname, debugger = "false", waitTime = 3):
		FileWriterLauncherGui.__init__(self, filename, windowname, debugger, waitTime)
	
	def close(self):		
		FileWriterLauncherGui.close(self)
		os.system("FontPdfMaker.exe")


def GetOffset(node, name):
	root = node.getRootOfDataMap()
	
	for child in root.getAllChildDataElements():
		if child.has_key("tag") and child["tag"].getInternalValue() == name:
			return int(child["offset"].getInternalValue())
	
	raise Exception("GetOffset: Unable to locate offset for table name %s" % name)

class TableDirectoryChecksumFixup(fixup.Fixup):
	'''
	I don't think this is checked.
	'''
	
	def __init__(self, ref):
		fixup.Fixup.__init__(self)
		self.ref = ref
	
	def fixup(self):
		self.context.defaultValue = "0"
		ref = self._findDataElementByName(self.ref)
		stuff = ref.getValue()
		if stuff == None:
			raise Exception("TableDirectoryChecksumFixup: was unable to locate [%s]" % self.ref)
		
		sum = 0
		for i in range(len(stuff)):
			sum += ord(stuff[i])
		
		return sum ^ key

class TablePadFixup(fixup.Fixup):
	'''
	Padding is the suck
	'''
	
	def __init__(self, ref):
		fixup.Fixup.__init__(self)
		self.ref = ref
	
	def fixup(self):
		self.context.defaultValue = "0"
		ref = self._findDataElementByName(self.ref)
		stuff = ref.getValue()
		if stuff == None:
			raise Exception("TablePadFixup: was unable to locate [%s]" % self.ref)
		
		pad = 0
		d = len(stuff)/4
		if len(stuff) - (4 * d) > 0:
			pad = 4 - (len(stuff) - (4 * d))
		
		return "\x00" * pad

from Peach.Engine.dom import *

class CompositeGlyph(Custom):
	'''
	A custom type
	'''
	
	ARG_1_AND_2_ARE_WORDS =	0
	ARGS_ARE_XY_VALUES =	1
	ROUND_XY_TO_GRID  = 2
	WE_HAVE_A_SCALE = 3
	RESERVED  = 4
	MORE_COMPONENTS = 5
	WE_HAVE_AN_X_AND_Y_SCALE = 6
	WE_HAVE_A_TWO_BY_TWO = 7
	WE_HAVE_INSTRUCTIONS = 8
	USE_MY_METRICS = 9
	OVERLAP_COMPOUND = 10
	SCALED_COMPONENT_OFFSET = 11
	UNSCALED_COMPONENT_OFFSET = 12

	
	def handleParsing(self, node):
		'''
		Handle any custom parsing of the XML such as
		attributes.
		'''
		
		pass
	
	def handleIncomingSize(self, node, data, pos, parent):
		'''
		Return initial read size for this type.
		'''
		
		# Always at least a single byte
		return (2, 6)
	
	def createNumber(self, name, size = 16, signed = False, endian = 'big'):
		num = Number()
		num.name = name
		num.size = size
		num.signed = signed
		num.endian = endian
		
	def handleIncoming(self, cntx, data, pos, parent, doingMinMax = False):
		'''
		Handle data cracking.
		'''
		
		try:
			while True:
				block = Block()
				self.add(block)
				
				flags = self.createNumber("flags")
				block.add(flags)
				
				glyphIndex = self.createNumber("glyphIndex")
				block.add(glyphIndex)
				
				(rating, pos) = cntx._handleNode(flags, data, pos, block)
				if rating > 2:
					return (rating, pos)
					
				(rating, pos) = cntx._handleNode(glyphIndex, data, pos, block)
				if rating > 2:
					return (rating, pos)
				
				if flags.defaultValue & ARG_1_AND_2_ARE_WORDS:
					argument1 = self.createNumber("argument1")
					argument2 = self.createNumber("argument2")
					block.add(argument1)
					block.add(argument2)
					
					(rating, pos) = cntx._handleNode(argument1, data, pos, block)
					if rating > 2:
						return (rating, pos)
						
					(rating, pos) = cntx._handleNode(argument2, data, pos, block)
					if rating > 2:
						return (rating, pos)
				else:
					arg1AndArg2 = self.createNumber("arg1AndArg2")
					block.add(arg1AndArg2)
					
					(rating, pos) = cntx._handleNode(arg1AndArg2, data, pos, block)
					if rating > 2:
						return (rating, pos)
				
				if flags.defaultValue & WE_HAVE_A_SCALE:
					scale = self.createNumber("scale", 16, True)
					block.add(scale)
					
					(rating, pos) = cntx._handleNode(scale, data, pos, block)
					if rating > 2:
						return (rating, pos)
					
				elif flags.defaultValue & WE_HAVE_AN_X_AND_Y_SCALE:
					xscale = self.createNumber("xscale", 16, True)
					yscale = self.createNumber("yscale", 16, True)
					block.add(xscale)
					block.add(yscale)
					
					(rating, pos) = cntx._handleNode(xscale, data, pos, block)
					if rating > 2:
						return (rating, pos)
					
					(rating, pos) = cntx._handleNode(yscale, data, pos, block)
					if rating > 2:
						return (rating, pos)
					
				elif flags.defaultValue & WE_HAVE_A_TWO_BY_TWO:
					xscale = self.createNumber("xscale", 16, True)
					scale01 = self.createNumber("scale01", 16, True)
					scale10 = self.createNumber("scale10", 16, True)
					yscale = self.createNumber("yscale", 16, True)
					block.add(xscale)
					block.add(scale01)
					block.add(scale10)
					block.add(yscale)
					
					(rating, pos) = cntx._handleNode(xscale, data, pos, block)
					if rating > 2:
						return (rating, pos)
					
					(rating, pos) = cntx._handleNode(scale01, data, pos, block)
					if rating > 2:
						return (rating, pos)
					
					(rating, pos) = cntx._handleNode(xscale10, data, pos, block)
					if rating > 2:
						return (rating, pos)
					
					(rating, pos) = cntx._handleNode(yscale, data, pos, block)
					if rating > 2:
						return (rating, pos)
				
				if not (flags & MORE_COMPONENTS):
					break
			
			if flags.defaultValue & WE_HAVE_INSTR:
				numInstr =  self.createNumber("numInstr")
				self.add(numInstr)
				
				(rating, pos) = cntx._handleNode(numInstr, data, pos, self)
				if rating > 2:
					return (rating, pos)
				
				instr = Blob()
				instr.name = "instr"
				instr.length = numInstr.defaultValue
				self.add(instr)
				
				(rating, pos) = cntx._handleNode(instr, data, pos, self)
				if rating > 2:
					return (rating, pos)
				
				# TODO - Create a relation between these guys
			
			return (2, pos)
			
		except:
			raise
			return (4, pos)
	
	def getInternalValue(self, sout = None):
		'''
		Return the internal value of this date element.  This
		value comes before any modifications such as packing,
		padding, truncating, etc.
		
		For Numbers this is the python int value.
		'''
		
		ret = ""
		for child in self:
			if isinstance(child, DataElement):
				ret += child.getValue()
				if sout != None:
					sout.write(child.getValue(), child.getFullDataName())
		
		return ret
	
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
