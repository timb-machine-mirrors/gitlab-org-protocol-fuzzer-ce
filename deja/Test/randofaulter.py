import clr
clr.AddReferenceByPartialName('Peach.Core')

import Peach.Core

def UpdateMethod(self, context):
	self.method = context.controlIteration.ToString()
