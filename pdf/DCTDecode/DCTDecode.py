
#
# Copyright (c) Adobe Systems, Inc.
#
# Author:
#   Michael Eddington
#

from Peach.Publishers.file import FileWriterLauncherGui
import os, sys

class DCTDecodePublisher(FileWriterLauncherGui):
	'''
	Custom publisher extension for Reader JPXDecode fuzzer. This
	publisher will wrap the JPEG2000 image with proper PDF stuffs.
	'''

	def __init__(self, filename, windowname, debugger = "false", waitTime = 3):
		FileWriterLauncherGui.__init__(self, filename, windowname, debugger, waitTime)
		self.withNode = True
		self.dataNode = None
		self.closed = False
	
	def sendWithNode(self, data, dataNode):
		print "> Writing file"
		self.closed = False
		self.dataNode = dataNode
		return self.send(data)
	
	def callWithNode(self, method, args, argNodes):
		print "> Call to start process"
		return self.call(method, args)
	
	def close(self):
		if self.closed:
			return
		
		print "> Closing file"
		
		self.closed = True
		
		try:
			os.unlink("fuzzed.pdf")
		except:
			pass
		
		FileWriterLauncherGui.close(self)
		os.system("JpegPdfMaker.exe \"" + self.dataNode.parent.data.fileName + "\"")


# end

