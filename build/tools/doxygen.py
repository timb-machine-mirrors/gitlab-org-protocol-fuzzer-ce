#! /usr/bin/env python

from fnmatch import fnmatchcase
import os, os.path, re, stat, tempfile
from waflib import Task, Utils, Node, Logs
from waflib.TaskGen import feature

def configure(conf):
	conf.find_program('doxygen', var='DOXYGEN')

DOXY_STR = '${DOXYGEN} - '
DOXY_FMTS = 'html latex man rft xml'.split()
DOXY_FILE_PATTERNS = '*.' + ' *.'.join('''
c cc cxx cpp c++ java ii ixx ipp i++ inl h hh hxx hpp h++ idl odl cs php php3
inc m mm py f90c cc cxx cpp c++ java ii ixx ipp i++ inl h hh hxx
'''.split())

re_rl = re.compile('\\\\\r*\n', re.MULTILINE)
re_nl = re.compile('\r*\n', re.M)
def parse_doxy(txt):
	tbl = {}
	txt   = re_rl.sub('', txt)
	lines = re_nl.split(txt)
	for x in lines:
		x = x.strip()
		if not x or x.startswith('#') or x.find('=') < 0:
			continue
		if x.find('+=') >= 0:
			tmp = x.split('+=')
			key = tmp[0].strip()
			if key in tbl:
				tbl[key] += ' ' + '+='.join(tmp[1:]).strip()
			else:
				tbl[key] = '+='.join(tmp[1:]).strip()
		else:
			tmp = x.split('=')
			tbl[tmp[0].strip()] = '='.join(tmp[1:]).strip()

	return tbl

def inst_runnable_status(self):
	for t in self.run_after:
		if not t.hasrun:
			return Task.ASK_LATER

	if not self.inputs:
		# Get the list of files to install
		# Set self.path doxygens output_dir
		self.path = self.doxy_tsk.output_dir
		self.source = self.inputs = self.path.ant_glob('**/*', quiet=True)

	ret = Task.Task.runnable_status(self)
	if ret == Task.SKIP_ME:
		return Task.RUN_ME
	return ret

class doxygen(Task.Task):
	vars  = ['DOXYGEN', 'DOXYFLAGS']
	run_str = '${DOXYGEN} ${SRC}'
	color = 'BLUE'

	def runnable_status(self):
		'''
		self.pars are populated in runnable_status - because this function is being
		run *before* both self.pars "consumers" - scan() and run()

		set output_dir (node) for the output
		'''

		for x in self.run_after:
			if not x.hasrun:
				return Task.ASK_LATER

		if not getattr(self, 'pars', None):
			txt = self.inputs[0].read()
			self.pars = parse_doxy(txt)

			# Override with any parameters passed to the task generator
			if getattr(self.generator, 'pars', None):
				for k, v in self.generator.pars.iteritems():
					self.pars[k] = v

			self.doxy_inputs = getattr(self, 'doxy_inputs', [])
			if not self.pars.get('INPUT'):
				self.doxy_inputs.append(self.inputs[0].parent)
			else:
				for i in self.pars.get('INPUT').split():
					if os.path.isabs(i):
						node = self.generator.bld.root.find_node(i)
					else:
						node = self.inputs[0].parent.find_node(i)
					if not node:
						self.generator.bld.fatal('Could not find the doxygen input %r' % i)
					self.doxy_inputs.append(node)

			self.doxy_extras = getattr(self, 'doxy_extras', [])
			for key in [ 'PROJECT_LOGO', 'HTML_EXTRA_STYLESHEET', 'HTML_EXTRA_FILES']:
				items = []
				for i in self.pars.get(key, '').split():
					if os.path.isabs(i):
						node = self.generator.bld.root.find_node(i)
					else:
						node = self.inputs[0].parent.find_node(i)
					if not node:
						self.generator.bld.fatal('Could not find the doxygen %s %r' % (key, i))
					self.doxy_extras.append(node)
					items.append(node)
				self.pars[key] = ' '.join([x.abspath() for x in items])

		if not getattr(self, 'output_dir', None):
			bld = self.generator.bld
			# First try to find an absolute path, then find or declare a relative path

			i = self.pars.get('OUTPUT_DIRECTORY', None)
			if not i:
				self.output_dir = self.inputs[0].parent.get_bld()
			else:
				self.output_dir = bld.root.find_dir(i)

			if not self.output_dir:
				self.output_dir = bld.path.find_or_declare(i)

			self.pars['OUTPUT_DIRECTORY'] = self.output_dir.abspath()

		# Ensure output directory is created
		os.makedirs(self.output_dir.abspath())

		self.signature()
		return Task.Task.runnable_status(self)

	def scan(self):
		exclude_patterns = self.pars.get('EXCLUDE_PATTERNS', '').split()
		file_patterns = self.pars.get('FILE_PATTERNS', '').split()
		if not file_patterns:
			file_patterns = DOXY_FILE_PATTERNS

		if self.pars.get('RECURSIVE') == 'YES':
			file_patterns = [ '**/%s' % x for x in file_patterns ]
			exclude_patterns = [ '**/%s' % x for x in exclude_patterns ]

		nodes = []
		names = []
		for node in self.doxy_inputs:
			if os.path.isdir(node.abspath()):
				for m in node.ant_glob(file_patterns):
					nodes.append(m)
			else:
				nodes.append(node)

		nodes.extend(self.doxy_extras)

		return (nodes, names)

	def exec_command(self, cmd, **kw):
		dct = self.pars.copy()
		# TODO will break if paths have spaces
		dct['INPUT'] = ' '.join([x.abspath() for x in self.doxy_inputs])
		code = '\n'.join(['%s = %s' % (x, dct[x]) for x in self.pars])
		code = code.encode() # for python 3

		try:
			(fd, tmp) = tempfile.mkstemp()
			os.write(fd, code)
			os.close(fd)
			cmd = [ cmd[0], tmp ]
			ret = super(doxygen, self).exec_command(cmd, **kw)
		finally:
			try:
				os.remove(tmp)
			except OSError:
				pass # anti-virus and indexers can keep the files open -_-

		return ret

	def post_run(self):
		nodes = self.output_dir.ant_glob('**/*', quiet=True)
		for x in nodes:
			x.sig = Utils.h_file(x.abspath())
		self.outputs += nodes
		#print self.outputs
		return Task.Task.post_run(self)

class tar(Task.Task):
	"quick tar creation"
	run_str = '${TAR} ${TAROPTS} ${TGT} ${SRC}'
	color   = 'RED'
	after   = ['doxygen']
	def runnable_status(self):
		for x in getattr(self, 'input_tasks', []):
			if not x.hasrun:
				return Task.ASK_LATER

		if not getattr(self, 'tar_done_adding', None):
			# execute this only once
			self.tar_done_adding = True
			for x in getattr(self, 'input_tasks', []):
				self.set_inputs(x.outputs)
			if not self.inputs:
				return Task.SKIP_ME
		return Task.Task.runnable_status(self)

	def __str__(self):
		tgt_str = ' '.join([a.nice_path(self.env) for a in self.outputs])
		return '%s: %s\n' % (self.__class__.__name__, tgt_str)

@feature('doxygen')
def process_doxy(self):
	if not getattr(self, 'doxyfile', None):
		self.generator.bld.fatal('no doxyfile??')

	node = self.doxyfile
	if not isinstance(node, Node.Node):
		node = self.path.find_resource(node)
	if not node:
		raise ValueError('doxygen file not found')

	# the task instance
	dsk = self.create_task('doxygen', node)

	inst_to = getattr(self, 'install_path', '${BINDIR}')
	inst = self.bld.install_files(inst_to, [], cwd = node.parent, relative_trick = True, chmod = Utils.O644)

	if inst:
		inst.doxy_tsk = dsk
		inst.set_run_after(dsk)
		inst.runnable_status = lambda inst=inst: inst_runnable_status(inst)

		# Store inst task in install_extras for packaging
		try:
			self.install_extras.append(inst)
		except AttributeError:
			self.install_extras = [inst]

	if getattr(self, 'doxy_tar', None):
		tsk = self.create_task('tar')
		tsk.input_tasks = [dsk]
		tsk.set_outputs(self.path.find_or_declare(self.doxy_tar))
		if self.doxy_tar.endswith('bz2'):
			tsk.env['TAROPTS'] = ['cjf']
		elif self.doxy_tar.endswith('gz'):
			tsk.env['TAROPTS'] = ['czf']
		else:
			tsk.env['TAROPTS'] = ['cf']

