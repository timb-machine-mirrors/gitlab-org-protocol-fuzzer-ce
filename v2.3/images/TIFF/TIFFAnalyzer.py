'''
Analyzers that produce data models from Binary blobs

@author: Michael Eddington
@version: $Id$
'''

#
# Copyright (c) 2009 Michael Eddington
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
#   Mikhail Davidov (sirus@haxsys.net)

# $Id$

import sys, os, re, struct

sys.path.append("c:/peach")

from Peach.Engine.dom import *
from Peach.Engine.common import *
from Peach.analyzer import Analyzer

class _Node(object):
	def __init__(self, type, startPos, endPos, value):
		self.type = type
		self.value = value
		self.startPos = startPos
		self.endPos = endPos


class TIFFAnalyzer(Analyzer):
	'''
	Analyzes binary blobs to build data models
	
	 1. Locate strings, char & wchar
	   a. Analyze string for XML
	   b. UTF8/UTF16 and byte order marks
	 2. Find string lengths (relations!) --> Would also give us endian
	 3. Compressed segments (zip, gzip)
	 
	 ?. Look for ASN.1 style data?
	 ?. Look for CRCs
	'''
	
	#: Does analyzer support asParser()
	supportParser = False
	#: Does analyzer support asDataElement()
	supportDataElement = True
	#: Does analyzer support asCommandLine()
	supportCommandLine = False
	#: Does analyzer support asTopLevel()
	supportTopLevel = True
	
	def __init__(self):
		pass
	


	def analyzeBlob(self, data, parent):
	
		print ''
		print ''
		print ''
		print ''
		print "tiff analyzer"
		print 'type:' + parent.parent.parent.name
		
		strt = ''
		inc = 4
		if (parent.parent.parent.name == 'LongArrayUnwrap'):
			print 'using LONG'
			strt = 'L'
			inc = 4
		else:
			print 'using SHORT'
			strt = 'H'
			inc = 2


		print parent.find('ImageCount').getInternalValue()
		DomPrint(0, parent.parent.parent.parent)

		idx = 0
		dlen = len(data)
		print 'length: ' + str(dlen)
		
		while (idx < dlen):
			print struct.unpack_from(strt, data, idx)
			idx += inc

			
		
		return Blob(None, None)
		

	def asDataElement(self, parent, args, dataBuffer):
		'''
		Called when Analyzer is used in a data model.
		
		Should return a DataElement such as Block, Number or String.
		'''
		dom = self.analyzeBlob(dataBuffer, parent)
		
		# Replace parent with new dom
		
		parentOfParent = parent.parent
		dom.name = parent.name
		
		indx = parentOfParent.index(parent)
		del parentOfParent[parent.name]
		parentOfParent.insert(indx, dom)
		
		# now just cross our fingers :)
	
	def asCommandLine(self, args):
		'''
		Called when Analyzer is used from command line.  Analyzer
		should produce Peach PIT XML as output.
		'''
		raise Exception("asCommandLine not supported")
	
	def asTopLevel(self, peach, args):
		'''
		Called when Analyzer is used from top level.
		
		From the top level producing zero or more data models and
		state models is possible.
		'''
		raise Exception("asTopLevel not supported")


if __name__ == "__main__":

	import Ft.Xml.Domlette
	from Ft.Xml.Domlette import Print, PrettyPrint
	
	fd = open("sample.bin", "rb+")
	data = fd.read()
	fd.close()
	
	b = Binary()
	dom = b.analyzeBlob(data)
	data2 = dom.getValue()
	
	if data2 == data:
		print "THEY MATCH"
	else:
		print repr(data2)
		print repr(data)
	
	dict = {}
	doc  = Ft.Xml.Domlette.NonvalidatingReader.parseString("<Peach/>", "http://phed.org")
	xml = dom.toXmlDom(doc.rootNode.firstChild, dict)
	PrettyPrint(doc, asHtml=1)

# end
