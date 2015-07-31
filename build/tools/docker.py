from waflib.Build import InstallContext
from waflib.TaskGen import feature, after_method
from waflib import Utils, Task, Logs, Options, Errors

def configure(conf):
	try:
		conf.find_program('docker', var='DOCKER')
		conf.env.append_value('supported_features', [ 'docker' ])
	except Exception, e:
		conf.env.append_value('missing_features', [ 'docker' ])
		if Logs.verbose:
			Logs.warn('Docker feature is not available: %s' % (e))

class DockerContext(InstallContext):
	'''create docker images and push them to the registry'''

	cmd = 'docker'

	def __init__(self, **kw):
		super(DockerContext, self).__init__(**kw)
		self.is_docker = True

@feature('docker')
@after_method('apply_link')
def docker_feature(self):
	tg = self.bld(name = '')
	tg.create_task('docker')

class docker(Task.Task):
	vars = []

	after = ['vnum', 'inst']

	def runnable_status(self):
		ret = Task.SKIP_ME
		if getattr(self.generator.bld, 'is_docker', None):
			ret = super(docker, self).runnable_status()
			if ret == Task.SKIP_ME:
				ret = Task.RUN_ME
		return ret

	def _call(self, cmd):
		# print ' '.join(cmd)
		Utils.subprocess.check_call(cmd)

	def run(self):
		self._call([
			self.env.DOCKER,
			'build',
			'-t', 'peach-pro',
			self.env.PREFIX,
		])
