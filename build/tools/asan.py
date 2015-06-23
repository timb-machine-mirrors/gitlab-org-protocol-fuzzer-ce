from waflib.TaskGen import feature, after_method
from waflib import Logs

def configure(conf):
	v = conf.env

	try:
		conf.find_program(v['ASAN_CC'])
		v.append_value('supported_features', 'asan')
	except Exception, e:
		v.append_value('missing_features', 'asan')
		if Logs.verbose > 0:
			Logs.warn('%s is not available: %s' % (v['ASAN_CC'], e))

@feature('asan')
@after_method('apply_link')
def process_asan(self):
	self.env['CC'] = self.env['ASAN_CC']
	self.env['CXX'] = self.env['ASAN_CXX']
	self.env['LINK_CC'] = self.env['ASAN_CC']
	self.env['LINK_CXX'] = self.env['ASAN_CXX']
