import os.path
from waflib.Configure import conf
from waflib.TaskGen import feature, before_method, after_method, extension
from waflib.Task import Task, SKIP_ME, RUN_ME, ASK_LATER, update_outputs, Task
from waflib import Utils, Errors, Logs, Context
import os, shutil, re

re_xi = re.compile('''^(include|image)::(.*?.(adoc|png))\[''', re.M)

def configure(conf):
	j = os.path.join
	v = conf.env

	conf.find_program('ruby')
	conf.find_program('bundle')

	root = j(conf.get_third_party(), 'asciidoctor-pdf')
	gemfile = j(root, 'asciidoctor-pdf.gemspec')

	# Write out a custom Gemfile that points to asciidoctor-pdf
	# This is to 1) include coderay and 2) keep the .lock file
	# out of the source directory
	gem = conf.bldnode.make_node('asciidoctor-pdf.gemspec')

	# Need to use relative path with unix style directory seperators
	rel = os.path.relpath(root, gem.parent.abspath()).replace('\\', '/')

	gem.write('''
source 'https://rubygems.org'

gem 'coderay', '1.1.0'

gemspec :path => "%s"
''' % rel)

	v.ASCIIDOCTOR_PDF = j(root, 'bin', 'asciidoctor-pdf')
	v.ASCIIDOCTOR_PDF_GEMFILE = gem.abspath()
	v.ASCIIDOCTOR_PDF_OPTS = [
		'-a',
		'BUILDTAG=%s' % v.BUILDTAG,
	]
	v.ASCIIDOCTOR_PDF_THEME_DEPS = []
	v.ASCIIDOCTOR_PDF_THEME_OPTS = []
	v.ASCIIDOCTOR_HTML_OPTS = []
	v.ASCIIDOCTOR_HTML_THEME_DEPS = []
	v.ASCIIDOCTOR_HTML_THEME_OPTS = []
	v.ASCIIDOCTOR_OPTS = []

	# Run bundler which will prepare all the prerequisites
	conf.cmd_and_log(v.BUNDLE + [ '--gemfile=%s' % v.ASCIIDOCTOR_PDF_GEMFILE ])

	# Try and find asciidoctor, that is expected to come down
	# as part of the bundle
	adr = conf.find_program('asciidoctor')
	(out,err) = conf.cmd_and_log(adr + ['--version'], output=Context.BOTH)

	if 'Asciidoctor 1.5.2 ' not in out:
		raise Errors.WafError("Expected Asciidoctor 1.5.2 but found:\n%s" % out)

	v.append_value('supported_features', 'asciidoctor-pdf')

@conf
def set_asciidoctor_pdf_theme(self, themes, name):
	if isinstance(themes, str):
		node = self.path.find_dir(themes)
	else:
		node = themes

	v = self.env
	v.ASCIIDOCTOR_PDF_THEME_DEPS = node.ant_glob('**/*')
	v.ASCIIDOCTOR_PDF_THEME_OPTS = [
		'-a',
		'pdf-stylesdir=%s' % node.abspath(),
		'-a',
		'pdf-style=%s' % name,
	]

@conf
def set_asciidoctor_html_theme(self, docinfo):
	if isinstance(docinfo, str):
		node = self.path.find_dir(docinfo)
	else:
		node = docinfo

	v = self.env
	v.ASCIIDOCTOR_HTML_THEME_DEPS = node.ant_glob('**/*')
	v.ASCIIDOCTOR_HTML_THEME_OPTS = [
		'-a',
		'docinfo1',
		'-a',
		'docinfodir=%s' % node.abspath(),
	]

@feature('asciidoctor-pdf')
@before_method('process_source')
def apply_asciidoctor_pdf(self):
	srcs = self.to_nodes(getattr(self, 'source', []))
	if not srcs:
		return

	# Clear source so we don't try and create a compiled task
	self.source = []

	name = getattr(self, 'target', None)
	if not name: name = self.name
	tgt = self.path.find_or_declare(name)
	tsk = self.create_task('asciidoctor_pdf', srcs, tgt)
	inst_to = getattr(self, 'install_path', '${BINDIR}')
	inst = self.install_files(inst_to, tsk.outputs, chmod=Utils.O644)

	tsk.env.append_value('ASCIIDOCTOR_PDF_OPTS', [ '--trace' ])

	self.compiled_tasks = [ tsk ]

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
		tsk.env.append_value('ASCIIDOCTOR_PDF_OPTS', [ '-a', 'images=%s' % img.path_from(srcs[0].parent) ])
		self.images = img

def asciidoctor_scan(self):
	depnodes = [x for x in self.themes()]

	img = getattr(self.generator, 'images', None)
	root = self.inputs[0]

	node_lst = [self.inputs[0]]
	seen = []
	while node_lst:
		nd = node_lst.pop(0)
		if nd in seen: continue
		seen.append(nd)

		code = nd.read()
		for m in re_xi.finditer(code):
			name = m.group(2)
			if img and '{images}' in name:
				name = name.replace("{images}", img.path_from(nd.parent))
			k = nd.parent.find_resource(name)
			if k:
				depnodes.append(k)
				node_lst.append(k)
			else:
				print 'Missing node for dependency: %s' % name
	return [depnodes, ()]

class asciidoctor_pdf(Task):
	run_str = '${BUNDLE} exec ${RUBY} ${ASCIIDOCTOR_PDF} ${ASCIIDOCTOR_PDF_OPTS} ${ASCIIDOCTOR_PDF_THEME_OPTS} -o ${TGT} ${SRC}'
	color   = 'PINK'
	vars    = ['ASCIIDOCTOR_PDF_OPTS', 'ASCIIDOCTOR_PDF_THEME_OPTS']
	scan    = asciidoctor_scan
	themes  = lambda x: x.env.ASCIIDOCTOR_PDF_THEME_DEPS

	def exec_command(self, cmd, **kw):
		env = dict(self.env.env or os.environ)
		env.update(BUNDLE_GEMFILE = self.env['ASCIIDOCTOR_PDF_GEMFILE'])
		kw['env'] = env
		return super(asciidoctor_pdf, self).exec_command(cmd, **kw)

class asciidoctor_html(Task):
	run_str = '${ASCIIDOCTOR} ${ASCIIDOCTOR_HTML_OPTS} ${ASCIIDOCTOR_HTML_THEME_OPTS} -b html5 -o ${TGT} ${SRC}'
	color   = 'PINK'
	ext_out = '.html'
	vars    = ['ASCIIDOCTOR_HTML_OPTS', 'ASCIIDOCTOR_HTML_THEME_OPTS']
	scan    = asciidoctor_scan
	themes  = lambda x: x.env.ASCIIDOCTOR_HTML_THEME_DEPS

class asciidoctor(Task):
	run_str = '${ASCIIDOCTOR} ${ASCIIDOCTOR_OPTS} -o ${TGT} ${SRC}'
	color   = 'PINK'
	vars    = ['ASCIIDOCTOR_OPTS']
	scan    = asciidoctor_scan
	themes  = lambda x: []
