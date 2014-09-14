
import clr, clrtype

clr.AddReference("Peach.Core")

import System
import Peach.Core
from Peach.Core import Variant, Fault, FaultType
from Peach.Core.Agent import Monitor

# Add the special assembly that our Python extensions will 
# appear in. This is the list of assemblies that Peach checks
# for extension types.
for a in System.AppDomain.CurrentDomain.GetAssemblies():
	if a.FullName.startswith("Snippets.scripting"):
		Peach.Core.ClassLoader.AssemblyCache[a.FullName] = a

# Create wrappers for class attributes we will use
MonitorAttr = clrtype.attribute(Peach.Core.MonitorAttribute)
DescriptionAttr = clrtype.attribute(Peach.Core.DescriptionAttribute)
ParameterAttr = clrtype.attribute(Peach.Core.ParameterAttribute)

class PythonMonitor(Monitor):
	'''Example of adding a custom Monitor to Peach using only Python'''
	
	__metaclass__ = clrtype.ClrClass
	_clrnamespace = "PythonExamples"   
	
	
	_clrclassattribs = [
		MonitorAttr("PythonMonitor", True),
		DescriptionAttr("Example Monitor in Python"),
		ParameterAttr("Param1", clr.GetClrType(str), "Example parameter"),
	]

	@clrtype.accepts(Peach.Core.Agent.IAgent, clr.GetClrType(str),  System.Collections.Generic.Dictionary[clr.GetClrType(str), Variant])
	def __init__(self, **args):
		pass
	
	def StopMonitor(self):
		pass
	
	def SessionStarting (self):
		print ">>>> SESSION STARTING FROM PYTHON"
		pass
 
	def SessionFinished(self):
		pass
 
	@clrtype.accepts(System.UInt32, clr.GetClrType(bool))
	def IterationStarting(self, iterationCount, isReproduction):
		print ">>>> ITERATION STARTING FROM PYTHON"
		self.iterationCount = iterationCount
		pass
 
	@clrtype.returns(clr.GetClrType(bool))
	def IterationFinished(self):
		return False
 
	@clrtype.returns(clr.GetClrType(bool))
	def DetectedFault(self):
		print ">> DETECTED FAULT: "+str(self.iterationCount)
		if self.iterationCount > 1:
			return True
			
		return False
 
	@clrtype.returns(Fault)
	def GetMonitorData(self):
		print ">>> GET MONITOR DATA"
		fault = Fault()
		fault.type = FaultType.Fault
		fault.monitorName = self.Name
		fault.title = "Fault generated from Python!"
		fault.description = "Description from Python."
		fault.exploitability = "Py"
		fault.majorHash = "0000"
		fault.minorHash = "FFFF"
		return fault
 
	@clrtype.returns(clr.GetClrType(bool))
	def MustStop(self):
		return False
 
	@clrtype.returns(Variant)
	def Message(self, name, data):
		return None
		
# end


