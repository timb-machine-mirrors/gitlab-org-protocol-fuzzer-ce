import json
from waflib.TaskGen import feature
from waflib import Task

def configure(conf):
	conf.find_program('npm')

@feature('npm')
def process_npm(self):
	src = self.path.find_resource('package.json')
	package_json = json.loads(src.read())
	deps = package_json['dependencies'].keys() + package_json['devDependencies'].keys()
	node_modules = self.path.find_or_declare('node_modules')
	tgt = map(lambda x: node_modules.get_src().make_node(str(x)), deps)

	install = self.create_task('npm_install', src, tgt)
	install.cwd = self.path.abspath()

	out = getattr(self, 'npm_outdir', None)
	src = self.to_nodes(getattr(self, 'npm_source', []))
	tgt = self.to_nodes(getattr(self, 'npm_target', []))

	build = self.create_task('npm_build', src, tgt)
	build.cwd = self.path.abspath()
	build.env.NPM_OUT = out.abspath()

	build.run_after.add(install)

	self.install_files(self.install_path, tgt)

class npm_install(Task.Task):
	run_str = '${NPM} install'

class npm_build(Task.Task):
	run_str = '${NPM} run build -- --output-path ${NPM_OUT}'
