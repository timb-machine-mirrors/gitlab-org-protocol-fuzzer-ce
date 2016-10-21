from waflib.Configure import conf
from waflib.TaskGen import feature, before_method, after_method, taskgen_method
from waflib import Utils, Logs, Task, Context, Errors

class Package(object):
	def __init__(self, path, name):
		self.name = name
		self.deps = []
		self.byfx = {}

		pattern = 'packages/%s/lib/*/*.dll' % name
		nodes = path.ant_glob(pattern, ignorecase=True)
		for node in nodes:
			self.byfx.setdefault(node.parent.name, []).append(node)

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
				parent = Package(self.path, suffix.split()[0])
				pkgs[parent.name] = parent
			elif depth == 3:
				pkg = suffix.split()[0]
				parent.deps.append(pkg)

	self.env.PAKET_PACKAGES = pkgs

	# import pprint
	# pprint.pprint(pkgs)

@feature('cs')
@after_method('apply_cs')
def apply_use_packages(self):
	pkgs = getattr(self, 'use_packages', None)
	if not pkgs:
		return

	nodes = set()
	refs = set()

	for pkg, item in pkgs.iteritems():
		use_packages_recurse(self, pkg, Utils.to_list(item[0]), item[1], nodes, refs)

	for ref in refs:
		self.env.append_value('CSFLAGS', '/reference:%s' % ref)

	self.cs_task.dep_nodes.extend(nodes)
	for node in nodes:
		self.env.append_value('CSFLAGS', '/reference:%s' % node.abspath())

	# inst_to = getattr(self, 'install_path', None) or '${LIBDIR}'
	# self.install_files(inst_to, nodes, chmod=Utils.O755)

def use_packages_recurse(self, pkg, fxs, excludes, into, refs):
	if pkg in excludes:
		return

	dep = self.env.PAKET_PACKAGES.get(pkg)
	if not dep:
		self.bld.fatal('%r depends on unknown package %r' % (self.name, pkg))

	if not dep.byfx:
		refs.add(dep.name)
	else:
		nodes = get_pkg_nodes(dep, fxs)
		if not nodes:
			self.bld.fatal('%r depends on unknown framework for package %r, available frameworks: %r' % (
				self.name, pkg, dep.byfx.keys()
			))

		into.update(nodes)

	for child in dep.deps:
		use_packages_recurse(self, child, fxs, excludes, into, refs)

def get_pkg_nodes(dep, fxs):
	for fx in fxs:
		nodes = dep.byfx.get(fx)
		if nodes:
			return nodes
	return None
