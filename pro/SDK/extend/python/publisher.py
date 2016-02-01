
import clr, clrtype

clr.AddReference("Peach.Core")
clr.AddReference("NLog")

import System
import NLog
import json

import Peach.Core
from Peach.Core import Publisher, Variant
from Peach.Core.IO import BitwiseStream

# Add the special assembly that our Python extensions will 
# appear in. This is the list of assemblies that Peach checks
# for extension types.
for a in System.AppDomain.CurrentDomain.GetAssemblies():
	if a.FullName.startswith("Snippets.scripting"):
		Peach.Core.ClassLoader.AssemblyCache[a.FullName] = a

# Create wrappers for class attributes we will use
PublisherAttr = clrtype.attribute(Peach.Core.PublisherAttribute)
DescriptionAttr = clrtype.attribute(Peach.Core.DescriptionAttribute)
ParameterAttr = clrtype.attribute(Peach.Core.ParameterAttribute)

class PythonPublisher(Publisher):
	'''Example of adding a custom Monitor to Peach using only Python'''

	__metaclass__ = clrtype.ClrClass
	_clrnamespace = "PythonExamples"


	_clrclassattribs = [
		System.SerializableAttribute,
		PublisherAttr("PythonPublisher", True),
		DescriptionAttr("Example Publisher in Python"),
	]

	logger = None

	@property
	@clrtype.accepts()
	@clrtype.returns(NLog.Logger)
	def Logger(self):
		if self.logger == None:
			self.logger = NLog.LogManager.GetLogger("PythonPublisher")
		return self.logger

	@clrtype.accepts(System.Collections.Generic.Dictionary[clr.GetClrType(str), Variant])
	def __init__(self, args):
		pass

	@clrtype.accepts(BitwiseStream)
	def OnOutput(self, data):
		'''Output data as a json string'''

		out = ""
		for i in range(data.Length):
			out += chr(data.ReadByte())

		print json.dumps(out)

# end


