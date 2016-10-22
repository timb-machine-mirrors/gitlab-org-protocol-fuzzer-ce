import os.path
from waflib import Utils
from waflib.TaskGen import feature

host_plat = [ 'linux' ]

archs = [ 'x86', 'x86_64' ]

'''
Compile:
g++
-DBIGARRAY_MULTIPLIER=1
-Wall
-Werror
-Wno-unknown-pragmas
-D__PIN__=1
-DPIN_CRT=1
-fno-stack-protector
-fno-exceptions
-funwind-tables
-fasynchronous-unwind-tables
-fno-rtti
-DTARGET_{IA32,IA32E}
-DHOST_{IA32,IA32E}
-fPIC
-DTARGET_LINUX
-fabi-version=2
-I../../../source/include/pin
-I../../../source/include/pin/gen
-isystem /home/peach/pin/pin-3.0-76991-gcc-linux/extras/stlport/include
-isystem /home/peach/pin/pin-3.0-76991-gcc-linux/extras/libstdc++/include
-isystem /home/peach/pin/pin-3.0-76991-gcc-linux/extras/crt/include
-isystem /home/peach/pin/pin-3.0-76991-gcc-linux/extras/crt/include/arch-{x86,x86_64}
-isystem /home/peach/pin/pin-3.0-76991-gcc-linux/extras/crt/include/kernel/uapi
-isystem /home/peach/pin/pin-3.0-76991-gcc-linux/extras/crt/include/kernel/uapi/asm-x86
-I../../../extras/components/include
-I../../../extras/xed-{ia32,intel64}/include
-I../../../source/tools/InstLib
-O3
-fomit-frame-pointer
-fno-strict-aliasing
-m{32,64}
-c
-o obj-intel64/MyPinTool.o MyPinTool.cpp

Link:
g++
-shared
-Wl,--hash-style=sysv
../../../{ia32,intel64}/runtime/pincrt/crtbeginS.o
-Wl,-Bsymbolic
-Wl,--version-script=../../../source/include/pin/pintool.ver
-fabi-version=2
-m{32,64}
-o obj-ia32/MyPinTool.so    obj-ia32/MyPinTool.o
-L../../../{ia32,intel64}/runtime/pincrt
-L../../../{ia32,intel64}/lib
-L../../../{ia32,intel64}/lib-ext
-L../../../extras/xed-{ia32,intel64}/lib
-lpin
-lxed
../../../{ia32,intel64}/runtime/pincrt/crtendS.o
-lpin3dwarf
-ldl-dynamic
-nostdlib
-lstlport-dynamic
-lm-dynamic
-lc-dynamic
'''

tools = [
	'gcc',
	'gxx',
	'cs',
	'resx',
	'misc',
	'tools.utils',
	'tools.externals',
	'tools.tsc',
	'tools.version',
	'tools.xcompile',
	'tools.yarn',
	'tools.paket',
]

optional_tools = [
	'tools.asan',
	'tools.mdoc',
	'tools.mkbundle',
	'tools.test',
	'tools.zip',
]


def prepare(conf):
	env = conf.env
	j = os.path.join

	env['MCS']  = 'mcs'
	env['CC']   = 'gcc'
	env['CXX']  = 'g++'

	if env.SUBARCH == 'x86':
		env['ARCH'] = [ '32' ]
		pin_tgt = 'ia32'
		pin_def = 'IA32'
	else:
		env['ARCH'] = [ '64' ]
		pin_tgt = 'intel64'
		pin_def = 'IA32E'

	env['ARCH_ST'] = '-m%s'

	env['PIN_VER'] = 'pin-2.14-71313-gcc.4.4.7-linux'

	pin = j(conf.get_third_party(), 'pin', env['PIN_VER'])

	env['EXTERNALS'] = {
		'pin' : {
			'INCLUDES'  : [
				j(pin, 'source', 'include', 'pin'),
				j(pin, 'source', 'include', 'pin', 'gen'),
				j(pin, 'extras', 'components', 'include'),
				j(pin, 'extras', 'xed-%s' % pin_tgt, 'include'),
			],
			'HEADERS'   : [],
			'STLIBPATH' : [
				j(pin, pin_tgt, 'lib'),
				j(pin, pin_tgt, 'lib-ext'),
				j(pin, 'extras', 'xed-%s' % pin_tgt, 'lib'),
			],
			'LIBPATH'   : [],
			'LIB'       : [],
			'STLIB'     : [ 'pin', 'xed', 'pindwarf' ],
			'DEFINES'   : [ 'BIGARRAY_MULTIPLIER=1', 'TARGET_LINUX', 'TARGET_%s' % pin_def, 'HOST_%s' % pin_def, 'USING_XED', ],
			'CFLAGS'    : [],
			'CXXFLAGS'  : [ '-fno-stack-protector', '-fomit-frame-pointer', '-fno-strict-aliasing' ],
			'LINKFLAGS' : [],
			'ENV'       : { 'STLIB_MARKER' : '-Wl,-Bsymbolic', 'SHLIB_MARKER' : '-Wl,-Bsymbolic', 'cxxshlib_PATTERN' : '%s.so' },
		},

	}

	env['TARGET_FRAMEWORK'] = 'v4.5'
	env['TARGET_FRAMEWORK_NAME'] = '.NET Framework 4.5'

	env['ASAN_CC'] = 'clang'
	env['ASAN_CXX'] = 'clang++'

	env['RUN_NETFX'] = 'mono'
	env['PEACH_PLATFORM_DLL'] = 'Peach.Pro.OS.Linux.dll'

	env.append_value('supported_features', [
		'peach',
		'linux',
		'c',
		'cstlib',
		'cshlib',
		'cprogram',
		'cxx',
		'cxxstlib',
		'cxxshlib',
		'cxxprogram',
		'fake_lib',
		'nuget_lib',
		'cs',
		'debug',
		'release',
		'emit',
		'vnum',
		'subst',
		'network',
		'unix',
		'flexnetls',
	])

def configure(conf):
	env = conf.env

	env.append_value('CSFLAGS', [
		'/sdk:4.5',
		'/warn:4',
		'/define:PEACH,UNIX,MONO',
		'/warnaserror',
		'/nowarn:1591', # Missing XML comment for publicly visible type
	])

	env.append_value('CSFLAGS_debug', [
		'/define:DEBUG;TRACE;MONO',
	])

	env.append_value('CSFLAGS_release', [
		'/define:TRACE;MONO',
		'/optimize+',
	])

	env['CSPLATFORM'] = 'AnyCPU'
	env['CSDOC'] = True

	env.append_value('DEFINES_debug', [
		'DEBUG',
	])

	cppflags = [
		'-pipe',
		'-Werror',
		'-Wno-unused',
	]

	cppflags_debug = [
		'-ggdb',
	]

	cppflags_release = [
		'-O3',
	]

	asan = [
		'-fsanitize=address'
	]

	env.append_value('CFLAGS_asan', asan)
	env.append_value('CXXFLAGS_asan', asan)
	env.append_value('LINKFLAGS_asan', asan)

	env.append_value('CPPFLAGS', cppflags)
	env.append_value('CPPFLAGS_debug', cppflags_debug)
	env.append_value('CPPFLAGS_release', cppflags_release)
	
	env.append_value('LIB', [ 'dl' ])
	env.append_value('LIB_network', [ 'pthread' ])

	env['VARIANTS'] = [ 'debug', 'release' ]

def debug(env):
	env.CSDEBUG = 'full'

def release(env):
	env.CSDEBUG = 'pdbonly'
