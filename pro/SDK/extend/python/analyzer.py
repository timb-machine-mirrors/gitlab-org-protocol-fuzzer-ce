
import clr
import clrtype
import System
from System.Reflection import BindingFlags

clr.AddReference("Peach.Core")
clr.AddReference("Peach.Pro")

import Peach.Core
from Peach.Core import Analyzer, Variant
from Peach.Core.Dom import Block, String, DataElement
from Peach.Pro.Core.Analyzers import BasePythonAnalyzer

# Add the special assembly that our Python extensions will 
# appear in. This is the list of assemblies that Peach checks
# for extension types.
for a in System.AppDomain.CurrentDomain.GetAssemblies():
	if a.FullName.startswith("Snippets.scripting"):
		Peach.Core.ClassLoader.AssemblyCache[a.FullName] = a

# Create wrappers for class attributes we will use
AnalyzerAttr = clrtype.attribute(Peach.Core.AnalyzerAttribute)
DescriptionAttr = clrtype.attribute(Peach.Core.DescriptionAttribute)
ParameterAttr = clrtype.attribute(Peach.Core.ParameterAttribute)

class PythonAnalyzer(BasePythonAnalyzer, System.Runtime.Serialization.ISerializable):
	'''Example of adding a custom Analyzer to Peach using only Python'''

	__metaclass__ = clrtype.ClrClass
	_clrnamespace = "PythonExamples"

	_clrclassattribs = [
		System.SerializableAttribute,
		AnalyzerAttr("PythonAnalyzer", True),
		DescriptionAttr("Example Analyzer in Python"),
	]

	@clrtype.accepts()
	def __init__(self):
		pass

	@clrtype.accepts(System.Collections.Generic.Dictionary[clr.GetClrType(str), Variant])
	def __init__(self, args):
		pass

	@clrtype.accepts(DataElement, System.Collections.Generic.Dictionary[DataElement, Peach.Core.Cracker.Position])
	def asDataElement(self, parent, args):
		s = String()
		s.DefaultValue = Variant("Hello From Analyzer\n")

		block = Block(parent.name)
		block.Add(s)

		parent.parent[parent.name] = block

# end
