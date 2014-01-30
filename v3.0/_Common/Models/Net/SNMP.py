
#!/usr/bin/env python

import clr
clr.AddReferenceByPartialName('Peach.Core')

import Peach.Core

#	Sets the type and value for all empty Value fields (The value for the Object Name pair):
def set_variables(ctx):
	vars_block = ctx.parent.actions[0].dataModel.find('VariableBindings')
	output = ctx.dataModel.find('VariableBindings').parent
	if vars_block:
		#Targets the Value field(s) inside of the VariableBindings Value
		#x[0] == Type, x[1] == Size, x[2] == Value
		variable_bindings = vars_block[0][2][0][0][2]
		for x in variable_bindings:
			if x[2].DefaultValue.ToString() == "":
				x[0].DefaultValue = Peach.Core.Variant(0x06)
				x[2].DefaultValue = Peach.Core.Variant((0x2b,0x06, 0x01, 0x04, 0x01, 0x8f, 0x51, 0x01, 0x01, 0x01, 0x82, 0x29, 0x5d, 0x01, 0x1b, 0x02, 0x02, 0x01))
		#Look into the value that is set and its meaning(s)
		output['VariableBindings'] = vars_block.Clone()
