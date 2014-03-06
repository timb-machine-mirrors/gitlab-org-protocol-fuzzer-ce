import os
import shutil
import zipfile
import xml.etree.ElementTree as ET
import copy
import testlib
import subprocess
import re
import socket
import time
import datetime
import sys

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
DUM_CONFIG = DummyPeachTestConfig()
# platform (via testlib.get_platform()) to Os tag as seen be peachfarm
PLATFORM_TAG_LOOKUP = {'win': 'Windows', 'osx': 'Osx', 'linux': 'Linux'}


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


def sample_files(base_files, pit_name):
	# in which dir under _Common/Samples should sample files reside?
	# assumption: 3/3/14 one samplespath elem per pit & one config per pit
	bpath  = base_path()
	sfiles = []

	# only one of the base files should be config/defines. be safe anyway
	defines_files = [bf for bf in base_files if bf.endswith('.config')]
	for df in defines_files:
		t = ET.parse(df)
		def_elems = t.findall('All//Define')
		print def_elems
		for de in def_elems:
			assert 'key'   in de.keys()
			assert 'value' in de.keys()
			if de.get('key') == 'SamplePath':
				spath = de.get('value')
				spath = os.path.join(bpath, spath)
				spath = os.path.abspath(spath)
				dir_contents = os.listdir(spath)
				# a little wide, but keep only/all files w/ pit name in them
				samples = filter(lambda f: pit_name.lower() in f.lower(), dir_contents)
				samples = map(lambda f: os.path.join(spath, f), samples)
				sfiles.extend(samples)
	return sfiles


def collect_pit_files(pit_name, tmp_dir):
	base_files = base_pit_files(pit_name)
	includes = included_files(base_files)
	imports = imported_files(base_files, includes)
	samples = sample_files(base_files, pit_name)
	bpath = base_path()

	# all of our files are expected to sit below the bpath
	for base_file in base_files:
		assert base_file.startswith(bpath)
		shutil.copy(base_file, tmp_dir)
	for i in includes + imports + samples:
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


def most_recent_error_header(error_headers):
	# this is susceptible to a bug if/when errors occur in the same second
	# while possible, it is highly unlikely in our testing environment
	# fix it if it happens
	if error_headers == []:
		raise Exception('No errors to choose from!!')
	dts_to_headers = {} # datetimes to
	for hdr in error_headers:
		line_parts = filter(lambda x: len(x) != 0, re.split('\s', hdr))
		hdr_time_string = ' '.join(line_parts[-3:])
		hdr_tstruct = time.strptime(hdr_time_string, '%m/%d/%Y %I:%M:%S %p')
		hdr_dt =  datetime.datetime.fromtimestamp(time.mktime(hdr_tstruct))
		if hdr_dt in dts_to_headers:
			raise Exception('Multiple errors in same second.')
		dts_to_headers[hdr_dt] = hdr
	most_recent_dt = sorted(dts_to_headers.keys())[-1]
	return dts_to_headers[most_recent_dt]


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


def run_pit_zip(zip_name, args):
	''' how will we detect failure??? '''
	# tags = [{<os_tag>, <test_name>}]
	# assume pathing for windows. throw assertion if this script runs on linux
	# `C:\pf\bin\pf_admin.exe -start -n1 -t Osx -r 0-1 -test Default ftp.zip`
	assert  os.path.exists(PFADMIN_PATH)
	admin_responses = {}
	cmd = [PFADMIN_PATH, '-start', '-n1', '-t', args['os_tag'],
		   '-range', '1-1',
		   '-test',  args['test_name'],
		   zip_name]
	print 'Running: ', ' '.join(cmd)
	sp = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
	out, err = sp.communicate()
	k = args['os_tag'] + ' - ' + args['test_name']
	admin_responses[k] = [' '.join(cmd), out, err]
	return admin_responses


def all_pit_argument_info():
	# this returns a dict of info concerning _all_ of the pits we know about
	arg_info = {} # pit name -> (os, test name)
	targets = list(testlib.get_targets(base_path())) # [{<stuff>}*]
	for target in targets:
		pit_name = target['file'].split('.')[0] # drop the file extension
		if target in testlib.skip_tests:
			continue
		tests = testlib.get_tests(target, DUM_CONFIG)
		target_info = []
		if tests:
			t = tests[0]
			if t.platform == 'all':
				target_info.append((t.test, 'Windows'))
				target_info.append((t.test, 'Linux'))
				target_info.append((t.test, 'Osx'))
			else:
				target_info.append((t.test, PLATFORM_TAG_LOOKUP[t.platform]))
		if len(target_info) == 0:
			print 'SKIPPING', target
			testlib.skip_tests.append(target)
		else:
			# ensure no dupes
			cleaned = list(set(target_info))
			arg_info[pit_name] = []
			for ti in target_info:
				arg_info[pit_name].append({ 'test_name': ti[0], 'os_tag': ti[1] })
	return arg_info


if __name__ == "__main__":
	temp_dirs = []
	failures = [] # [(pit, os, testname, error_output)]
	pits = ['BMP']

	pit_argument_info = all_pit_argument_info()

	# TODO: turn pits into testlib.get_targets() instead
	bpath = base_path()
	pit_names = map(lambda d: d['file'].split('.')[0], testlib.get_targets(bpath))
	pit_names = sorted(pit_names)
	for pit_name in pit_names:
		args_for_runs = pit_argument_info[pit_name]
		zip_name = pit_name + '.zip'

		temp_dir = temp_dir_name(pit_name)
		temp_dirs.append(temp_dir)
		os.mkdir(temp_dir)

		collect_pit_files(pit_name, temp_dir)
		create_pit_zip(zip_name, temp_dir)

		for run_args in args_for_runs:
			prerun_error_count = len(peachfarm_errors())
			admin_responses = run_pit_zip(zip_name, run_args)
			print 'ADMIN_RESPONSES: \n', admin_responses
			postrun_errors = peachfarm_errors()
			postrun_error_count = len(postrun_errors)
			if postrun_error_count != prerun_error_count:

				prev_error_hdr  = most_recent_error_header(postrun_errors)
				prev_error_info = postrun_errors[prev_error_hdr]
				prev_error     = prev_error_hdr + '\n' + prev_error_info

				failures.append((pit_name,
								 run_args['os_tag'], run_args['test_name'],
								 prev_error))
				# TODO fix this right and report the problem
				implement_error_reporting = False
				assert implement_error_reporting

	map(shutil.rmtree, temp_dirs)

	if failures != []:
		failures.sort(key=lambda x: x[0]) # by pit name
		for fail in failures:
			print 'FAILED: '
		print 'Failing Tests: ', '  '.join(map(lambda f: '-'.join(f), failures))
		sys.exit(len(failures))


