from waflib.TaskGen import feature, before_method, after_method, extension
from waflib import Task, Utils, Logs, Configure, Context, Options, Errors
import re, os.path

refs = re.compile('<reference\s+path="(.*)"\s*/>', re.M)

def configure(conf):
	v = conf.env

	v['TSC_FLAGS'] = [ '--target', 'ES5', '--module', 'amd', '--removeComments', '--sourcemap' ]

	try:
		conf.find_program('tsc')
		v.append_value('supported_features', 'tsc')
	except Exception, e:
		v.append_value('missing_features', 'tsc')
		if Logs.verbose > 0:
			Logs.warn('TypeScript compiler is not available: %s' % (e))

@feature('tsc')
@after_method('process_source')
def process_tsc(self):
	outputs = []
	for t in getattr(self, 'compiled_tasks', []):
		outputs.extend(t.outputs)

	# remember that the install paths are given by the task generators
	try:
		inst_to = self.install_path
	except AttributeError:
		inst_to = tsc.inst_to
	if inst_to:
		for o in outputs:
			# Rename any 'ts' folders to 'js'
			dst = o.path_from(self.path.get_bld()).split(os.path.sep)
			dst = [ x == 'ts' and 'js' or x for x in dst ]
			dst = os.path.sep.join(dst)
			i = self.bld.install_as('%s/%s' % (inst_to, dst), o, env=self.env, chmod=Utils.O644);
			self.install_extras.append(i)

def parse_tsc(self):
	env = self.env

	outputs = []
	lst_src = []
	missing = []

	seen = []
	to_see = [self.inputs[0]]
	basedir = self.inputs[0].get_src().parent;

	# Find dependencies and generate the list of output files

	while to_see:
		node = to_see.pop(0)
		if node in seen:
			continue
		seen.append(node)
		lst_src.append(node)
		cwd = node.parent

		# If this is a child, add it to the outputs
		if cwd.is_child_of(basedir):
			outputs.append(node.change_ext('.js'))
			outputs.append(node.change_ext('.js.map'))

		# read the file
		code = node.read()

		# find all references
		names = refs.findall(code)
		for n in names:
			u = node.parent.find_resource(n)
			if u:
				to_see.append(u)
			else:
				missing.append(u)
				Logs.warn('could not find %r' % n)

	self.outputs = outputs

	# Cache for the scanner function
	# Dep nodes, Unresolved names
	self.tsc_deps = (lst_src, missing)

def tsc_scan(self):
	return self.tsc_deps

class tsc(Task.Task):
	"""
	Run tsc
	"""
	run_str = '${TSC} ${TSC_FLAGS} -outDir ${TGT[0].bld_dir()} ${SRC}'
	inst_to = '${BINDIR}'
	chmod   = Utils.O644
	scan    = tsc_scan

@extension('.ts')
def tsc_hook(self, node):
	task = self.create_task('tsc', node)

	parse_tsc(task)

	try:
		self.compiled_tasks.append(task)
	except AttributeError:
		self.compiled_tasks = [task]
	return task
