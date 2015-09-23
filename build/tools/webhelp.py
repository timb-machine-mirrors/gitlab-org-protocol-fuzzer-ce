import os.path, shutil
from waflib.TaskGen import feature, before_method, after_method, extension
from waflib.Task import Task, SKIP_ME, RUN_ME, ASK_LATER, update_outputs, Task
from waflib import Utils, Errors, Logs, Context

def configure(conf):
	j = os.path.join
	v = conf.env
	pub = j(conf.path.abspath(), 'docs', 'publishing')

	if 'asciidoctor-pdf' not in v.supported_features:
		raise Errors.WafError("asciidoctor-pdf feature is missing")

	conf.find_program('java')
	conf.find_program('xmllint')
	conf.find_program('xsltproc')

	conf.env.append_value('SGML_CATALOG_FILES', [ j(pub, 'docbook-xml-4.5', 'catalog.xml') ])

	conf.env['XMLLINT_OPTS'] = [
		'--catalogs',
		'--nonet',
		'--noout',
		'--valid',
	]

	docbook = j('docs', 'publishing', 'docbook-xsl-1.78.1')
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

def webhelp_inst_runnable_status(self):
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

@feature('webhelp')
@before_method('process_source')
def apply_webhelp(self):
	srcs = self.to_nodes(getattr(self, 'source', []))
	if not srcs:
		return

	if len(srcs) != 1:
		raise Errors.WafError("webhelp feature only supports a single source")

	# Clear source so we don't try and create a compiled task
	self.source = []

	name = getattr(self, 'target', None)
	if not name: name = self.name + '.xml'
	xml = self.path.find_or_declare(name)

	tsk = self.create_task('asciidoctor', srcs, xml)
	self.create_task('xmllint', xml)

	if getattr(self, 'dryrun', False):
		idx = None
	else:
		self.create_task('webhelp', xml)
		idx = self.create_task('webindex', xml)

	tsk.env.append_value('ASCIIDOCTOR_OPTS', [
		'-v',
		'-d',
		'article',
		'-b',
		'docbook45',
	])

	# xsltproc outputs to cwd
	self.output_dir = xml.parent.find_dir(self.name)
	if not self.output_dir:
		self.output_dir = xml.parent.find_or_declare(self.name)

	# docbook-xsl will puts all docs in a 'docs' subfolder
	# so include this in our cwd so it is stripped in the BINDIR
	cwd = self.output_dir.find_or_declare('docs/file').parent

	inst_to = getattr(self, 'install_path', '${BINDIR}/%s' % self.name)
	inst = self.bld.install_files(inst_to, [], cwd = cwd, relative_trick = True, chmod = Utils.O644)

	if inst and idx:
		inst.set_run_after(idx)
		inst.runnable_status = lambda inst=inst: webhelp_inst_runnable_status(inst)

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
		tsk.env.append_value('ASCIIDOCTOR_OPTS', [ '-a', 'images=images' ])

		# Install images to bin directory
		inst = self.bld.install_files('%s/images' % inst_to, img.ant_glob('**/*'), cwd = img, relative_trick = True, chmod = Utils.O644)
		if inst:
			try:
				self.install_extras.append(inst)
			except AttributeError:
				self.install_extras = [inst]

	root = self.bld.launch_node()

	# Install template files to BINDIR
	template = root.find_dir(os.path.join(self.env.WEBHELP_DIR, 'template'))
	inst = self.bld.install_files(inst_to, template.ant_glob('**/*', excl='favicon.ico'), cwd = template, relative_trick = True, chmod = Utils.O644)
	if inst:
		self.install_extras.append(inst)

class xmllint(Task):
	run_str = '${XMLLINT} ${XMLLINT_OPTS} ${SRC}'
	color   = 'PINK'
	before  = [ 'webhelp' ]
	vars    = [ 'XMLLINT_OPTS' ]

	def exec_command(self, cmd, **kw):
		env = dict(self.env.env or os.environ)
		env.update(SGML_CATALOG_FILES = ';'.join(self.env['SGML_CATALOG_FILES']))
		kw['env'] = env

		return super(xmllint, self).exec_command(cmd, **kw)

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

		return super(webhelp, self).exec_command(cmd, **kw)

@update_outputs
class webindex(Task):
	run_str = '${JAVA} ${WEBINDEX_OPTS}'
	color   = 'PINK'
	vars    = [ 'WEBINDEX_OPTS' ]
	after   = [ 'webhelp' ]

	def exec_command(self, cmd, **kw):
		# Force 'cwd' to be output_dir
		kw['cwd'] = self.generator.output_dir.abspath()
		ret = super(webindex, self).exec_command(cmd, **kw)

		if not ret:
			# gather the list of output files from webhelp and webindex
			self.outputs = self.generator.output_dir.ant_glob('**/*', quiet=True)

		return ret

	
'''	
	inst_to = getattr(self, 'install_path', '${BINDIR}')
	inst = self.install_files(inst_to, tsk.outputs, chmod=Utils.O644)

	tsk.env.append_value('ASCIIDOCTOR_PDF_OPTS', [ '--trace' ])

	# Store inst task in install_extras for packaging
	try:
		self.install_extras.append(inst)
	except AttributeError:
		self.install_extras = [inst]

'''