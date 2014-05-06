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

	if opts is None:
		return False

	for x in opts:
		if len(x) <= 3:
			return False
		cmd = x[1].DefaultValue
		opt = x[2].DefaultValue
		cmd_opt = str(cmd) + "_" + str(opt)
		if not value_in_store(store, cmd_opt):
			set_to_store(store, cmd_opt, 0)


def is_client_sent_option(ctx):
	#code.InteractiveConsole(locals=locals()).interact()
	store = ctx.parent.parent.parent.context.iterationStateStore
	if ctx.parent.actions[6].dataModel.find("command") is None:
		return False
	if ctx.parent.actions[6].dataModel.find("optionIds") is None:
		return False

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

	if commandPath is None:
		return False

	value = int(commandPath.DefaultValue)
	cmd = 0
	if value == 0xfd or value == 0xfe:
		cmd = 0xfc
	else:
		cmd = 0xfe
	commandPath.DefaultValue = Peach.Core.Variant(cmd)

def process_server_output(ctx):
	store = ctx.parent.parent.parent.context.iterationStateStore
	create_negop_store(store)
	create_reply_store(store)

	opts = ctx.dataModel.find('optCodes')

	if opts is None:
		return False

	set_server_opposite(ctx)
	for x in opts:
		if len(x) <= 3:
			return False
		cmd = x[1].DefaultValue
		opt = x[2].DefaultValue
		cmd_opt = str(cmd) + "_" + str(opt)
		if not value_in_reply_store(store, cmd_opt):
			set_to_reply_store(store, cmd_opt, 0)
		if is_value_negop_code(opt) and not value_in_negop_store(store, cmd_opt):
			set_to_negop_store(store, cmd_opt, 0)

#accepts all options sent by server
def set_server_opposite(ctx):
	commandPath = ctx.dataModel.find("command")

	if commandPath is None:
		return False

	value = int(commandPath.DefaultValue)
	cmd = value
	if value == 0xfd:
		cmd = 0xfb
	elif value == 0xfe:
		cmd = 0xfc
	elif value == 0xfb:
		cmd = 0xfd
	elif value == 0xfc:
		cmd = 0xfe
	commandPath.DefaultValue = Peach.Core.Variant(cmd)

def set_to_reply_store(store, key, val):
	store['reply'].update({key: val})

def set_to_negop_store(store, key, val):
	store['negop'].update({key: val})

def value_needs_reply(ctx):
	store = ctx.parent.parent.parent.context.iterationStateStore

	if ctx.dataModel.find('command') is None:
		return False
	if ctx.dataModel.find('optionIds') is None:
		return False

	cmd = ctx.dataModel.find('command').DefaultValue
	opt = ctx.dataModel.find('optionIds').DefaultValue
	return opposite_value_in_negop_store(store, cmd, opt)

def value_in_negop_store(store, cmd):
	return cmd in store['negop']

def opposite_value_in_negop_store(store, cmd, opt):
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


	if val_1 in store['negop']:
		del store['negop'][val_1]
		return True
	elif val_2 in store['negop']:
		del store['negop'][val_2]
		return True
	return False

def is_value_negop_code(val):
	if int(val) == 0x20: #Terminal Speed
		return True
	elif int(val) == 0x27: #New Environment Option
		return True
	elif int(val) == 0x18: #Terminal Type
		return True
	return False

def value_in_reply_store(store, val):
	if val in store['reply']:
		return True
	return False

def clear_negop_store(ctx):
	store = ctx.parent.parent.parent.context.iterationStateStore
	if 'negop' not in store:
		return True
	store['negop'] = {}
	return True

def clear_reply_store(ctx):
	store = ctx.parent.parent.parent.context.iterationStateStore
	if 'reply' not in store:
		return True
	store['reply'] = {}
	return True

def create_negop_store(store):
	if 'negop' not in store:
		store['negop'] = {}

def create_reply_store(store):
	if 'reply' not in store:
		store['reply'] = {}