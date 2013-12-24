import os
import os.path

from collections import OrderedDict

from waflib.extras import msvs
from waflib import Utils, TaskGen, Logs, Task, Context, Node, Options, Errors

msvs.msvs_generator.cmd = 'msvs2010'

CS_PROJECT_TEMPLATE = r'''<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">${project.properties.keys()[0]}</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">${project.properties.values()[0]['PlatformTarget']}</Platform>
    <ProductVersion>8.0.30703</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    ${for k, v in project.globals.iteritems()}
    <${k}>${v}</${k}>
    ${endfor}
  </PropertyGroup>

  ${for cfg, props in project.properties.iteritems()}
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == '${cfg}|${props['PlatformTarget']}' ">
    ${for k, v in props.iteritems()}
    <${k}>${str(v)}</${k}>
    ${endfor}
  </PropertyGroup>
  ${endfor}

  ${if project.references}
  <ItemGroup>
    ${for k,v in project.references.iteritems()}
    ${if v}
    <Reference Include="${k}">
      <HintPath>${v}</HintPath>
    </Reference>
    ${else}
    <Reference Include="${k}" />
    ${endif}
    ${endfor}
  </ItemGroup>
  ${endif}

  ${if project.project_refs}
  <ItemGroup>
    ${for k,v in project.project_refs.iteritems()}
    <ProjectReference Include="${k}">
      <Project>{${v.uuid}}</Project>
      <Name>${v.name}</Name>
    </ProjectReference>
    ${endfor}
  </ItemGroup>
  ${endif}

  ${if project.source_files}
  <ItemGroup>
    ${for src in project.source_files}
    <Compile Include="${src}" />
    ${endfor}
  </ItemGroup>
  ${endif}

  ${if project.linked_files}
  <ItemGroup>
    ${for name,src in project.linked_files}
    <Compile Include="${src}">
      <Link>${name}</Link>
    </Compile>
    ${endfor}
  </ItemGroup>
  ${endif}

  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />

</Project>
'''

class vsnode_cs_target(msvs.vsnode_project):
	VS_GUID_CSPROJ = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC"
	def ptype(self):
		return self.VS_GUID_CSPROJ

	def __init__(self, ctx, tg):
		base = getattr(ctx, 'projects_dir', None) or tg.path
		self.node = base.make_node(os.path.splitext(tg.name)[0] + '.csproj') # the project file as a Node
		msvs.vsnode_project.__init__(self, None, self.node)
		self.name = tg.name
		self.tg = tg # task generators
		self.is_active = True
		self.globals = OrderedDict()
		self.properties = OrderedDict()
		self.project_refs = {}

		self.collect_globals()
		
	def collect_globals(self):
		tg = self.tg
		g = self.globals

		asm_name = os.path.splitext(tg.cs_task.outputs[0].name)[0]

		# Order matters!
		g['ProjectGuid'] = '{%s}' % self.uuid
		g['OutputType'] = getattr(tg, 'bintype', tg.gen.endswith('.dll') and 'library' or 'exe')
		g['AppDesignerFolder'] = 'Properties'
		g['RootNamespace'] = getattr(tg, 'namespace', asm_name)
		g['AssemblyName'] = asm_name
		g['TargetFrameworkVersion'] = 'v4.0'
		g['TargetFrameworkProfile'] = os.linesep + '    '
		g['FileAlignment'] = '512'

	def combine_flags(self, flag):
		tg = self.tg
		final = {}
		for item in tg.env.CSFLAGS:
			if item.startswith(flag):
				opt = item[len(flag):]
				for x in opt.split(';'):
					final.setdefault(x)
		return ';'.join(final.keys())

	def collect_use(self):
		tg = self.tg

		self.references = {}
		self.other_tgen = []

		names = tg.to_list(getattr(tg, 'use', []))
		get = tg.bld.get_tgen_by_name

		for x in names:
			try:
				y = get(x)
			except Errors.WafError:
				self.references.setdefault(os.path.splitext(x)[0])
				continue
			y.post()

			tsk = getattr(y, 'cs_task', None) or getattr(y, 'link_task', None)
			if not tsk:
				self.bld.fatal('cs task has no link task for use %r' % self)

			self.other_tgen.append(y)

	def collect_source(self):
		tg = self.tg
		srcs = tg.to_nodes(tg.cs_task.inputs, [])

		self.source_files = []
		self.linked_files = []

		for x in srcs:
			rel_path = x.path_from(tg.path)
			if x.is_child_of(tg.path):
				self.source_files.append(rel_path)
			else:
				self.linked_files.append((x.name, rel_path))

		self.collect_use()

	def write(self):
		print('msvs: creating %r' % self.path)

		# first write the project file
		template1 = msvs.compile_template(CS_PROJECT_TEMPLATE)
		proj_str = template1(self)
		proj_str = msvs.rm_blank_lines(proj_str)
		self.path.stealth_write(proj_str)
		#print proj_str

		return
		# then write the filter
		template2 = compile_template(FILTER_TEMPLATE)
		filter_str = template2(self)
		filter_str = rm_blank_lines(filter_str)
		tmp = self.path.parent.make_node(self.path.name + '.filters')
		tmp.stealth_write(filter_str)

	def collect_properties(self):
		tg = self.tg
		p = OrderedDict()

		# Order matters!
		p['PlatformTarget'] = getattr(tg, 'platform', 'AnyCPU')
		p['DebugSymbols'] = getattr(tg, 'csdebug', tg.env.CSDEBUG) and True or False
		p['DebugType'] = getattr(tg, 'csdebug', tg.env.CSDEBUG)
		p['Optimize'] = '/optimize+' in tg.env.CSFLAGS
		p['OutputPath'] = 'C:\\Work\\Empty'
		p['DefineConstants'] = self.combine_flags('/define:')
		p['ErrorReport'] = 'prompt'
		p['WarningLevel'] = self.combine_flags('/warn:')
		p['NoWarn'] = self.combine_flags('/nowarn:')
		p['TreatWarningsAsErrors'] = '/warnaserror' in tg.env.CSFLAGS
#		p['DocumentationFile'] = getattr(tg, 'csdoc', tg.env.CSDOC) and '$(OutputPath)' + os.sep + p['AssemblyName'] + '.xml'
		p['AllowUnsafeBlocks'] = False

		self.properties[tg.bld.variant] = p;

class idegen(msvs.msvs_generator):
	all_projs = {}
	is_idegen = True
	depth = 0
	all_variants = []

	def init(self):
		msvs.msvs_generator.init(self)
		self.projects_dir = None
		self.proj_by_tg = {}

	def execute(self):
		idegen.depth += 1
		msvs.msvs_generator.execute(self)

	def write_files(self):
		idegen.depth -= 1
		if idegen.depth == 0:
			self.all_projects = idegen.all_projs.values()
			for p in self.all_projects:
				p.build_properties = []
				for k, v in p.properties.iteritems():
					prop = msvs.build_property()
					prop.configuration = k
					prop.platform = v['PlatformTarget']
					prop.outdir = v['OutputPath']
					p.build_properties.append(prop)
					#(k, , v['OutputPath']))#(configuration, platform, output_directory)
					
				p.ctx = self
			self.configurations = idegen.all_variants
			self.platforms = [ 'waf' ]
			
			msvs.msvs_generator.write_files(self)

		elif self.all_projects:
			idegen.all_variants.append(self.variant)

	def add_aliases(self):
		# Don't make build/install aliases
		pass

	def vsnode_cs_target(self, ctx, tg):
		name = tg.name or os.path.splitext(tg.cs_task.gen)[0]
		key = os.path.join(tg.path.abspath(), name)
		val = idegen.all_projs.get(key, None)

		if not val:
			val = vsnode_cs_target(ctx, tg)
			idegen.all_projs[key] = val

		# Update taskgen for this variant
		val.tg = tg
		return val

	def collect_targets(self):
		"""
		Process the list of task generators
		"""
		for g in self.groups:
			for tg in g:
				if not isinstance(tg, TaskGen.task_gen):
					continue

				tg.post()
				if not getattr(tg, 'cs_task', None):
					continue

				p = self.vsnode_cs_target(self, tg)
				p.collect_source() # delegate this processing
				p.collect_properties()
				self.proj_by_tg[tg] = p

		# Resolve dependent task generators to the actual
		# project instance
		for p in self.proj_by_tg.itervalues():
			for tg in p.other_tgen:
				o = self.proj_by_tg[tg]
				k = o.node.path_from(p.node.parent)
				p.project_refs.setdefault(k, o)

		self.all_projects = self.proj_by_tg.values()

'''
class idegen(BuildContext):
#	
	cmd = 'idegen'
	fun = 'build'

	is_idegen = True
	depth = 0
	all_csproj = {}

	def execute_build(self):
		Logs.info("Waf: Entering directory `%s'" % self.variant_dir)

		idegen.depth += 1

		self.recurse([self.run_dir])
		self.collect_projects()

		idegen.depth -= 1
		if idegen.depth == 0:
			self.write_files()

		Logs.info("Waf: Leaving directory `%s'" % self.variant_dir)

	def vsnode_cs_target(self, tg):
		
	def collect_projects(self):
		"""
		Process the list of task generators
		"""
		for g in self.groups:
			for tg in g:
				if not isinstance(tg, TaskGen.task_gen):
					continue

				tg.post()
				if not getattr(tg, 'cs_task', None):
					continue

				p = self.vsnode_cs_target(self, tg)
				p.collect_source()
				p.collect_properties()
				self.all_projects.append(p)

	def write_files(self):
		print 'Write Sln!'
'''