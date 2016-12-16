import os, os.path, platform
from waflib import Utils, Errors
from waflib.TaskGen import feature

host_plat = [ 'win32' ]

archs = [ 'x86', 'x64' ]

# x86 compilation line:
#cl /MT /EHs- /EHa- /wd4530 /DTARGET_WINDOWS /DBIGARRAY_MULTIPLIER=1 /D_CRT_SECURE_NO_DEPRECATE /D_SECURE_SCL=0 /nologo /Gy /Oi- /GR- /GS- /D__PIN__=1 /DPIN_CRT=1 /D_WINDOWS_H_PATH_="C:\Program Files (x86)\Windows Kits\8.1\Include\um" /D__i386__ /Zc:threadSafeInit- /DTARGET_IA32 /DHOST_IA32  /I../../../source/include/pin /I../../../source/include/pin/gen -I../../../extras/stlport/include -I../../../extras -I../../../extras/libstdc++/include -I../../../extras/crt/include -I../../../extras/crt -I../../../extras/crt/include/arch-x86 -I../../../extras/crt/include/kernel/uapi -I../../../extras/crt/include/kernel/uapi/asm-x86 /FIinclude/msvc_compat.h /I../../../extras/components/include /I../../../extras/xed-ia32/include /I../../../source/tools/InstLib /O2  /c /Foobj-ia32/MyPinTool.obj MyPinTool.cpp
#MyPinTool.cpp
#link /DLL /EXPORT:main /NODEFAULTLIB /NOLOGO /INCREMENTAL:NO /IGNORE:4210 /IGNORE:4049 /DYNAMICBASE /NXCOMPAT ../../../ia32/runtime/pincrt/crtbeginS.obj /MACHINE:x86 /ENTRY:Ptrace_DllMainCRTStartup@12 /BASE:0x55000000 /OPT:REF  /out:obj-ia32/MyPinTool.dll obj-ia32/MyPinTool.obj  /LIBPATH:../../../ia32/lib /LIBPATH:../../../ia32/lib-ext /LIBPATH:../../../ia32/runtime/pincrt /LIBPATH:../../../extras/xed-ia32/lib pin.lib xed.lib stlport-static.lib m-static.lib c-static.lib os-apis.lib pinvm.lib ntdll-32.lib kernel32.lib
#   Creating library obj-ia32/MyPinTool.lib and object obj-ia32/MyPinTool.exp
#pin.lib(pin_client.obj) : warning LNK4217: locally defined symbol ___sF imported in function "void __cdecl LEVEL_PINCLIENT::StartProgram(void)" (?StartProgram@LEVEL_PINCLIENT@@YAXXZ)

tools = [
	'msvc',
	'cs',
	'resx',
	'midl',
	'misc',
	'tools.utils',
	'tools.externals',
	'tools.tsc',
	'tools.version',
	'tools.paket',
]

optional_tools = [
	'tools.msbuild',
	'tools.msi',
	'tools.test',
	'tools.zip',
]

def prepare(conf):
	env = conf.env
	j = os.path.join

	env['MSVC_VERSIONS'] = ['msvc 14.0', 'msvc 12.0', 'msvc 11.0', 'msvc 10.0', 'wsdk 7.1' ]
	env['MSVC_TARGETS']  = 'x64' in env.SUBARCH and [ 'x64', 'x86_amd64' ] or [ 'x86' ]

	env['EXTERNALS_x86'] = {
		'com' : {
			'DEFINES' : [ '_WINDLL' ],
			'STLIB' : [ 'Ole32', 'OleAut32', 'Advapi32' ],
		},
		'network' : {
			'HEADERS' : [ 'winsock2.h' ],
			'STLIB'   : [ 'ws2_32' ],
		},
	}

	env['EXTERNALS_x64'] = {
		'com' : {
			'DEFINES' : [ '_WINDLL' ],
			'STLIB' : [ 'Ole32', 'OleAut32', 'Advapi32' ],
		},
		'network' : {
			'HEADERS' : [ 'winsock2.h' ],
			'STLIB'   : [ 'ws2_32' ],
		},
	}

	env['EXTERNALS'] = env['EXTERNALS_%s' % env.SUBARCH]

	# This is lame, the resgen that vcvars for x64 finds is the .net framework 3.5 version.
	# The .net 4 version is in the x86 search path.
	if env.SUBARCH == 'x64':
		env['MCS'] = getattr(conf.all_envs.get('win_x86'), 'MCS', [ None ])[0]
		env['RESGEN'] = getattr(conf.all_envs.get('win_x86'), 'RESGEN', [ None ])[0]

	pfiles = os.getenv('PROGRAMFILES(X86)', os.getenv('PROGRAMFILES'))
	env['TARGET_FRAMEWORK'] = 'v4.6.1'
	env['TARGET_FRAMEWORK_NAME'] = '.NET Framework 4.6.1'
	env['REFERENCE_ASSEMBLIES'] = j(pfiles, 'Reference Assemblies', 'Microsoft', 'Framework', '.NETFramework', env['TARGET_FRAMEWORK'])

	env['RUN_NETFX'] = ''
	env['PEACH_PLATFORM_DLL'] = 'Peach.Pro.OS.Windows.dll'

def configure(conf):
	env = conf.env

	# Ensure reference assembly folder exists
	if not os.path.isdir(env.REFERENCE_ASSEMBLIES):
		raise Errors.WafError("Could locate .NET Framework %s reference assemblies in: %s" % (env.TARGET_FRAMEWORK, env.REFERENCE_ASSEMBLIES))

	# Make sure all ASSEMBLY entries are fully pathed
	env.ASS_ST = '/reference:%s%s%%s' % (env.REFERENCE_ASSEMBLIES, os.sep)

	env.append_value('supported_features', [
		'peach',
		'win',
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
		'msbuild',
		'flexnetls',
	])

	cppflags = [
		'/Z7',
		'/W4',
		'/WX',
	]

	cppflags_debug = [
		'/MTd',
		'/Od',
	]

	cppflags_release = [
		'/MT',
		'/Ox',
	]

	env.append_value('CPPFLAGS', cppflags)
	env.append_value('CPPFLAGS_debug', cppflags_debug)
	env.append_value('CPPFLAGS_release', cppflags_release)

	env.append_value('CXXFLAGS_com', [ '/EHsc' ])

	env.append_value('DEFINES', [
		'WIN32',
		'_CRT_SECURE_NO_WARNINGS',
	])

	env.append_value('DEFINES_debug', [
		'DEBUG',
		'_DEBUG',
	])

	env.append_value('CSFLAGS', [
		'/noconfig',
		'/nologo',
		'/nostdlib+',
		'/warn:4',
		'/define:PEACH',
		'/errorreport:prompt',
		'/warnaserror',
		'/nowarn:1591', # Missing XML comment for publicly visible type
	])

	env.append_value('CSFLAGS_debug', [
		'/define:DEBUG;TRACE',
	])

	env.append_value('CSFLAGS_release', [
		'/define:TRACE',
		'/optimize+',
	])

	env.append_value('ASSEMBLIES', [
		'mscorlib.dll',
	])

	env.append_value('LINKFLAGS', [
		'/NOLOGO',
		'/DEBUG',
		'/INCREMENTAL:NO',
		'/WX',
		'/MACHINE:%s' % env.SUBARCH,
	])

	env['CSPLATFORM'] = env.SUBARCH
	env['CSDOC'] = True

	env.append_value('MIDLFLAGS', [
		'/%s' % ('x86' in env.SUBARCH and 'win32' or 'amd64'),
	])

	env['VARIANTS'] = [ 'debug', 'release' ]

def debug(env):
	env.CSDEBUG = 'full'

def release(env):
	env.CSDEBUG = 'pdbonly'
