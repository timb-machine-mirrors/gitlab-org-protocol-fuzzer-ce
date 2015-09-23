# -*- coding: utf-8 -*-

from waflib.Configure import conf
from waflib.TaskGen import feature, before_method, after_method, extension, taskgen_method
from waflib.Task import Task, SKIP_ME, RUN_ME, ASK_LATER, update_outputs
from waflib import Utils, Errors, Logs
import os, shutil, re, sys, zipfile, json
import xml.etree.ElementTree as ET

re_name = re.compile('^:Doctitle: (.*)', re.M)
re_desc = re.compile('^:Description: (.*)', re.M)

def configure(conf):
	v = conf.env
	j = os.path.join

	v.PIT_DOC_TEMPLATE     = '''= ${PIT_TITLE}
Peach Fuzzer, LLC
v{BUILDTAG}
:doctype: book
:compat-mode:
:experimental:
:icons: font
:listing-caption:
:toclevels: 3
:chapter-label:
ifdef::backend-pdf[]
:pagenums:
:source-highlighter: coderay
endif::[]

[abstract]
Copyright © 2015 Peach Fuzzer, LLC. All rights reserved.

This document may not be distributed or used for commercial purposes without the explicit consent of the copyright holders.

Peach Fuzzer® is a registered trademark of Peach Fuzzer, LLC.

Peach Fuzzer contains Patent Pending technologies.

While every precaution has been taken in the preparation of this book, the publisher and authors assume no responsibility for errors or omissions, or for damages resulting from the use of the information contained herein. 

Peach Fuzzer, LLC +
1122 E Pike St +
Suite 1064 +
Seattle, WA 98112

== ${PIT_DESCRIPTION}

include::${PIT_USAGE}[]
'''

@conf
def pit_common_export(self):
	return self.path.ant_glob('Assets/_Common/**')

@conf
def pit_common_doc(self):
	return self.path.ant_glob('Assets/*/*.adoc')

@conf
def pit_common_source(self):
	return self.path.ant_glob('Assets/*/*.xml')

def pit_subcategory(node):
	name = node.name
	k = name.find('.')
	if k >= 0:
		name = name[:k]
	return name.split('_')

@conf
def pit_builder(bld, name, **kw):
	source = Utils.to_list(kw.get('source', bld.pit_common_source()))

	# Make a builder for using the common models that doesn't make a pit zip
	bld(
		name = name,
		features = 'pit',
		export   = kw.get('export', bld.pit_common_export()),
		doc      = kw.get('doc', bld.pit_common_doc()),
		category = kw.get('category', 'Network'),
		use      = [],
	)

	# Make a pit zip for each pit source files
	# that will depend on a common set of models
	use = Utils.to_list(kw.get('use', [])) + [ name ]

	for s in source:
		# Name of zip is name of pit fle w/o extension
		childname,ext = os.path.splitext(str(s))

		# If there are multiple pit files all part of protocol 'XXX'
		# it is invalid to have XXX.xml as a pit, need more descriptive
		# names lile 'XXX_Client.xml' and 'XXX_Server.xml'
		# If there is only one pit file, 'XXX.xml' is considered valid
		if childname == name:
			if len(source) != 1:
				raise Errors.WafError("Error, '%s%s' must have a name starting with '%s_'." % (childname,ext,name))
		elif not childname.startswith(name + '_'):
			Logs.warn("Pit inconsistency in '%s.zip' - '%s%s' should have a name starting with '%s_'" % (name, childname, ext, name))

		bld(
			name     = '%s.zip' % (childname),
			target   = '%s.zip' % (childname),
			features = 'pit',
			source   = [ s ],
			use      = use,
			category = kw.get('category', 'Network'),
			shipping = kw.get('shipping', True),
			pit      = kw.get('pit', name),
		)

@conf
def pit_file_builder(bld, name, **kw):
	source = Utils.to_list(kw.get('source', bld.pit_common_source()))

	if len(source) == 1:
		# Expet pit file to be the same as name of this pit builder
		# IE: name='PNG' and source='PNG.xml'
		childname,ext = os.path.splitext(str(source[0]))
		if childname != name:
			Logs.warn("Pit inconsistency in '%s.zip' - '%s%s' should ne named '%s%s'" % (name, childname, ext, name, ext))

	return bld(
		name     = name,
		target   = name + '.zip',
		features = 'pit',
		category = kw.get('category', 'File'),
		source   = source,
		use      = kw.get('use', []),
		export   = kw.get('export', bld.pit_common_export()),
		doc      = kw.get('doc', bld.pit_common_doc()),
		shipping = kw.get('shipping', True),
		pit      = kw.get('pit', name),
	)

@conf
def pit_net_builder(bld, name, **kw):
	kw['category'] = 'Network'
	return bld.pit_builder(name, **kw)

class pit_idx(Task):
	vars    = [ 'BUILDTAG', 'PIT_CATEGORY' ]
	ext_in  = [ '.config' ]
	ext_out = [ '.json' ]

	def run(self):
		meta = []
		cat = self.env.PIT_CATEGORY

		for s in self.inputs:
			tree = ET.parse(s.abspath())
			root = tree.getroot()

			config = []

			for section in root:
				for item in section:
					item.attrib['type'] = item.tag
					if item.attrib.has_key('min'):
						item.attrib['min'] = int(item.attrib['min'])
					if item.attrib.has_key('max'):
						item.attrib['max'] = int(item.attrib['max'])
					config.append(item.attrib)

			meta.append({
				'name'   : [ cat ] + pit_subcategory(s),
				'pit'    : s.path_from(s.parent.parent),
				'build'  : self.env.BUILDTAG,
				'config' : config,
				'calls'  : [ 'StartIterationEvent', 'ExitIterationEvent' ],
			})

		with open(self.outputs[0].abspath(), "w+") as fd:
			json.dump(meta, fd, indent = 1, sort_keys = True)

@feature('pit')
@before_method('process_source')
def process_pit_source(self):
	srcs = self.to_nodes(getattr(self, 'source', []))

	# Install exports always
	self.install_pits(self.to_nodes(getattr(self, 'export', [])))

	# TaskGen only exists for exports
	if not srcs:
		return

	# Clear sources sinde we don't want waf to try and build them
	self.source = []

	cfgs = []
	extras = []
	exts = [ '.json', '.test' ]

	# Collect .xml.config files as well a extra files to install
	for s in srcs:
		cfgs.append(s.change_ext('.xml.config'))
		for x in exts:
			n = s.parent.find_resource(s.name + x)
			if n: extras.append(n)

	# Generate index.json from all the .config files
	self.idx_task = self.create_task('pit_idx', cfgs, self.path.find_or_declare(self.name + '.index.json'))

	try:
		self.idx_task.env.PIT_CATEGORY = self.category
	except AttributeError:
		raise Errors.WafError("TaskGen missing category attribute: %r" % self)

	assets = self.pit_assets_dir()
	chmod = Utils.O644 << 16L

	# Collect triple of (Node, Name, Attr) for files to zip
	self.zip_inputs = [ (self.idx_task.outputs[0], 'index.json', chmod) ]
	for x in srcs + cfgs:
		self.zip_inputs.append((x, x.path_from(assets), chmod))

	# Install our sources to flattened output folder
	self.install_pits(srcs + cfgs + extras)


@feature('pit')
@before_method('process_pit_source')
def process_pit_docs(self):
	v = self.env

	if not 'asciidoctor-pdf' in v['supported_features']:
		return

	docs = self.to_nodes(getattr(self, 'doc', []))
	if not docs:
		return

	sheet = None
	usage = None

	for x in docs:
		if not sheet and x.name.endswith('_DataSheet.adoc'):
			expected = self.name + '_DataSheet.adoc'
			if x.name != expected:
				Logs.warn("Inconsistent file name, found '%s' but expected '%s'" % (x.name, expected))
			sheet = x
		elif not usage and x.name.endswith('_Usage.adoc'):
			expected = self.name + '_Usage.adoc'
			if x.name != expected:
				Logs.warn("Inconsistent file name, found '%s' but expected '%s'" % (x.name, expected))
			usage = x
		else:
			Logs.warn("Ignoring unrecognized documentation file '%s'" % (x.name))

	if not sheet:
		raise Errors.WafError("No DataSheet for pit %s" % self.name)

	if not usage:
		raise Errors.WafError("No Usage guide for pit %s" % self.name)

	contents = sheet.read()

	try:
		v.PIT_TITLE = re_name.search(contents).group(1)
	except AttributeError, e:
		raise Errors.WafError("Missing :Doctitle: in datasheet '%s'" % sheet.abspath(), e)

	try:
		v.PIT_DESCRIPTION = re_desc.search(contents).group(1)
	except AttributeError, e:
		raise Errors.WafError("Missing :Description: in datasheet '%s'" % sheet.abspath(), e)

	target = self.path.find_or_declare(self.name + '.adoc')

	v.PIT_USAGE = usage.path_from(target.parent)
	v.EMIT_SOURCE = Utils.subst_vars(v.PIT_DOC_TEMPLATE, v)

	img_path = sheet.parent.path_from(target.parent)
	v.append_value('ASCIIDOCTOR_PDF_OPTS', [ '-a', 'images=%s' % img_path ])

	tsk = self.create_task('emit', None, [ target ])
	adoc = tsk.outputs[0]

	# Make html version of datasheet
	v.append_value('ASCIIDOCTOR_OPTS', [ '-d', 'article', '-a', 'last-update-label!' ])
	tsk = self.create_task('asciidoctor_html', sheet, sheet.change_ext('.html'))
	self.install_files('${BINDIR}/docs/datasheets', tsk.outputs)

	self.pdf_task = self.create_task('asciidoctor_pdf', adoc, adoc.change_ext('.pdf'))
	self.install_files('${BINDIR}/docs', self.pdf_task.outputs)

	# Don't save pdf task outputs in self.zip_inputs just yet
	# That will happen when we process the use parameter

@feature('pit')
@after_method('process_source')
def make_pit_zip(self):
	# Only make the zip if we made a .json manifest
	if not hasattr(self, 'idx_task'):
		return

	chmod = Utils.O644 << 16L

	# Collect triple of (Node, Name, Attr) for dependencies to zip
	self.collect_pit_deps(self.name, [], chmod)

	# Collecting the deps might have filled in our pdf_task
	pdf = getattr(self, 'pdf_task', None)
	if not pdf:
		if getattr(self, 'shipping', True):
			Logs.warn("No documentation for shipping pit '%s'" % self.name)
	else:
		self.zip_inputs.append((pdf.outputs[0], pdf.outputs[0].name, chmod))

	# Create the zip
	zip_srcs = [ x for x,d,a in self.zip_inputs ]
	zip_target = self.path.find_or_declare(self.target)
	self.zip_task = self.create_task('zip', zip_srcs, zip_target)
	self.install_files('${BINDIR}', self.zip_task.outputs)

class PitList:
	def __str__(self):
		return json.dumps(self.__pits, indent=4)

	def __repr__(self):
		return self.__pits.__repr__() + 'x'

	def __iter__(self):
		return self.__pits.__iter__()

	def __init__(self):
		self.__pits = []

	def add_pit_zip(self, tg):
		rec = next( (x for x in self.__pits if x['name'] == tg.pit), None)
		if not rec:
			rec = { 'name' : tg.pit, 'archives' : [] }
			self.__pits.append(rec)
			self.__pits.sort(key=lambda x: x['name'])
		rec['archives'].append('pits/%s' % tg.zip_task.outputs[0].name)

def verify_shipping_packs(ctx):
	shipping_packs = getattr(ctx, 'shipping_packs', None)
	if not shipping_packs:
		raise Errors.WafError("No shipping packs registered. Add \"bld.shipping_packs='xxx.json'\" to a wscript.");

	shipping_pits = ctx.shipping_pits_task.env.EMIT_SOURCE
	shipping_packs = json.loads(shipping_packs.read())
	
	for pack in shipping_packs:
		for pit in pack['pits']:
			shipping_pits = [ x for x in shipping_pits if x['name'] != pit ]

	if shipping_pits:
		raise Errors.WafError("The following shipping pits are not referenced: %r" % shipping_pits)

@feature('pit')
@after_method('make_pit_zip')
def make_shipping_pits(self):
	if not getattr(self, 'shipping', True):
		return

	zip_task = getattr(self, 'zip_task', None)
	if not zip_task:
		return

	tsk = getattr(self.bld, 'shipping_pits_task', None)
	if not tsk:
		tg = self.bld(name = 'shipping_pits')
		out = tg.path.find_or_declare('shipping_pits.json')
		tsk = tg.create_task('emit', [], out)
		tg.install_files('${BINDIR}', tsk.outputs)
		tsk.env.EMIT_SOURCE = PitList()
		self.bld.shipping_pits_task = tsk
		self.bld.add_post_fun(verify_shipping_packs)

	tsk.env.EMIT_SOURCE.add_pit_zip(self)

@taskgen_method
def install_pits(self, srcs):
	if srcs:
		self.bld.install_files('${BINDIR}', srcs, relative_trick = True, cwd = self.path)

@taskgen_method
def pit_assets_dir(self):
	return self.path.find_dir('Assets')

@taskgen_method
def collect_pit_deps(self, name, seen, chmod):
	# Prevent infinite looping
	seen.append(name)

	try:
		y = self.bld.get_tgen_by_name(name)
	except Errors.WafError:
		return

	y.post()

	# Only consider deps from pit builders
	if 'pit' not in y.features:
		return

	deps = y.to_nodes(getattr(y, 'export', []))
	assets = y.pit_assets_dir()

	# Save off the deps for inclusion in our pit zip
	for x in deps:
		self.zip_inputs.append((x, x.path_from(assets), chmod))

	other_doc = getattr(y, 'pdf_task', None)
	if other_doc and self.name.startswith(y.name):
		if not hasattr(self, 'pdf_task'):
			self.pdf_task = other_doc
		elif self.pdf_task.generator != self:
			raise Errors.WafError("Attempting to include multiple docs in pit zip %s: '%r' and '%r'" % (self.name, self.pdf_task, other_doc))

	# Recursivley collect dependencies
	for x in self.to_list(getattr(y, 'use', [])):
		if x not in seen:
			self.collect_pit_deps(x, seen, chmod)
