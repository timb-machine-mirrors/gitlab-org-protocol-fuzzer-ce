
import clr, clrtype

clr.AddReference("Peach.Core")

import System
import Peach.Core
from Peach.Core import Variant
from Peach.Core.Agent import Monitor2, IterationStartingArgs, MonitorData
from Peach.Core.Agent.MonitorData import Info

# Add the special assembly that our Python extensions will 
# appear in. This is the list of assemblies that Peach checks
# for extension types.
for a in System.AppDomain.CurrentDomain.GetAssemblies():
	if a.FullName.startswith("Snippets.scripting"):
		Peach.Core.ClassLoader.AssemblyCache[a.FullName] = a

# Create wrappers for class attributes we will use
MonitorAttr = clrtype.attribute(Peach.Core.Agent.MonitorAttribute)
DescriptionAttr = clrtype.attribute(Peach.Core.DescriptionAttribute)
ParameterAttr = clrtype.attribute(Peach.Core.ParameterAttribute)

class PythonMonitor(Monitor2):
	'''Example of adding a custom Monitor to Peach using only Python'''

	__metaclass__ = clrtype.ClrClass
	_clrnamespace = "PythonExamples"   

	_clrclassattribs = [
		MonitorAttr("PythonMonitor"),
		DescriptionAttr("Example Monitor in Python"),
		ParameterAttr("Param1", clr.GetClrType(str), "Example parameter"),
	]

	@clrtype.accepts(System.Collections.Generic.Dictionary[clr.GetClrType(str), clr.GetClrType(str)])
	def StartMonitor(self, args):
		print ">>>> START MONITOR '%s/%s' FROM PYTHON" % (self.Name, self.Class)
		for kv in args:
			print ">>>>   PARAM '%s' = '%s'" % (kv.Key, kv.Value)
		self.count = 0
		pass

	def StopMonitor(self):
		print ">>>> STOP MONITOR FROM PYTHON"
		pass

	def SessionStarting (self):
		print ">>>> SESSION STARTING FROM PYTHON"
		pass

	def SessionFinished(self):
		print ">>>> SESSION FINISHED FROM PYTHON"
		pass

	@clrtype.accepts(IterationStartingArgs)
	def IterationStarting(self, args):
		print ">>>> ITERATION STARTING FROM PYTHON"
		self.isReproduction = args.IsReproduction
		self.lastWasFault = args.LastWasFault
		self.count += 1
		pass

	def IterationFinished(self):
		print ">>>> ITERATION FINISHED FROM PYTHON"
		pass

	@clrtype.returns(clr.GetClrType(bool))
	def DetectedFault(self):
		fault = (self.count % 2) == 0 or self.isReproduction
		print ">>>> DETECTED FAULT: %s" % fault
		return fault

	@clrtype.returns(MonitorData)
	def GetMonitorData(self):
		print ">>> GET MONITOR DATA"
		data = MonitorData()
		data.Title = "Fault generated from Python"
		data.Fault = MonitorData.Info()
		data.Fault.Description = "Description from Python"
		data.Fault.MajorHash = self.Hash("Major Hash Info Goes Here")
		data.Fault.MinorHash = self.Hash("Minor Hash Info Goes Here")
		data.Fault.Risk = "UNKNOWN"
		data.Fault.MustStop = False
		return data

	def Message(self, name):
		print ">>>> MESSAGE '%s' FROM PYTHON" % name
		pass

# end


