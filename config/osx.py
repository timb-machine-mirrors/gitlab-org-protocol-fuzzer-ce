from waflib import Utils

archs = [ ]
tools = [
#	'gcc',
#	'gxx',
	'mcs',
	'resx',
]

def prepare(conf):
	# set up paths in conf.env that will be used by the tools
	pass

def configure(conf):
	# setup CFLAGS, LINKFLAGS, etc
	return [ 'debug', 'release' ]

def debug(env):
	pass

def release(env):
	pass
