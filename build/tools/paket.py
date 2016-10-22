import os
from waflib.Configure import conf
from waflib.TaskGen import feature, before_method, after_method, taskgen_method
from waflib import Utils, Logs, Task, Context, Errors

def configure(conf):
	dotnet = []
	if Utils.unversioned_sys_platform() != 'win32':
		conf.find_program('mono')
		dotnet = ['MONO']

	bootstrapper = os.path.abspath(os.path.join(
		'paket',
		'.paket', 
		'paket.bootstrapper.exe'
	))

	paket = os.path.abspath(os.path.join(
		'paket',
		'.paket', 
		'paket.exe'
	))

	conf.cmd_and_log(dotnet + [bootstrapper], cwd='paket')
	conf.cmd_and_log(dotnet + [paket, 'restore'], cwd='paket')

	conf.env.append_value('supported_features', 'paket')

class Package(object):
	def __init__(self, bld, name):
		self.name = name
		self.deps = []
		self.byfx = {}

		byfx_libs = self.collect_byfx(bld, 'packages/%s/lib/*/*.dll' % name)
		byfx_refs = self.collect_byfx(bld, 'packages/%s/ref/*/*.dll' % name)

		self.create_tgs(bld, byfx_libs, False)
		self.create_tgs(bld, byfx_refs, True)

	def collect_byfx(self, bld, pattern):
		nodes = bld.path.ant_glob(pattern, ignorecase=True)
		byfx = {}
		for node in nodes:
			fx = node.parent.name
			byfx.setdefault(fx, []).append(node)
		return byfx

	def create_tgs(self, bld, byfx, is_ref):
		for fx, nodes in byfx.iteritems():
			if fx in self.byfx:
				continue

			tgs = []
			for node in nodes:
				tg = bld(
					name='%s:%s:%s' % (self.name, fx, node.name),
					features='nuget_lib',
					node=node,
					is_ref=is_ref
				)
				tgs.append(tg)
			self.byfx[fx] = tgs

	def __repr__(self):
		return '%s: %s' % (self.name, self.byfx)

@conf
def read_paket(self, lockfile):
	if not self.env.MCS:
		return

	pkgs = {}
	parent = None

	src = self.path.find_resource(lockfile)
	if src:
		contents = src.read()
		for line in contents.splitlines():
			suffix = line.lstrip(' ')
			depth = (len(line) - len(suffix)) / 2
			if depth == 2:
				parent = Package(self, suffix.split()[0])
				pkgs[parent.name] = parent
			elif depth == 3:
				pkg = suffix.split()[0]
				parent.deps.append(pkg)

	self.env.PAKET_PACKAGES = pkgs

@feature('cs', 'paket')
@before_method('install_packages')
def use_nuget(self):
	pkgs = getattr(self, 'use_packages', None)
	if not pkgs:
		return

	default_settings = dict(
		excludes = [],
		frameworks = []
	)
	settings = getattr(self, 'paket_settings', default_settings)

	use = self.to_list(getattr(self, 'use', []))

	tgs = set()

	for pkg in pkgs:
		use_packages_recurse(self, pkg, settings, tgs)

	for tg in sorted(tgs):
		# print 'use: %s' % tg.name
		use.append(tg.name)

def use_packages_recurse(self, pkg, settings, into):
	if pkg in settings['excludes']:
		return

	dep = self.env.PAKET_PACKAGES.get(pkg)
	if not dep:
		self.bld.fatal('%r depends on unknown package %r' % (self.name, pkg))

	tgs = get_pkg_for_fx(dep, settings['frameworks'])
	if tgs is None:
		self.bld.fatal('%r depends on unknown framework for package %r, available frameworks: %r' % (
			self.name, pkg, dep.byfx.keys()
		))

	for tg in tgs:
		if not tg.is_ref:
			into.add(tg)

	for child in dep.deps:
		use_packages_recurse(self, child, settings, into)

def get_pkg_for_fx(dep, fxs):
	for fx in fxs:
		tgs = dep.byfx.get(fx)
		if tgs:
			return tgs
	return None

@feature('nuget_lib')
def process_nuget_lib(self):
	# self.node.sig = Utils.h_file(self.node.abspath())

	self.link_task = self.create_task('fake_csshlib', [], [self.node])
	self.target = self.node.name
