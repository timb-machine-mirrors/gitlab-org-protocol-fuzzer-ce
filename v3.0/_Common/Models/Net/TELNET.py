#!/usr/bin/env python

from zlib import crc32 
import time

import code

import clr
clr.AddReferenceByPartialName('Peach.Core')

import Peach.Core

def set_to_store(store, key, val):
	store['control'].update({key: val})


def value_in_store(store, val):
	if val in store['control']:
		return True
	return False

def clear_store(ctx):
	store = ctx.parent.parent.parent.context.iterationStateStore
	if 'control' not in store:
		return True
	store['control'] = {}
	return True

def create_control_store(store):
	if 'control' not in store:
		store['control'] = {}
		

def store_and_set_opposite(ctx):
	#code.InteractiveConsole(locals=locals()).interact()
	process_client_output(ctx)
	set_client_opposite(ctx)
		
def process_client_output(ctx):
	store = ctx.parent.parent.parent.context.iterationStateStore
	create_control_store(store)
	opts = ctx.dataModel.find('optCodes')
		
	for x in opts:
		cmd = x[1].DefaultValue
		opt = x[2].DefaultValue
		cmd_opt = str(cmd) + "_" + str(opt)
		if not value_in_store(store, cmd_opt):
			set_to_store(store, cmd_opt, 0)

	
def is_client_sent_option(ctx):
	#code.InteractiveConsole(locals=locals()).interact()
	store = ctx.parent.parent.parent.context.iterationStateStore
	cmd = ctx.parent.actions[6].dataModel.find("command").DefaultValue
	opt = ctx.parent.actions[6].dataModel.find("optionIds").DefaultValue
	
	cmd = int(cmd)
	oppositeCommand_1 = 0
	oppositeCommand_2 = 0

	if cmd == 0xfd or cmd == 0xfe:
		oppositeCommand_1 = 0xfc
	else:
		oppositeCommand_1 = 0xfe
	
	if cmd == 0xfd or cmd == 0xfe:
		oppositeCommand_2 = 0xfb
	else:
		oppositeCommand_2 = 0xfd
	
	val_1 = str(oppositeCommand_1) + "_" + str(opt)
	val_2 = str(oppositeCommand_2) + "_" + str(opt)	

	if val_1 in store['control'] or val_2 in store['control']:
		return True
	return False	
			
	
#declines all options sent by server
def set_client_opposite(ctx):
	commandPath = ctx.dataModel.find("command")
	value = int(commandPath.DefaultValue)
	cmd = 0
	if value == 0xfd or value == 0xfe:
		cmd = 0xfc
	else:
		cmd = 0xfe
	commandPath.DefaultValue = Peach.Core.Variant(cmd)

	