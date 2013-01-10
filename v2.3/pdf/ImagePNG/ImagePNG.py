
#
# Copyright (c) Adobe Systems, Inc.
#
# Author:
#   Michael Eddington
#

from Peach.Publishers.file import FileWriterLauncherGui
import os, sys

class ImagePNGPublisher(FileWriterLauncherGui):

	def __init__(self, filename, windowname, debugger = "false", waitTime = 3):
		FileWriterLauncherGui.__init__(self, filename, windowname, debugger, waitTime)
		
	def close(self):
		'''
		Before we close out file write the postfix data.
		'''
		
		FileWriterLauncherGui.close(self)
		os.system("PngPdfMaker.exe")


# end

