#!/usr/bin/env python

# Import wscript contents from Peach/build/tools/wscript.py

import os.path, sys
from tools import wscript

out = 'slag'
inst = 'output'
appname = 'EPeach'
maxdepth = 2
branch = 1

# Path to peach community
peach = 'Peach'

# Ignore peach commuinity Peach.exe
ignore = [ 'Peach/Peach' ]

def add_tools(tools):
	for i in ['win', 'linux', 'osx']:
		__import__('config.%s' % i)
		mod = sys.modules['config.%s' % i]
		mod.tools.extend(tools)

def supported_variant(name):
	return True

def init(ctx):
	wscript.init(ctx)

def options(opt):
	wscript.options(opt)

def configure(ctx):
	add_tools(['sign', 'tsc'])

	wscript.configure(ctx)

def build(ctx):
	wscript.build(ctx)
