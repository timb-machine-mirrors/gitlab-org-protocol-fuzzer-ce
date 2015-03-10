
import clr
import clrtype
import System
from System.Reflection import BindingFlags

clr.AddReference("Peach.Core")
clr.AddReference("Peach.Pro")

import Peach.Core
from Peach.Core import Variant, Fixup
from Peach.Core.Dom import Block, String, DataElement
from Peach.Pro.Fixups import BasePythonFixup

# Add the special assembly that our Python extensions will 
# appear in. This is the list of assemblies that Peach checks
# for extension types.
for a in System.AppDomain.CurrentDomain.GetAssemblies():
	if a.FullName.startswith("Snippets.scripting"):
		Peach.Core.ClassLoader.AssemblyCache[a.FullName] = a

# Create wrappers for class attributes we will use
FixupAttr = clrtype.attribute(Peach.Core.FixupAttribute)
DescriptionAttr = clrtype.attribute(Peach.Core.DescriptionAttribute)
ParameterAttr = clrtype.attribute(Peach.Core.ParameterAttribute)

class PythonFixup(BasePythonFixup):
	'''
	Example of adding a custom Fixup to Peach using only Python.
	
	BasePythonFixup is a special base class needed to create
	pure python Fixups.
	'''
	
	__metaclass__ = clrtype.ClrClass
	_clrnamespace = "PythonExamples"   
	
	# This array sets the class attributes to use. This
	# is like saying [Fixup(...)] in c#
	_clrclassattribs = [
		System.SerializableAttribute,
		FixupAttr("PythonFixup", True),
		DescriptionAttr("Example Analyzer in Python"),
	]
	
	@clrtype.accepts(DataElement, System.Collections.Generic.Dictionary[clr.GetClrType(str), Variant])
	def __init__(self, parent, args):
		pass
	
	@clrtype.accepts()
	@clrtype.returns(Variant)
	def fixupImpl(self):
		return Variant("hello from python fixup\n")
	

# end


