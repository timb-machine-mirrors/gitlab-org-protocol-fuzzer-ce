import os
import shutil
import zipfile
import xml.etree.ElementTree as ET
import copy
import testlib

class DummyPeachTestConfig:
	iterations = None
	timeout    = None
	logdir     = None
	peach      = None
	color      = None

	def __init__(self):
		pass

# as it is seen in the xml element's representation (not necessarily repr)
PEACH_SCHEMA_LOCATION = '{http://peachfuzzer.com/2012/Peach}'
OTHER_OS_SEP = {'/': '\\', '\\': '/'} # *nix to win, and vice versa
PIT_DIRS = ['Application', 'Audio', 'Image', 'Net', 'Video']


def base_path():
	# find the 'v3.0' directory. this is what ##Path## should be most times
	d = os.path.abspath(__file__)
	while True:
		if os.path.basename(d) == 'v3.0':
			break
		d = os.path.dirname(d)
	return d


def temp_dir_name(pit_name):
	pid = os.getpid()
	tmp_dir = 'tmp_%s_%s' % (pit_name, pid)
	return os.path.abspath(os.path.join('.', tmp_dir))


def base_pit_files(pit_name):
	# returns fullpaths to basefiles
	files = []
	for pd in PIT_DIRS:
		listing = os.listdir(os.path.join('..', pd))
		pfname = pit_name + '.xml'
		cfname = pit_name + '.xml.config'
		if pfname in listing:
			pf_path = os.path.join('..', pd, pfname)
			pf_path = os.path.abspath(pf_path)
			files.append(pf_path)
		if cfname in listing:
			cf_path = os.path.join('..', pd, cfname)
			cf_path = os.path.abspath(cf_path)
			files.append(cf_path)
	return files


def actual_include_location(ifile_src):
	# pop up the current path until we find a directory corresponding
	# to '##Path##'
	# return an os.path.abspath() to the included file path

	PATH_SUB = "file:##Path##"
	# nomalize ifile_src to path that makes sense on current os
	if os.path.sep not in ifile_src:
		ifile_src = ifile_src.replace(OTHER_OS_SEP[os.path.sep], os.path.sep)

	curdir = os.getcwd()
	if ifile_src.startswith(PATH_SUB):
		ifile_src = ifile_src[len(PATH_SUB):]
	indicator = ifile_src.split(os.path.sep)[1]
	while True:
		if indicator in os.listdir(curdir):
			break
		# analog of `cd ..`
		curdir = os.path.dirname(curdir)
	actual_path = os.path.join(curdir, *ifile_src.split(os.path.sep))

	abspath = os.path.abspath(actual_path)
	assert os.path.exists(abspath)
	return abspath


def included_files(base_files):
	# base files are base pit files. '.xml' and '.xml.config'
	# we search these for includes of other files
	# returns fullpaths to included files
	pit_files = copy.deepcopy(base_files)
	include_files = []
	for pf in pit_files:
		t = ET.parse(pf)
		includes = t.findall(PEACH_SCHEMA_LOCATION + 'Include')
		for include in includes:
			# includes that have xpaths don't refer to files
			# '.keys()' is how you get the elements attributes
			if 'xpath' in include.keys():
				continue
			assert 'src' in include.keys()
			include_files.append(include.get('src'))
			pit_files.append(
			    actual_include_location(include.get('src')))

	return map(actual_include_location, include_files)


def collect_pit_files(pit_name, tmp_dir):
	base_files = base_pit_files(pit_name)

	includes = included_files(base_files)

	bpath = base_path()

	# all of our files are expected to sit below the bpath
	for base_file in base_files:
		assert base_file.startswith(bpath)
		shutil.copy(base_file, tmp_dir)
	for include in includes:
		assert include.startswith(bpath)

		sub_path = os.path.dirname(include)
		sub_path = sub_path[len(bpath):]
		sub_path = sub_path.strip(os.path.sep)

		tmp_sub_dir = os.path.join(tmp_dir, sub_path)
		if not os.path.exists(tmp_sub_dir):
			os.makedirs(tmp_sub_dir)
		shutil.copy(include, tmp_sub_dir)


def create_pit_zip(zip_name, tmp_dir):
	bpath = base_path()
	with zipfile.ZipFile(zip_name, 'w') as zf:
		for root, dirs, files in os.walk(tmp_dir):
			for f in files:
				f = os.path.join(root, f)
				assert f.startswith(tmp_dir)
				# want local subpath from head of the temp dir down to file
				zip_dest = f[len(tmp_dir):]
				zip_dest.strip(os.path.sep)
				zf.write(f, zip_dest)


def get_os_tags(pit_name):
	''' this will interact with pre-existing tester code'''
	#######################################################
	# assumption: right now everything is just running x64
	#######################################################
	platform_map = {
		'all'  : ['Windows', 'Linux', 'Osx'],
		'win'  : ['Windows'],
		'osx'  : ['Osx'],
		'linux': ['Linux']
	}
	bpath = base_path()
	if not pit_name.endswith('.xml'):
		pit_name += '.xml'
	for d in PIT_DIRS:
		fulld = os.path.join(bpath, d)
		if pit_name in os.listdir(fulld):
			pdir = fulld
			break

	target = {
	    "path": pdir,
	    "file": pit_name
	}
	tests = testlib.get_tests(target, DummyPeachTestConfig)
	tags  = []
	for t in tests:
		assert t.platform in ['all', 'win', 'osx', 'linux']
		tags.extend(platform_map[t.platform])
	return list(set(tags))


def run_pit_zip(pit_name, tags):
	''' how will we detect failure??? '''
	# assume pathing for windows. throw assertion if this script runs on linux
	# `C:\pf\bin\pf_admin.exe -start -n1 -t Windows -t Linux -t Osx ftp.zip`
	assert 42 == 'the answer'


def mai_main():
	pits = ['ftp'] # temporary shim

	# push out for one iteration
	## what is success???
	## complain on failure

	# cleanup
	map(shutil.rmtree, temp_dirs)
	assert False
	os.remove('ftp.zip')


if __name__ == "__main__":
	#mai_main()

	pit_name = 'ftp'
	zip_name = 'ftp.zip'

	zip_pit_dir  = temp_dir_name(pit_name)
	os.mkdir(zip_pit_dir)
	collect_pit_files(pit_name, zip_pit_dir)
	create_pit_zip(zip_name, zip_pit_dir)
	tags = get_os_tags(pit_name)
	run_pit_zip(zip_name, tags)


