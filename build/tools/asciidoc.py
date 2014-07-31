from waflib.TaskGen import feature, before_method, after_method, extension
from waflib.Task import Task
from waflib import Utils, Errors
import os
import re

def configure(conf):
	j = os.path.join
	pub = j(conf.path.abspath(), 'docs', 'publishing')

	conf.find_program('ruby')
	conf.find_program('perl')
	conf.find_program('java')
	conf.find_program('xmllint')

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

@feature('asciidoc')
def apply_asciidoc(self):
	pass

@extension('.adoc')
def adoc_hook(self, node):
	xml = node.change_ext('.xml')
	pdf = xml.change_ext('.pdf')

	adoc = self.create_task('asciidoctor', node, xml)
	lint = self.create_task('xmllint', xml)
	fopub = self.create_task('fopub', xml, pdf)

	# ensure fopub runs after xmllint
	fopub.set_run_after(lint)

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

	# Install pdf to bin directory
	self.install_as('${BINDIR}/%s' % self.name, pdf, chmod=Utils.O644)

#	try:
#		self.compiled_tasks.append(task)
#	except AttributeError:
#		self.compiled_tasks = [task]
#	return task

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
	vars    = ['XMLLINT_OPTS']

class fopub(Task): 
	run_str = '${FOPUB} ${SRC} ${FOPUB_OPTS}'
	color   = 'PINK'
	vars    = ['FOPUB_OPTS']
