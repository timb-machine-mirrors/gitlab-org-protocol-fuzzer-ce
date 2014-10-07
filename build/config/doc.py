from waflib import Utils, Errors
from waflib.TaskGen import feature
import os.path

'''
For Windows 7 x64:
 * Strawberry Perl 5.18.2.2 64bit (strawberry-perl-5.18.2.2-64bit.msi)
 * Ruby 1.9.3-p545 (rubyinstaller-1.9.3-p545.exe)
 * Java SE Development Kit 8u11 x64 (jdk-8u11-windows-x64.exe)

For Ubuntu 14.04 x64:
 * default-jdk ruby-1.9.1 perl xsltproc xmllint
'''

host_plat = [ 'win32', 'linux', 'darwin' ]

archs = [ ]

tools = [
	'misc',
	'tools.asciidoc',
	'tools.utils',
]

def prepare(conf):
	pass

def configure(conf):
	env = conf.env

	env.append_value('supported_features', [
		'asciidoc',
		'webhelp',
		'emit',
		'subst',
	])
