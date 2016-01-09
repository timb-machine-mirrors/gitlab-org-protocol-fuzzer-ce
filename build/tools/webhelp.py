import os.path, shutil
from waflib.Configure import conf
from waflib.TaskGen import feature, before_method, after_method, extension
from waflib.Task import Task, SKIP_ME, RUN_ME, ASK_LATER, update_outputs, Task
from waflib.Node import Node
from waflib import Utils, Errors, Logs, Context

def configure(conf):
	j = os.path.join
	v = conf.env

	if 'asciidoctor-pdf' not in v.supported_features:
		raise Errors.WafError("asciidoctor-pdf feature is missing")

	conf.find_program('java')
	conf.find_program('xmllint')
	conf.find_program('xsltproc')

	v['XMLLINT_OPTS'] = [
		'--noout',
		'--dtdvalid',
		j(conf.get_third_party(), 'docbook-5.0', 'dtd', 'docbook.dtd')
	]

	xsl = j(conf.get_third_party(), 'docbook-xsl-ns-1.78.1')

	v['WEBHELP_DIR'] = j(xsl, 'webhelp')
	v['WEBHELP_XSL'] = j(xsl, 'webhelp', 'xsl', 'webhelp.xsl')

	extensions = j(conf.get_third_party(), 'docbook-xsl-ns-1.78.1', 'extensions')

	classes = [
		j(extensions, 'webhelpindexer.jar'),
		j(extensions, 'tagsoup-1.2.1.jar'),
		j(extensions, 'lucene-analyzers-3.0.0.jar'),
		j(extensions, 'lucene-core-3.0.0.jar'),
	]

	conf.env['WEBINDEX_OPTS'] = [
		'-DindexerLanguage=en',
		'-DhtmlExtension=html',
		'-DdoStem=true',
		'-DindexerExcludedFiles=""',
		'-Dorg.xml.sax.driver=org.ccil.cowan.tagsoup.Parser',
		'-Djavax.xml.parsers.SAXParserFactory=org.ccil.cowan.tagsoup.jaxp.SAXFactoryImpl',
		'-classpath',
		os.pathsep.join(classes),
		'com.nexwave.nquindexer.IndexerMain',
	]

@conf
def set_webhelp_theme(self, path):
	if isinstance(path, str):
		node = self.path.find_dir(path)
	else:
		node = path
	if not node:
		raise Errors.WafError("webhelp theme directory not found: %r in %r" % (path, self))

	self.env.WEBHELP_THEME_PATH = node
	self.env.WEBHELP_THEME_DEPS = node.ant_glob('**/*')

def runnable_status(self):
	for t in self.run_after:
		if not t.hasrun:
			return ASK_LATER

	if not self.inputs:
		# Get the list of files to install
		self.source = self.inputs = self.path.ant_glob('**/*', quiet=True)

	ret = Task.runnable_status(self)
	if ret == SKIP_ME:
		return RUN_ME
	return ret

def install_webhelp(self, inst_to, srcs, cwd, tsk = None):
	inst = self.bld.install_files(inst_to, srcs, cwd = cwd, relative_trick = True, chmod = Utils.O644)

	if inst:
		if tsk:
			inst.set_run_after(tsk)
			inst.runnable_status = lambda inst=inst: runnable_status(inst)

		# Store inst task in install_extras for packaging
		try:
			self.install_extras.append(inst)
		except AttributeError:
			self.install_extras = [inst]

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
	out = xml.parent.get_bld().make_node(self.name)
	out.mkdir()

	doc = self.create_task('asciidoctor', srcs, xml)
	# Disable xmllint until dtd verification for docbook 5 is fixed with asciidoctor 1.5.4
	#lnt = self.create_task('xmllint', xml)
	hlp = self.create_task('webhelp', xml)
	idx = self.create_task('webindex', xml)

	idx.set_run_after(hlp)
	#hlp.set_run_after(lnt)

	self.output_dir = out

	self.env.append_value('ASCIIDOCTOR_OPTS', [
		'-v',
		'-d',
		'article',
		'-b',
		'docbook',
		'-a'
		'images=images',
	])

	hlp.env['OUTPUT_DIR'] = out.path_from(self.bld.bldnode).replace('\\', '/') + '/'
	idx.env['OUTPUT_DIR'] = '-DhtmlDir=%s' % out.path_from(self.bld.bldnode)

	# Set a reasonable default install_path
	inst_to = getattr(self, 'install_path', '${BINDIR}/%s' % self.name)

	install_webhelp(self, inst_to, [], out, idx)

	images = getattr(self, 'images', None)
	if images:
		if isinstance(images, str):
			img = self.path.find_dir(images)
		else:
			img = images
		if not img:
			raise Errors.WafError("image directory not found: %r in %r" % (images, self))
		self.images = img

		# Install image files
		install_webhelp(self, '%s/images' % inst_to, img.ant_glob('**/*'), img)

	root = self.bld.launch_node()

	# Install user template files
	if self.env.WEBHELP_THEME_DEPS:
		install_webhelp(self, inst_to, self.env.WEBHELP_THEME_DEPS, self.env.WEBHELP_THEME_PATH)

	# Install template files not included by the user
	template = os.path.relpath(os.path.join(self.env.WEBHELP_DIR, 'template'), self.path.abspath())
	node = self.path.find_dir(template)
	if not node:
		raise Errors.WafError("webhelp template directory not found at %r in %r" % (template, self))

	theme = [ x.path_from(self.env.WEBHELP_THEME_PATH) for x in self.env.WEBHELP_THEME_DEPS ]
	files = [ x for x in node.ant_glob('**/*') if x.path_from(node) not in theme ]

	if files:
		install_webhelp(self, inst_to, files, node)

class xmllint(Task):
	run_str = '${XMLLINT} ${XMLLINT_OPTS} ${SRC}'
	color   = 'PINK'
	before  = [ 'webhelp' ]
	vars    = [ 'XMLLINT_OPTS' ]

@update_outputs
class webhelp(Task):
	run_str = '${XSLTPROC} --stringparam base.dir ${OUTPUT_DIR} ${WEBHELP_XSL} ${SRC}'
	color   = 'PINK'
	vars    = [ 'WEBHELP_XSL', 'OUTPUT_DIR' ]
	after   = [ 'xmllint' ]

	def exec_command(self, cmd, **kw):
		if os.path.exists(self.generator.output_dir.abspath()):
			try:
				shutil.rmtree(self.generator.output_dir.abspath())
			except OSError:
				pass

		os.makedirs(self.generator.output_dir.abspath())

		ret = super(webhelp, self).exec_command(cmd, **kw)

		if not ret:
			# gather the list of output files from webhelp
			# @update_outputs will make waf generate node signatures
			self.outputs = self.generator.output_dir.ant_glob('*', quiet=True)

		return ret

@update_outputs
class webindex(Task):
	run_str = '${JAVA} ${OUTPUT_DIR} ${WEBINDEX_OPTS} '
	color   = 'PINK'
	vars    = [ 'WEBINDEX_OPTS', 'OUTPUT_DIR' ]
	after   = [ 'webhelp' ]

	def exec_command(self, cmd, **kw):
		ret = super(webindex, self).exec_command(cmd, **kw)

		if not ret:
			# gather the list of output files from webindex
			# @update_outputs will make waf generate node signatures
			self.outputs = self.generator.output_dir.ant_glob('search/*', quiet=True)

		return ret

