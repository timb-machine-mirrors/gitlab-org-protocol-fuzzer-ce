from waflib.TaskGen import feature, before_method, after_method, extension
from waflib.Task import Task, SKIP_ME, RUN_ME, ASK_LATER, update_outputs
from waflib import Utils, Errors
import os, shutil, re

def configure(conf):
	j = os.path.join
	pub = j(conf.path.abspath(), 'docs', 'publishing')

	conf.find_program('ruby')
	conf.find_program('perl')
	conf.find_program('java')
	conf.find_program('xmllint')
	conf.find_program('xsltproc')

	conf.find_program('asciidoctor', path_list = [ j(pub, 'asciidoctor', 'bin') ], exts = '')
	conf.find_program('fopub', path_list = [ j(pub, 'asciidoctor-fopub') ])

	conf.env['ASCIIDOCTOR_OPTS'] = [
		'-v',
		'-b', 'docbook45',
		'-d', 'article',
	]

	conf.env['FOPUB_OPTS'] = [
		'-param', 'paper.type', 'USletter',
		'-param', 'header.column.widths', '0 1 0',
		'-param', 'footer.column.widths', '0 1 0',
	]

	conf.env['XMLLINT_OPTS'] = [
		'--noout',
		'--valid',
	]

	docbook = j('docs', 'publishing', 'asciidoctor-fopub', 'build', 'fopub', 'docbook')
	conf.env['WEBHELP_ICO'] = j('docs', 'publishing', 'favicon.ico')
	conf.env['WEBHELP_DIR'] = j(docbook, 'webhelp')
	conf.env['WEBHELP_XSL'] = j(conf.path.abspath(), docbook, 'webhelp', 'xsl', 'webhelp.xsl')

	extensions = j(conf.path.abspath(), docbook, 'extensions')
	xerces = j(pub, 'xerces-2_11_0')

	classes = [
		j(extensions, 'webhelpindexer.jar'),
		j(extensions, 'lucene-analyzers-3.0.0.jar'),
		j(extensions, 'lucene-core-3.0.0.jar'),
		j(extensions, 'tagsoup-1.2.1.jar'),
		j(extensions, 'saxon-65.jar'),
		j(xerces, 'xercesImpl.jar'),
		j(xerces, 'xml-apis.jar '),
	]

	conf.env['WEBINDEX_OPTS'] = [
		'-DhtmlDir=docs',
		'-DindexerExcludedFiles=""',
		'-Dorg.xml.sax.driver=org.ccil.cowan.tagsoup.Parser',
		'-Djavax.xml.parsers.SAXParserFactory=org.ccil.cowan.tagsoup.jaxp.SAXFactoryImpl',
		'-cp',
		os.pathsep.join(classes),
		'com.nexwave.nquindexer.IndexerMain',
	]

def runnable_status(self):
	for t in self.run_after:
		if not t.hasrun:
			return ASK_LATER

	if not self.inputs:
		# Get the list of files to install
		# self.path is the same as our task generator's output_dir
		self.source = self.inputs = self.path.ant_glob('**/*', quiet=True)

	ret = Task.runnable_status(self)
	if ret == SKIP_ME:
		return RUN_ME
	return ret

@feature('asciidoc')
@after_method('process_source')
def apply_asciidoc(self):
	# Turn all docbook xml to pdf
	for adoc in getattr(self, 'compiled_tasks', []):
		xml = adoc.outputs[0]
		pdf = xml.change_ext('.pdf')
		fopub = self.create_task('fopub', xml, pdf)

		# Install pdf to bin directory
		inst = self.install_as('${BINDIR}/%s' % self.name, pdf, chmod=Utils.O644)

		# Store inst task in install_extras for packaging
		try:
			self.install_extras.append(inst)
		except AttributeError:
			self.install_extras = [inst]

	# Set path to images relative to xml file
	images = getattr(self, 'images', None)
	if images:
		if isinstance(images, str):
			img = self.path.find_dir(images)
		else:
			img = images
		if not img:
			raise Errors.WafError("image directory not found: %r in %r" % (images, self))
		adoc.env.append_value('ASCIIDOCTOR_OPTS', [ '-a', 'images=%s' % img.path_from(xml.parent) ])

@feature('webhelp')
@after_method('process_source')
def apply_webhelp(self):
	for adoc in getattr(self, 'compiled_tasks', []):
		xml = adoc.outputs[0]

		# xsltproc outputs to cwd
		self.output_dir = xml.parent.find_dir(self.name)
		if not self.output_dir:
			self.output_dir = xml.parent.find_or_declare(self.name)

		xsl = self.create_task('webhelp', xml)
		idx = self.create_task('webindex', xml)
		inst = self.bld.install_files('${BINDIR}/%s' % self.name, [], cwd = self.output_dir, relative_trick = True, chmod = Utils.O644)

		if inst:
			inst.set_run_after(idx)
			inst.runnable_status = lambda inst=inst: runnable_status(inst)

			# Store inst task in install_extras for packaging
			try:
				self.install_extras.append(inst)
			except AttributeError:
				self.install_extras = [inst]

	# Set path to images relative to webroot
	images = getattr(self, 'images', None)
	if images:
		if isinstance(images, str):
			img = self.path.find_dir(images)
		else:
			img = images
		if not img:
			raise Errors.WafError("image directory not found: %r in %r" % (images, self))
		adoc.env.append_value('ASCIIDOCTOR_OPTS', [ '-a', 'images=images' ])

	# Install images to bin directory
	inst = self.bld.install_files('${BINDIR}/%s/docs/images' % self.name, img.ant_glob('**/*'), cwd = img, relative_trick = True, chmod = Utils.O644)
	if inst:
		self.install_extras.append(inst)

	# Install template files to BINDIR
	template = self.bld.root.find_dir(os.path.join(self.env.WEBHELP_DIR, 'template'))
	inst = self.bld.install_files('${BINDIR}/%s/docs' % self.name, template.ant_glob('**/*', excl='favicon.ico'), cwd = template, relative_trick = True, chmod = Utils.O644)
	if inst:
		self.install_extras.append(inst)

	# Install favicon to BINDIR
	ico = self.bld.root.find_resource(self.env.WEBHELP_ICO)
	inst = self.bld.install_files('${BINDIR}/%s' % self.name, ico, cwd = ico.parent, relative_trick = True, chmod = Utils.O644)
	if inst:
		self.install_extras.append(inst)


@extension('.adoc')
def adoc_hook(self, node):
	xml = node.change_ext('.%d.xml' % self.idx)

	adoc = self.create_task('asciidoctor', node, xml)
	lint = self.create_task('xmllint', xml)

	try:
		self.compiled_tasks.append(adoc)
	except AttributeError:
		self.compiled_tasks = [adoc]
	return adoc

re_xi = re.compile('''^(include|image)::([^.]*.(adoc|png))\[''', re.M)

def asciidoc_scan(self):
	p = self.inputs[0].parent
	node_lst = [self.inputs[0]]
	seen = []
	depnodes = []
	while node_lst:
		nd = node_lst.pop(0)
		if nd in seen: continue
		seen.append(nd)

		code = nd.read()
		for m in re_xi.finditer(code):
			name = m.group(2)
			k = p.find_resource(name)
			if k:
				depnodes.append(k)
				node_lst.append(k)
	return [depnodes, ()]

class asciidoctor(Task):
	run_str = '${RUBY} -I. ${ASCIIDOCTOR} ${ASCIIDOCTOR_OPTS} -o ${TGT} ${SRC}'
	color   = 'PINK'
	vars    = ['ASCIIDOCTOR_OPTS']
	scan    = asciidoc_scan

class xmllint(Task):
	run_str = '${XMLLINT} ${XMLLINT_OPTS} ${SRC}'
	color   = 'PINK'
	before  = [ 'fopub', 'webhelp' ]
	vars    = [ 'XMLLINT_OPTS' ]

class webhelp(Task):
	run_str = '${XSLTPROC} ${WEBHELP_XSL} ${SRC[0].abspath()}'
	color   = 'PINK'
	vars    = [ 'WEBHELP_XSL' ]
	after   = [ 'xmllint' ]

	def exec_command(self, cmd, **kw):
		# webhelp outputs all files in cwd
		self.cwd = self.generator.output_dir.abspath()

		if os.path.exists(self.cwd):
			try:
				shutil.rmtree(self.cwd)
			except OSError:
				pass

		os.makedirs(self.cwd)

		kw['cwd'] = self.cwd

		Task.exec_command(self, cmd, **kw)

@update_outputs
class webindex(Task):
	run_str = '${JAVA} ${WEBINDEX_OPTS}'
	color   = 'PINK'
	vars    = [ 'WEBINDEX_OPTS' ]
	after   = [ 'webhelp' ]

	def exec_command(self, cmd, **kw):
		# Force 'cwd' to be output_dir
		kw['cwd'] = self.generator.output_dir.abspath()
		Task.exec_command(self, cmd, **kw)

		# gather the list of output files from webhelp and webindex
		self.outputs = self.generator.output_dir.ant_glob('**/*', quiet=True)

class fopub(Task): 
	run_str = '${FOPUB} ${SRC} ${FOPUB_OPTS}'
	color   = 'PINK'
	vars    = [ 'FOPUB_OPTS' ]
	after   = [ 'xmllint' ]
