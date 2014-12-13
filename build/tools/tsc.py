from waflib.TaskGen import feature, before_method, after_method, extension
from waflib import Task, Utils, Logs, Configure, Context, Options, Errors
import re, os.path
import tools.utils

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
	for t in getattr(self, 'tsc', []):
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
			i = self.bld.install_as('%s/%s' % (inst_to, dst), o, env=self.env, chmod=Utils.O644)
			tools.utils.save_inst_task(self, i)
			tsc_out = getattr(self, 'tsc_out', [])
			tsc_out.append(dst)
			self.tsc_out = tsc_out

def parse_tsc(self):
	env = self.env

	outputs = []
	lst_src = []
	missing = []

	seen = []
	to_see = [self.inputs[0]]
	basedir = self.inputs[0].get_src().parent;

	# Find dependencies
	while to_see:
		node = to_see.pop(0)
		if node in seen:
			continue
		seen.append(node)
		lst_src.append(node)

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

	# Cache for the scanner function
	# Dep nodes, Unresolved names
	self.tsc_deps = (lst_src, missing)

def tsc_scan(self):
	return self.tsc_deps

class tsc(Task.Task):
	"""
	Run tsc
	"""
	run_str = '${TSC} ${TSC_FLAGS} -out ${TGT[0].abspath()} ${SRC}'
	inst_to = '${BINDIR}'
	chmod   = Utils.O644
	scan    = tsc_scan

@extension('.ts')
def tsc_hook(self, node):
	task = self.create_task('tsc', node)
	task.outputs = [
		node.change_ext('.js'),
		node.change_ext('.js.map'),
	]
	parse_tsc(task)

	tasks = getattr(self, 'tsc', [])
	tasks.append(task)
	self.tsc = tasks

	return task
