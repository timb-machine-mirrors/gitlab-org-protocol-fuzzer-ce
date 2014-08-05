from waflib.TaskGen import feature, before_method, after_method, extension
from waflib.Task import Task
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

	webhelp = j(pub, 'asciidoctor-fopub', 'build', 'fopub', 'docbook', 'webhelp')
	conf.env['WEBHELP_DIR'] = webhelp
	conf.env['WEBHELP_XSL'] = j(webhelp, 'xsl', 'webhelp.xsl')

@feature('asciidoc')
@after_method('process_source')
def apply_asciidoc(self):
	# Turn all docbook xml to pdf
	for adoc,lint in getattr(self, 'compiled_tasks', []):
		xml = adoc.outputs[0]
		pdf = xml.change_ext('.pdf')
		fopub = self.create_task('fopub', xml, pdf)

		# ensure fopub runs after xmllint
		#fopub.set_run_after(lint)

		# Install pdf to bin directory
		self.install_as('${BINDIR}/%s' % self.name, pdf, chmod=Utils.O644)

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
def apply_webhelp(self):
	for adoc,lint in getattr(self, 'compiled_tasks', []):
		xml = adoc.outputs[0]

		xsl = self.create_task('webhelp', xml)
		#xsl.env.XSLTPROC_OPTS = xsl.env.WEBHELP_XSL
		#xsl.set_run_after(lint)

		# xsltproc outputs to cwd
		xsl.output_dir = xml.parent.find_dir(self.name)
		if not xsl.output_dir:
			xsl.output_dir = xml.parent.find_or_declare(self.name)

@extension('.adoc')
def adoc_hook(self, node):
	xml = node.change_ext('.%d.xml' % self.idx)

	adoc = self.create_task('asciidoctor', node, xml)
	lint = self.create_task('xmllint', xml)

	try:
		self.compiled_tasks.append((adoc,lint))
	except AttributeError:
		self.compiled_tasks = [(adoc,lint)]
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
		self.cwd = self.output_dir.abspath()

		if os.path.exists(self.cwd):
			try:
				shutil.rmtree(self.cwd)
			except OSError:
				pass

		os.makedirs(self.cwd)

		Task.exec_command(self, cmd, **kw)

	def post_run(self):
		nodes = self.output_dir.ant_glob('**/*', quiet=True)
		for x in nodes:
			x.sig = Utils.h_file(x.abspath())
		self.outputs += nodes
		return Task.post_run(self)

class fopub(Task): 
	run_str = '${FOPUB} ${SRC} ${FOPUB_OPTS}'
	color   = 'PINK'
	vars    = [ 'FOPUB_OPTS' ]
	after   = [ 'xmllint' ]
