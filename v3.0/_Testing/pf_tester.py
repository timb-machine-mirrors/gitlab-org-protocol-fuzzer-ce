import os
import shutil
import zipfile
import xml.etree.ElementTree as ET
import copy
import testlib
import subprocess
import re
import socket

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
PFADMIN_PATH = r'C:\pf\bin\pf_admin.exe'


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


def file_location_under_path(fname, path):
	# we're hoping there is only one file with that name under
	# the given path. fail loudly when otherwise.
	found_files = []
	for dirpath, dirnames, filenames in os.walk(path):
		for fn in filenames:
			if fname == fn:
				fpath = os.path.join(dirpath, fname)
				fpath = os.path.abspath(fpath)
				found_files.append(fpath)
	assert len(found_files) == 1
	return found_files[0]



def imported_files(base_files, includes):
	'''the search for python modules imported into pit....'''
	# starting at base path, walk down directory tree and find
	# imports (they end in '.py')
	bpath  = base_path()
	ifiles = []
	for f in base_files + includes:
		t = ET.parse(f)
		imports = t.findall(PEACH_SCHEMA_LOCATION + 'Import')
		for elem in imports:
			assert 'import' in elem.keys()
			ifile_name = elem.get('import') + '.py'
			ifile = file_location_under_path(ifile_name, bpath)
			ifiles.append(ifile)
	return ifiles


def collect_pit_files(pit_name, tmp_dir):
	base_files = base_pit_files(pit_name)

	includes = included_files(base_files)

	imports = imported_files(base_files, includes)

	bpath = base_path()

	# all of our files are expected to sit below the bpath
	for base_file in base_files:
		assert base_file.startswith(bpath)
		shutil.copy(base_file, tmp_dir)
	for i in includes + imports:
		assert i.startswith(bpath)

		sub_path = os.path.dirname(i)
		sub_path = sub_path[len(bpath):]
		sub_path = sub_path.strip(os.path.sep)

		tmp_sub_dir = os.path.join(tmp_dir, sub_path)
		if not os.path.exists(tmp_sub_dir):
			os.makedirs(tmp_sub_dir)
		shutil.copy(i, tmp_sub_dir)


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

def is_error_line_header(line):
	# e.g.: '10.0.1.77       Error   2/24/2014 7:26:22 PM'
	# assume it is, fail if it ain't
	line_parts = filter(lambda x: len(x) != 0, re.split('\s', line))

	if len(line_parts) != 5:
		return False
	# first info in an error header is the IP of the originating node
	try:
		socket.inet_aton(line_parts[0])
	except socket.error:
		return False
	if line_parts[1] != 'Error':
		return False
	# look for date
	if line_parts[2].count('/') != 2:
		return False
	if line_parts[-1].lower() not in ['am', 'pm']:
		return False
	return True


def peachfarm_errors():
	assert  os.path.exists(PFADMIN_PATH)
	sp = subprocess.Popen([PFADMIN_PATH, '-errors'],
		                  stdout=subprocess.PIPE, stderr=subprocess.PIPE)
	out, err = sp.communicate()
	assert len(err) == 0 # dont want no standard error output

	errors = {}
	header = ''
	content = []
	for line in out.splitlines():
		if is_error_line_header(line):
			# toss out copyright info at beginning of output
			if header != '':
				errors[header] = "\n".join(content)
			header  = line
			content = []
		else:
			content.append(line)
	return errors


def run_pit_zip(zip_name, tags):
	''' how will we detect failure??? '''
	# assume pathing for windows. throw assertion if this script runs on linux
	# `C:\pf\bin\pf_admin.exe -start -n1 -t Windows -t Linux -t Osx ftp.zip`
	assert  os.path.exists(PFADMIN_PATH)
	admin_responses = {}

	node_oses = ['Osx', 'Linux', 'Windows']
	for node_os in node_oses:
		cmd = [PFADMIN_PATH, '-start', '-n1', '-t', node_os, zip_name]
		print 'RUNNING: ', cmd
		sp = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
		out, err = sp.communicate()
		print 'out', out
		print 'err', err
		if sp.returncode != 0:
			admin_responses[node_os] = [out, err]
	return admin_responses


if __name__ == "__main__":
	temp_dirs = []
	pits = ['BMP']

	for pit_name in pits:
		zip_name = pit_name + '.zip'

		temp_dir  = temp_dir_name(pit_name)
		temp_dirs.append(temp_dir)
		os.mkdir(temp_dir)

		collect_pit_files(pit_name, temp_dir)
		create_pit_zip(zip_name, temp_dir)
		tags = get_os_tags(pit_name)

		prerun_error_count = len(peachfarm_errors())
		admin_responses = run_pit_zip(zip_name, tags)
		print 'ADMIN_RESPONSES', admin_responses
		postrun_error_count = len(peachfarm_errors())
		if prerun_error_count != postrun_error_count:
			# TODO fix this right and report the problem
			print '$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$'
			print admin_responses
			assert prerun_error_count == postrun_error_count

	map(shutil.rmtree, temp_dirs)



