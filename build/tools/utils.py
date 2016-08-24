import os.path, re
from waflib.TaskGen import feature, before_method, after_method, taskgen_method
from waflib.Configure import conf
from waflib import Utils, Logs, Task, Context, Errors
from waflib.Tools import ccroot

ccroot.lib_patterns['resource'] = ['%s']

@taskgen_method
def install_files(self, dest, files, **kw):
	inst_task = self.bld.install_files(dest, files, **kw)
	save_inst_task(self, inst_task)

@taskgen_method
def install_as(self, dest, srcfile, **kw):
	inst_task = self.bld.install_as(dest, srcfile, **kw)
	save_inst_task(self, inst_task)

def save_inst_task(self, inst_task):
	extras = getattr(self, 'install_extras', [])
	extras.append(inst_task)
	self.install_extras = extras

@feature('*')
@before_method('process_source')
def default_variant(self):
	if not self.env.VARIANT:
		return

	features = set(Utils.to_list(self.features))
	available = set(Utils.to_list(self.env.VARIANTS))
	intersect = features & available

	if not intersect:
		features.add(self.env.VARIANT)
		self.features = list(features)

@feature('*')
@after_method('process_source')
def apply_install(self):
	try:
		inst_to = self.install_path
	except AttributeError:
		inst_to = hasattr(self, 'link_task') and getattr(self.link_task.__class__, 'inst_to', None)

	# For the attributes install_644 and install_755
	# The value can be a string: 'file1 file2 file3'
	# An array (string || nodes): ['file1', self.find_resource('file2')]
	# A dict of { cwd : string|array }
	# Will install files to ${BINDIR} relative to cwd or self.path

	do_install(self, inst_to, 'install_644', Utils.O644)
	do_install(self, inst_to, 'install_755', Utils.O755)

def do_install(self, inst_to, attr, chmod, **kw):
	val = getattr(self, attr, [])

	if isinstance(val, dict):
		for cwd, items in val.iteritems():
			if isinstance(cwd, str):
				cwd = self.path.find_node(cwd)
			do_install2(self, inst_to, cwd, items, chmod)
	else:
		do_install2(self, inst_to, self.path, val, chmod)

def do_install2(self, inst_to, cwd, items, chmod):
	extras = self.to_nodes(items, path=cwd)
	if extras:
		if not inst_to:
			Logs.warn('\'%s\' has no install path but is supposed to install: %s' % (self.name, extras))
		else:
			self.install_files(inst_to, extras, env=self.env, cwd=cwd, relative_trick=True, chmod=chmod)

@feature('win', 'win_x86', 'win_x64', 'linux', 'linux_x86', 'linux_x86_64', 'osx', 'debug', 'release', 'com', 'pin', 'network', 'peach', 'unix')
def dummy_platform(self):
	# prevent warnings about features with unbound methods
	pass

@feature('fake_lib')
@after_method('process_lib')
def install_fake_lib(self):
	name = self.link_task.__class__.__name__
	if name is not 'fake_csshlib':
		install_outputs(self)

@feature('cs')
@after_method('apply_cs')
def install_content(self):
	names = self.to_list(getattr(self, 'content', []))
   	get = self.bld.get_tgen_by_name
	for x in names:
		try:
			y = get(x)
			install_content2(y)
		except Errors.WafError:
			self.bld.fatal('cs task has no taskgen for content %r' % self)

@feature('cs')
@after_method('apply_cs')
def install_aspnet(self):
	# special installation structure for asp.net projects
	if not getattr(self, 'ide_aspnet', False):
		return

	if getattr(self.bld, 'is_idegen', False):
		return

	if not getattr(self, 'install_task', False):
		return

	inst_to = getattr(self, 'install_path', '${BINDIR}')
	inst_to_bin = inst_to + '/bin'

	self.install_task.dest = inst_to_bin

	names = self.to_list(getattr(self, 'use', []))
	for x in names:
		y = self.bld.get_tgen_by_name(x)
		y.post()
		task = getattr(y, 'install_task', getattr(y, 'link_task', None))
		self.install_files(inst_to_bin, task.outputs, chmod=Utils.O755)

	content = getattr(self, 'ide_content', [])
	if content:
		self.install_files(inst_to, content, cwd=self.path, relative_trick=True, chmod=Utils.O644)

def install_content2(self):
	if getattr(self, 'has_installed', False):
		return

	self.has_installed = True

	content = getattr(self, 'content', [])
	if content:
		self.install_files('${BINDIR}', content, cwd=self.path, relative_trick=True)

def install_outputs(self):
	if getattr(self, 'has_installed', False):
		return

	self.has_installed = True

	# install 3rdParty libs into ${LIBDIR}
	self.install_files('${LIBDIR}', self.link_task.outputs, chmod=Utils.O755)

	# install any pdb or .config files into ${LIBDIR}
	for lib in self.link_task.outputs:
		# only look for .config if we are mono - as they are the only ones that support this
		config = self.env.CS_NAME == 'mono' and lib.parent.find_resource(lib.name + '.config')
		if config:
			self.install_files('${LIBDIR}', config, chmod=Utils.O644)

		name = lib.name
		ext='.pdb'
		k = name.rfind('.')
		if k >= 0:
			name = name[:k] + ext
		else:
			name = name + ext

		pdb = lib.parent.find_resource(name)
		if pdb:
			self.install_files('${LIBDIR}', pdb, chmod=Utils.O755)

@feature('cs', 'msbuild')
@before_method('apply_cs', 'apply_mbuild')
def cs_helpers(self):
	# set self.gen based off self.name since they are usually the same
	if not getattr(self, 'gen', None):
		setattr(self, 'gen', self.name)

	# ensure all binaries get chmod 755
	setattr(self, 'chmod', Utils.O755)

	# add optional csflags
	csflags = getattr(self, 'csflags', [])
	if csflags:
		self.env.append_value('CSFLAGS', csflags)

	# ensure the appropriate platform is being set on the command line
	if not getattr(self, 'platform', None):
		setattr(self, 'platform', self.env.CSPLATFORM)

	# ensure install_path is set
	if not hasattr(self, 'install_path'):
		setattr(self, 'install_path', '${BINDIR}')

@feature('cs')
@after_method('apply_cs')
def cs_resource(self):
	base = getattr(self, 'namespace', os.path.splitext(self.gen)[0])

	if getattr(self, 'unsafe', False):
		self.env.append_value('CSFLAGS', ['/unsafe+'])

	keyfile = self.to_nodes(getattr(self, 'keyfile', []))
	self.cs_task.dep_nodes.extend(keyfile)
	if keyfile:
		self.env.append_value('CSFLAGS', '/keyfile:%s' % (keyfile[0].abspath()))

	# add external resources to the dependency list and compilation command line
	resources = self.to_nodes(getattr(self, 'resource', []))
	self.cs_task.dep_nodes.extend(resources)
	for x in resources:
		rel_path = x.path_from(self.path)
		name = rel_path.replace('\\', '.').replace('/', '.')
		final = base + '.' + name
		self.env.append_value('CSFLAGS', '/resource:%s,%s' % (x.abspath(), final))

	embeds = self.to_list(getattr(self, 'embed', []))
	get = self.bld.get_tgen_by_name
	for x in embeds:
		y = get(x)
		y.post()
		tsk = getattr(y, 'link_task', None)
		self.cs_task.dep_nodes.extend(tsk.outputs) # dependency
		final = '%s.Resources.%s' % (base, x)
		self.env.append_value('CSFLAGS', '/resource:%s,%s' % (tsk.outputs[0].abspath(), final))

	# win32 icon support
	icon = getattr(self, 'icon', None)
	if icon:
		node = self.path.find_or_declare(icon)
		self.cs_task.dep_nodes.append(node)
		self.env.append_value('CSFLAGS', ['/win32icon:%s' % node.path_from(self.bld.bldnode)])

	if 'exe' in self.cs_task.env.CSTYPE:
		# if this is an exe, require app.config and install to ${BINDIR}
		cfg = self.path.find_or_declare('app.config')
		if self.env.CS_NAME != 'mono':
			manifest = self.path.find_resource('app.manifest')
			if manifest:
				setattr(self, 'app_manifest', manifest)
				self.cs_task.dep_nodes.append(manifest)
				self.env.append_value('CSFLAGS', ['/win32manifest:%s' % manifest.path_from(self.bld.bldnode)])
	elif self.env.CS_NAME == 'mono':
		# if this is an assembly, app.config is optional and
		# only supported by mono
		cfg = self.path.find_resource('app.config')
	else:
		cfg = None

	if cfg:
		setattr(self, 'app_config', cfg)
		inst_to = getattr(self, 'install_path', '${BINDIR}')

		# use the taskgen method to collect installed dependencies for zips & msis
		self.install_as('%s/%s.config' % (inst_to, self.gen), cfg, env=self.env, chmod=Utils.O644)

target_framework_template = '''using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=${TARGET_FRAMEWORK}", FrameworkDisplayName = "${TARGET_FRAMEWORK_NAME}")]
'''

@feature('cs')
@before_method('apply_cs')
def apply_target_framework(self):
	if getattr(self.bld, 'is_idegen', False):
		return

	# Add TargetFrameworkAttribute to the assembly
	self.env.EMIT_SOURCE = Utils.subst_vars(target_framework_template, self.env)
	name = '.NETFramework,Version=%s.AssemblyAttributes.cs' % self.env.TARGET_FRAMEWORK
	target = self.path.find_or_declare(name)
	tsk = self.create_task('emit', None, [ target ])
	self.source = self.to_nodes(self.source) + tsk.outputs

	# For any use entries that can't be resolved to a task generator
	# assume they are system reference assemblies and add them to the
	# ASSEMBLIES variable so they get full path linkage automatically added
	filtered = []
	names = self.to_list(getattr(self, 'use', []))
	get = self.bld.get_tgen_by_name
	for x in names:
		try:
			y = get(x)
			if 'fake_lib' in getattr(y, 'features', ''):
				y.post()
				install_outputs(y)
			filtered.append(x)
		except Errors.WafError:
			self.env.append_value('ASSEMBLIES', x)
	self.use = filtered

@conf
def clone_env(self, variant):
	env = self.all_envs.get(variant, None)
	if env is None:
		return None
	copy = env.derive()
	copy.PREFIX = self.env.PREFIX
	copy.BINDIR = self.env.BINDIR
	copy.LIBDIR = self.env.LIBDIR
	copy.DOCDIR = self.env.DOCDIR
	return copy

@conf
def read_all_csshlibs(self, subdir):
	libs = self.path.find_dir(subdir).ant_glob('*.dll')
	for x in libs:
		self.read_csshlib(x.name, paths=[x.parent.path_from(self.path)])

@conf
def get_version(self, tool):
	env = self.env
	environ = dict(self.environ)
	environ.update(PATH = ';'.join(env['PATH']))
	cmd = self.cmd_to_list(env[tool])
	(out,err) = self.cmd_and_log(cmd + ['/help'], env=environ, output=Context.BOTH)
	exe = os.path.split(cmd[0])[1].lower()
	ver_re = re.compile('.*ersion (\d+\.\d+\.\d+(\.\d+)?)')
	m = ver_re.match(out)
	if not m:
		m = ver_re.match(err)
	if not m:
		return None
	return m.group(1)
	
@conf
def ensure_version(self, tool, ver_exp):
	ver = self.get_version(tool)
	ver_exp = Utils.to_list(ver_exp)
	if not ver:
		raise Errors.WafError("Could not verify version of %s" % (tool))
	found = False
	for v in ver_exp:
		found = ver.startswith(v) or found
	if not found:
		raise Errors.WafError("Requires %s %s but found version %s" % (tool, ver_exp, ver))

@feature('emit')
@before_method('process_rule')
def apply_emit(self):
	self.env.EMIT_SOURCE = self.source
	self.source = []
	self.meths.remove('process_source')
	outputs = [ self.path.find_or_declare(self.target) ]
	self.create_task('emit', None, outputs)

class emit(Task.Task):
	color = 'PINK'

	vars = [ 'EMIT_SOURCE' ]

	def run(self):
		text = str(self.env['EMIT_SOURCE'])
		self.outputs[0].write(text)

class fake_resource(Task.Task):
	"""
	Task used for reading a foreign resource and adding the dependency on it
	"""
	color   = 'YELLOW'
	inst_to = None

	def runnable_status(self):
		for x in self.outputs:
			x.sig = Utils.h_file(x.abspath())
		return Task.SKIP_ME

@conf
def read_resource(self, name, paths=[]):
	"""
	Read an external resource and register it for the *use* system::

		def build(bld):
			bld.read_external_resource('some_resource.ext', paths=[bld.env.mypath])
			bld(features='cs', source='Hi.cs', bintype='exe', gen='hi.exe', use='some_resource.ext')

	:param name: Name of the resource
	:type name: string
	:param paths: Folders in which the resource may be found
	:type paths: list of string
	:return: A task generator having the feature *fake_lib* which will call :py:func:`waflib.Tools.ccroot.process_lib`
	:rtype: :py:class:`waflib.TaskGen.task_gen`
	"""
	return self(name=name, features='fake_lib', lib_paths=paths, lib_type='resource')
