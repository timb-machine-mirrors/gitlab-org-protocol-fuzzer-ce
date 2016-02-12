import os
import json
import shutil
from waflib.TaskGen import feature
from waflib import Task, Utils

def configure(conf):
	conf.find_program('npm')
	conf.env.append_value('supported_features', 'npm')

@feature('npm')
def process_npm(self):
	npm_source = getattr(self, 'npm_source', [])
	npm_target = getattr(self, 'npm_target', [])

	__waf__ = self.path.get_bld().find_or_declare('__waf__')
	node_modules = __waf__.make_node('node_modules')
	package_node = self.path.make_node('package.json')

	src1 = self.to_nodes(npm_source)
	tgt1 = map(lambda x: __waf__.find_or_declare(x.path_from(self.path)), npm_source)
	copy_task = self.create_task('materialize', src1, tgt1)

	package_json = json.loads(package_node.read())
	deps = sorted(package_json['dependencies'].keys())

	src2 = package_node
	tgt2 = map(lambda x: node_modules.make_node(str(x)), deps)
	install_task = self.create_task('npm_install', src2, tgt2)
	install_task.cwd = __waf__.abspath()

	src3 = self.to_nodes(npm_source) + tgt2
	tgt3 = map(lambda x: __waf__.make_node(x), npm_target)
	build_task = self.create_task('npm_build', src3, tgt3)
	build_task.cwd = __waf__.abspath()

	self.install_files(self.install_path, tgt3)

	install_task.run_after.add(copy_task)
	build_task.run_after.add(install_task)

class materialize(Task.Task):
	def run(self):
		for i, x in enumerate(self.outputs):
			shutil.copy(self.inputs[i].abspath(), x.abspath())

class npm_install(Task.Task):
	run_str = '${NPM} install'

class npm_build(Task.Task):
	run_str = '${NPM} run build'
