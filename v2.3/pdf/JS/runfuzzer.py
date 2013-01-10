
#
# Fixup Acrobat JS Fuzzer
# Copyright (c) Adobe Systems, Inc.
#
# This script takes the non-working JS PIT and generates
# a new PIT and Peach run for every data model.  Peach is
# run via this script.
#
# Author:
#   Michael Eddington
#

from Ft.Xml import Parse
from Ft.Xml.Domlette import Print, PrettyPrint
import os, sys

# #############################################################################

def FindFolderByName(root, folderName):
	'''
	Recursivly search for a folder name folderName
	and return the full path starting with root.
	'''
	
	folderNameLength = len(folderName)
	
	for f in os.listdir(root):
		f = os.path.join(root, f)
		if not os.path.isdir(f):
			continue
		
		if f[len(f)-folderNameLength:] == folderName:
			return f

	for f in os.listdir(root):
		f = os.path.join(root, f)
		if not os.path.isdir(f):
			continue
		
		ret = FindFolderByName(f, folderName)
		if ret != None:
			return ret
	
	return None

def FindNodeByName(node, name):
	'''
	Helper function to locate a data element with a name attribute
	that matches the name parameter.
	'''
	
	for child in node.childNodes:
		if hasattr(child, 'getAttributeNS') and child.getAttributeNS(None, 'name') == name:
			return child
	
	return None

def DeepFindNodeByName(node, name):
	'''
	Helper function to locate a data element with a name attribute
	that matches the name parameter.
	
	This function will recurse into child elements.
	'''
	
	for child in node.childNodes:
		if hasattr(child, 'getAttributeNS') and child.getAttributeNS(None, 'name') == name:
			return child
	
	for child in node.childNodes:
		if hasattr(child, 'childNodes'):
			ret = DeepFindNodeByName(child, name)
			if ret != None:
				return ret
	
	return None

def GenerateUnnamedDataElements(node):
	'''
	Will generate (yield) data elements that are not named.  Unnamed
	elements are always parameter values.
	'''
	
	for child in node.childNodes:
		if (child.nodeName == 'String' or child.nodeName == 'Number') and not child.hasAttributeNS(None, 'name'):
			yield child

# #############################################################################

print ""
print "| Fixup Acrobat JS Fuzzer"
print "| Copyright (c) Adobe Systems Inc."
print "| Michael Eddington\n"

if len(sys.argv) < 2:
	print "Syntax: runfuzzer.py -a"
	print "Syntax: runfuzzer.py ModelName\n"
	
	print "  -a        Fuzz all data models"
	print "  ModelName A data model to fuzz\n"
	
	print "Examples:"
	print "  runfuzzer.py -a"
	print "  runfuzzer.py AlternatePresentation_start"
	print "  runfuzzer.py AnnotRichMedia_callAS\n"
	
	sys.exit()


skiptoModel = None
if sys.argv[1] != '-a':
	skiptoModel = sys.argv[1]
	
if os.path.exists("logs"):
	print "Error: Folder 'logs' already exists.  Please remove"
	print "       before starting fuzzing run.\n"
	
	sys.exit()

# Note: We must start the agent first else a file handle
#       to status.txt will stop us from moving files.
print " * Starting Peach Agent"
os.system("start \"Local Peach Agent\" \"c:\\peach\\peach\" -a")

docInput = Parse("input.xml")
docOutput = Parse("template.xml")

actionTemplateNode = docOutput.xpath("//Action[@name='T']")[0]
peachNode = docOutput.xpath('//Peach')[0]
stateNode = docOutput.xpath("//State[@name='start']")[0]
closeActionNode = docOutput.xpath("//Action[@type='close']")[0]

stateNode.removeChild(actionTemplateNode)

# Locate every target data model in input
for dataModelNode in docInput.xpath('//DataModel'):
	
	if dataModelNode.nodeName != 'DataModel':
		continue
	
	if skiptoModel != None and dataModelNode.getAttributeNS(None, 'name') != skiptoModel:
		continue
	
	print " * Converting: %s" % dataModelNode.getAttributeNS(None, 'name')
	
	# Copy actionTemplateNode
	actionNode = actionTemplateNode.cloneNode(True)
	
	# Convert excludable section
	excludeBlockNode = FindNodeByName(dataModelNode, 'excludable')
	excludeString = u""
	
	for es in excludeBlockNode.childNodes:
		if es.nodeName == "String" or es.nodeName == "Number":
			excludeString += es.getAttributeNS(None, 'value')
		
	DeepFindNodeByName(actionNode,'Value').setAttributeNS(None, 'value', excludeString)
	stateNode.insertBefore(actionNode, closeActionNode)
	
	# Move parameters over
	paramTemplate = FindNodeByName(actionNode, 'P')
	actionNode.removeChild(paramTemplate)
	
	cnt = 0
	for param in GenerateUnnamedDataElements(dataModelNode):
		cnt += 1
		
		paramNode = paramTemplate.cloneNode(True)
		paramNode.setAttributeNS(None, 'name', 'P%d'%cnt)
		
		if param.nodeName == 'String':
			paramNode.childNodes[1].setAttributeNS(None, 'ref', 'StringParam')
		else:
			paramNode.childNodes[1].setAttributeNS(None, 'ref', 'NumberParam')
		
		actionNode.appendChild(paramNode)
	
	# Write XML to disk
	fd = open("out.xml", "wb")
	Print(docOutput, stream=fd, encoding='us-ascii')
	fd.close()
	
	# Run fuzzer!
	print " * Starting fuzzing run for:", dataModelNode.getAttributeNS(None, 'name')
	
	os.system("c:\\peach\\peach out.xml")
	
	# Collect faults (if any)
	faultsFolder = FindFolderByName("logs", "Faults")
	if faultsFolder != None:
		faultPath = os.path.join("Faults", dataModelNode.getAttributeNS(None, 'name'))
		
		if not os.path.exists("Faults"):
			os.mkdir("Faults")
		
		if not os.path.exists(faultPath):
			os.mkdir(faultPath)
			os.system("move %s %s" % (faultsFolder, faultPath))
			
		else:
			cnt = 0
			while os.path.exists(faultPath):
				cnt+=1
				faultPath = os.path.join(faultPath, "Faults_%d")
			
			os.system("move %s %s" % (faultsFolder, faultPath))
	
	# Remove action from state
	stateNode.removeChild(actionNode)
	
	# Loop to next target
	#break

print "\n\n Done fuzzing all JS targets \n\n"

# end
