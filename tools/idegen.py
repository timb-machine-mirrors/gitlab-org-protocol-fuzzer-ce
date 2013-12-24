import os
import os.path

from collections import OrderedDict

from waflib.extras import msvs
from waflib import Utils, TaskGen, Logs, Task, Context, Node, Options, Errors

msvs.msvs_generator.cmd = 'msvs2010'

CS_PROJECT_TEMPLATE = r'''<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">${project.build_properties[0].configuration}</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">${project.build_properties[0].platform}</Platform>
    <ProductVersion>8.0.30703</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    ${for k, v in project.globals.iteritems()}
    <${k}>${v}</${k}>
    ${endfor}
  </PropertyGroup>

  ${for props in project.build_properties}
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == '${props.configuration}|${props.platform}' ">
    ${for k, v in props.properties.iteritems()}
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
    ${for r in project.project_refs}
    <ProjectReference Include="${r.path}">
      <Project>{${r.uuid}}</Project>
      <Name>${r.name}</Name>
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
		self.base = getattr(ctx, 'projects_dir', None) or tg.path
		self.node = self.base.make_node(os.path.splitext(tg.name)[0] + '.csproj') # the project file as a Node
		msvs.vsnode_project.__init__(self, ctx, self.node)
		self.name = os.path.splitext(tg.gen)[0]
		self.tg = tg # task generators

		self.is_active = True
		self.globals      = OrderedDict()
		self.properties   = OrderedDict()
		self.references   = {} # Name -> HintPath
		self.source_files = [] # Name
		self.linked_files = [] # (Name, HintPath)
		self.project_refs = [] # uuid

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

			base = getattr(self.ctx, 'projects_dir', None) or y.path
			other = base.make_node(os.path.splitext(y.name)[0] + '.csproj')
			
			dep = msvs.build_property()
			dep.path = other.path_from(self.node.parent)
			dep.uuid = msvs.make_uuid(other.abspath())
			dep.name = y.name

			self.project_refs.append(dep)

	def collect_source(self):
		tg = self.tg
		srcs = tg.to_nodes(tg.cs_task.inputs, [])

		for x in srcs:
			proj_path = x.path_from(tg.path)
			rel_path = x.path_from(self.node.parent)
			
			if not x.is_child_of(tg.path):
				self.linked_files.append((x.name, rel_path))
			elif proj_path == rel_path:
				self.source_files.append(rel_path)
			else:
				self.linked_files.append((proj_path, rel_path))

		self.collect_use()

	def write(self):
		print('msvs: creating %r' % self.path)

		# first write the project file
		template1 = msvs.compile_template(CS_PROJECT_TEMPLATE)
		proj_str = template1(self)
		proj_str = msvs.rm_blank_lines(proj_str)
		self.path.stealth_write(proj_str)

	def collect_properties(self):
		tg = self.tg
		g = self.globals

		asm_name = os.path.splitext(tg.cs_task.outputs[0].name)[0]
		out = os.path.join('bin', self.ctx.variant)

		# Order matters!
		g['ProjectGuid'] = '{%s}' % self.uuid
		g['OutputType'] = getattr(tg, 'bintype', tg.gen.endswith('.dll') and 'library' or 'exe')
		g['AppDesignerFolder'] = 'Properties'
		g['RootNamespace'] = getattr(tg, 'namespace', self.name)
		g['AssemblyName'] = asm_name
		g['TargetFrameworkVersion'] = 'v4.0'
		g['TargetFrameworkProfile'] = os.linesep + '    '
		g['FileAlignment'] = '512'

		p = self.properties

		# Order matters!
		p['PlatformTarget'] = getattr(tg, 'platform', 'AnyCPU')
		p['DebugSymbols'] = getattr(tg, 'csdebug', tg.env.CSDEBUG) and True or False
		p['DebugType'] = getattr(tg, 'csdebug', tg.env.CSDEBUG)
		p['Optimize'] = '/optimize+' in tg.env.CSFLAGS
		p['OutputPath'] = out
		p['DefineConstants'] = self.combine_flags('/define:')
		p['ErrorReport'] = 'prompt'
		p['WarningLevel'] = self.combine_flags('/warn:')
		p['NoWarn'] = self.combine_flags('/nowarn:')
		p['TreatWarningsAsErrors'] = '/warnaserror' in tg.env.CSFLAGS
		p['DocumentationFile'] = getattr(tg, 'csdoc', tg.env.CSDOC) and out + os.sep + asm_name + '.xml'
		p['AllowUnsafeBlocks'] = False

class idegen(msvs.msvs_generator):
	all_projs = {}
	is_idegen = True
	depth = 0

	def init(self):
		msvs.msvs_generator.init(self)

		#self.projects_dir = None
		self.solution_name = self.env.APPNAME + '.sln'

		if not getattr(self, 'vsnode_cs_target', None):
			self.vsnode_cs_target = vsnode_cs_target

	def execute(self):
		idegen.depth += 1
		msvs.msvs_generator.execute(self)

	def write_files(self):
		if self.all_projects:
			idegen.all_projs[self.variant] = self.all_projects

		idegen.depth -= 1
		if idegen.depth == 0:
			self.all_projects = self.flatten_projects()
			msvs.msvs_generator.write_files(self)

	def flatten_projects(self):
		ret = {}
		configs = {}
		platforms = {}
		
		print 'Flatten!'
		for k,v in idegen.all_projs.iteritems():
			for p in v:
				p.ctx = self
				ret.setdefault(p.uuid, p)

				if not getattr(p, 'tg', []):
					continue

				main = ret[p.uuid]

				env = p.tg.bld.env

				prop = msvs.build_property()
				prop.configuration = '%s_%s' % (env.TARGET, env.VARIANT)
				prop.platform = p.properties['PlatformTarget']
				prop.properties = p.properties

				configs.setdefault(prop.configuration)
				platforms.setdefault(prop.platform)

				main.build_properties.append(prop)

		self.configurations = configs.keys()
		self.platforms = platforms.keys()
		
		return ret.values()

	def add_aliases(self):
		pass

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
				self.all_projects.append(p)
