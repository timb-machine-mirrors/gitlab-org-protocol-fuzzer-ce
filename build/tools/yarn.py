import os
import json
import shutil
from waflib.TaskGen import feature
from waflib import Task, Utils

def configure(conf):
	conf.find_program('yarn')
	conf.env.append_value('supported_features', 'yarn')

@feature('yarn')
def process_yarn(self):
	yarn_source = getattr(self, 'yarn_source', [])
	yarn_target = getattr(self, 'yarn_target', [])

	__waf__ = self.path.get_bld().find_or_declare('__waf__')
	node_modules = __waf__.make_node('node_modules')
	package_node = self.path.make_node('package.json')

	src1 = self.to_nodes(yarn_source)
	tgt1 = map(lambda x: __waf__.find_or_declare(x.path_from(self.path)), yarn_source)
	copy_task = self.create_task('materialize', src1, tgt1)

	package_json = json.loads(package_node.read())
	deps = sorted(package_json['dependencies'].keys())

	src2 = package_node
	tgt2 = map(lambda x: node_modules.make_node(str(x)), deps)
	install_task = self.create_task('yarn_install', src2, tgt2)
	install_task.cwd = __waf__.abspath()

	src3 = self.to_nodes(yarn_source) + tgt2
	tgt3 = map(lambda x: __waf__.make_node(x), yarn_target)
	build_task = self.create_task('yarn_build', src3, tgt3)
	build_task.cwd = __waf__.abspath()

	self.install_files(self.install_path, tgt3, cwd=__waf__, relative_trick=True)

	install_task.run_after.add(copy_task)
	build_task.run_after.add(install_task)

class materialize(Task.Task):
	def run(self):
		for i, x in enumerate(self.outputs):
			shutil.copy(self.inputs[i].abspath(), x.abspath())

class yarn_install(Task.Task):
	run_str = '${YARN} install'

class yarn_build(Task.Task):
	run_str = '${YARN} run build'
