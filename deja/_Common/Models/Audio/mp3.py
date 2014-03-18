#!/usb/bin/env python

import clr
clr.AddReferenceByPartialName('Peach.Core')

import Peach.Core
import code

VER1 = 3
VER2 = 2
VER2_5 = 0
LAYER3 = 1


bitratemap = {(VER1,LAYER3,1):32, (VER2,LAYER3,1):8, (VER2_5,LAYER3,1):8,
	(VER1,LAYER3,2):40, (VER2,LAYER3,2):16, (VER2_5,LAYER3,2):16,
	(VER1,LAYER3,3):48, (VER2,LAYER3,3):24, (VER2_5,LAYER3,3):24,
	(VER1,LAYER3,4):56, (VER2,LAYER3,4):32, (VER2_5,LAYER3,4):32,
	(VER1,LAYER3,5):64, (VER2,LAYER3,5):40, (VER2_5,LAYER3,5):40,
	(VER1,LAYER3,6):80, (VER2,LAYER3,6):48, (VER2_5,LAYER3,6):48,
	(VER1,LAYER3,7):96, (VER2,LAYER3,7):56, (VER2_5,LAYER3,7):56,
	(VER1,LAYER3,8):112, (VER2,LAYER3,8):64, (VER2_5,LAYER3,8):64,
	(VER1,LAYER3,9):128, (VER2,LAYER3,9):80, (VER2_5,LAYER3,9):80,
	(VER1,LAYER3,10):160, (VER2,LAYER3,10):96, (VER2_5,LAYER3,10):96,
	(VER1,LAYER3,11):192, (VER2,LAYER3,11):112, (VER2_5,LAYER3,11):112,
	(VER1,LAYER3,12):224, (VER2,LAYER3,12):128, (VER2_5,LAYER3,12):128,
	(VER1,LAYER3,13):256, (VER2,LAYER3,13):144, (VER2_5,LAYER3,13):144,
	(VER1,LAYER3,14):320, (VER2,LAYER3,14):160, (VER2_5,LAYER3,14):160
	}


freqmap = {(VER1,0):44100, (VER1,1):48000, (VER1,2):32000, 
	(VER2,0):22050, (VER2,1):24000, (VER2,2):16000, 
	(VER2_5,0):11025, (VER2_5,1):12000, (VER2_5,2):8000
	}

def bitrateIndexLookup(version, layer, val):
	#print "bitrateval= "+str(val)
	if (version,layer,val) in bitratemap:
		return bitratemap[(version,layer,val)]*1000
	return 96000

def sampleRateFrequencyIndexLookup(version, val):
	#print "samplefreqval= "+str(val)
	if (version,val) in freqmap:
		return freqmap[(version,val)]
	return 32000

def calcFrameLength(verobj, layobj, bitrateobj, samplefreqobj, padding):
	if not (verobj and layobj and bitrateobj and samplefreqobj):
		print "NULLOBJ"
		code.InteractiveConsole(locals=locals()).interact()

	version = int(verobj.DefaultValue)
	layer = int(layobj.DefaultValue)	

	if layer==LAYER3:
		bitrate = bitrateIndexLookup(version, layer, int(bitrateobj.DefaultValue))
		samplefreq = sampleRateFrequencyIndexLookup(version, int(samplefreqobj.DefaultValue))

		#print str(144*bitrate/samplefreq + padding)

		#code.InteractiveConsole(locals=locals()).interact()

		return (144*bitrate/samplefreq + padding)
	else:
		print "TODO"
		code.InteractiveConsole(locals=locals()).interact()

def getPaddingBit(layer, bitrate, samplefreq, frameSize):
	return 0
