#!/usr/bin/env python

# Import wscript contents from Peach/build/tools/wscript.py

import os.path
from tools import wscript

out = 'slag'
inst = 'output'
appname = 'EPeach'
maxdepth = 2
peach = 'Peach'

def supported_variant(name):
	return True

def init(ctx):
	wscript.init(ctx)

def options(opt):
	wscript.options(opt)

def configure(ctx):
	wscript.configure(ctx)

def build(ctx):
	subdirs = getattr(ctx, 'subdirs', None)

	if subdirs:
		try:
			subdirs.remove(os.path.join('Peach', 'Peach'))
		except ValueError:
			pass

	wscript.build(ctx)
