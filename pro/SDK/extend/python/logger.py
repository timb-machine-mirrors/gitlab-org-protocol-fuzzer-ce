
import clr
import clrtype
import System
from System.Reflection import BindingFlags

clr.AddReference("Peach.Core")
clr.AddReference("Peach.Pro")

import Peach.Core
from Peach.Core import Variant, Logger, RunContext
from Peach.Core.Dom import Block, String, DataElement
from Peach.Pro.Core.Loggers import BasePythonLogger

# Add the special assembly that our Python extensions will 
# appear in. This is the list of assemblies that Peach checks
# for extension types.
for a in System.AppDomain.CurrentDomain.GetAssemblies():
	if a.FullName.startswith("Snippets.scripting"):
		Peach.Core.ClassLoader.AssemblyCache[a.FullName] = a

# Create wrappers for class attributes we will use
SerializableAttr = clrtype.attribute(System.SerializableAttribute)
LoggerAttr = clrtype.attribute(Peach.Core.LoggerAttribute)
DescriptionAttr = clrtype.attribute(Peach.Core.DescriptionAttribute)
ParameterAttr = clrtype.attribute(Peach.Core.ParameterAttribute)

class PythonLogger(BasePythonLogger):
	'''Example of adding a custom Logger to Peach using only Python'''

	__metaclass__ = clrtype.ClrClass
	_clrnamespace = "PythonExamples"   

	_clrclassattribs = [
		SerializableAttr(),
		LoggerAttr("PythonLogger", True),
		DescriptionAttr("Example Logger in Python"),
		ParameterAttr("Param1", clr.GetClrType(str), "Example parameter"),
		ParameterAttr("Param2", clr.GetClrType(str), "Optional parameter", "DefaultValue"),
	]

	@clrtype.accepts(System.Collections.Generic.Dictionary[clr.GetClrType(str), Variant])
	def __init__(self, args):
		print '>>> INIT Param1=%s' % (str(args['Param1']))
		pass

	@clrtype.accepts(RunContext)
	def Engine_TestStarting(self, context):
		print ">> TEST STARTING"

	@clrtype.accepts(RunContext)
	def Engine_TestFinished(self, context):
		print ">> TEST FINISHED"

	@clrtype.accepts(RunContext, System.UInt32, System.UInt32)
	def Engine_IterationStarting(self, context, currentIteration, totalIterations):
		print ">> ITERATION STARTING: " + str(currentIteration)

	@clrtype.accepts(RunContext, System.UInt32)
	def Engine_IterationFinished(self, context, currentIteration):
		print ">> ITERATION FINISHED: " + str(currentIteration)

# end


