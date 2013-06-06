#!/usr/bin/env python

# Import wscript contents from Peach/build/tools/wscript.py

import os.path
from tools import wscript

out = 'slag'
inst = 'output'
appname = 'epeach'
maxdepth = 2

def supported_variant(name):
	return True

def init(ctx):
	wscript.init(ctx)

def options(opt):
	wscript.options(opt)

def configure(ctx):
	j = os.path.join
	root = ctx.path.abspath()

	ctx.env['NUNIT_PATH'] = j(root, 'Peach', '3rdParty', 'NUnit-2.6.0.12051', 'bin')
	ctx.env['MDOC_PATH']  = j(root, 'Peach', '3rdParty', 'mdoc-net-2010-01-04')
	ctx.env['PIN_ROOT']   = j(root, 'Peach', '3rdParty', 'pin')

	wscript.configure(ctx)

def build(ctx):
	subdirs = getattr(ctx, 'subdirs', None)

	if subdirs:
		try:
			subdirs.remove(os.path.join('Peach', 'Peach'))
		except ValueError:
			pass

	wscript.build(ctx)
