from waflib import Utils, Errors
from waflib.TaskGen import feature
import os.path

'''
For Windows 7 x64:
 * Strawberry Perl 5.18.2.2 64bit (strawberry-perl-5.18.2.2-64bit.msi)
 * Ruby 1.9.3-p545 (rubyinstaller-1.9.3-p545.exe)
 * Java SE Development Kit 8u11 x64 (jdk-8u11-windows-x64.exe)
 * Doxygen (doxygen-1.8.8-setup.exe)

For Ubuntu 14.04 x64:
 * ruby1.9.1 perl xsltproc libxml2-utils ghostscript
 * Oracle JDK 1.8
 * http://www.webupd8.org/2012/09/install-oracle-java-8-in-ubuntu-via-ppa.html
'''

host_plat = [ 'win32', 'linux', 'darwin' ]

archs = [ ]

tools = [
	'misc',
	'tools.asciidoc',
	'tools.utils',
	'tools.doxygen',
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
