#!/usr/bin/env python

# Import wscript contents from Peach/build/tools/wscript.py

import os.path
from tools import wscript

out = 'slag'
inst = 'output'
appname = 'epeach'
maxdepth = 2

def init(ctx):
	wscript.init(ctx)

def options(opt):
	wscript.options(opt)

def configure(ctx):
	j = os.path.join
	root = ctx.path.abspath()

	ctx.env['NUNIT_PATH'] = j(root, 'Peach', '3rdParty', 'NUnit-2.6.0.12051', 'bin')
	ctx.env['PIN_ROOT']   = j(root, 'Peach', '3rdParty', 'pin')

	wscript.configure(ctx)

def build(ctx):
	wscript.build(ctx)

def go(ctx):
	wscript.go(ctx)
