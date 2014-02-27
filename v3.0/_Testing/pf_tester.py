import os
import shutil
import zipfile
import xml.etree.ElementTree as ET
import copy

# as it is seen in the xml elements representation (not necessarily repr)
PEACH_SCHEMA_LOCATION = '{http://peachfuzzer.com/2012/Peach}'
OTHER_OS_SEP = {'/': '\\', '\\': '/'} # *nix to win, and vice versa

def temp_dir_name(pit_name):
	pid = os.getpid()
	tmp_dir = 'tmp_%s_%s' % (pit_name, pid)
	return tmp_dir

def make_temp_dir(dir_name):
	tmp_dir = os.path.join('.', dir_name) 
	os.mkdir(tmp_dir)

def base_pit_files(pit_name):
	# returns fullpaths to basefiles
	pit_dirs = ['Application', 'Audio', 'Image', 'Net', 'Video']
	files = []
	for pd in pit_dirs:
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

	return os.path.abspath(actual_path)


def included_files(base_files):
	# base files are base pit files. '.xml' and '.xml.config'
	# we search these for includes of other files
	# returns fullpaths to included files
	'''
	look at includes!!! see notes! mostly:
		no xpath attrs, this is different
		includes tags w/ includes attrs??? maybe...
		everything else should be good
	'''
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

	ifiles = included_files(base_files)
	assert False

def create_pit_zip(pit_name, tmp_dir):
	assert False

def get_os_tags(pit_name):
	''' this will interact with pre-existing tester code'''
	assert False

def run_pit_zip(pit_name):
	''' how will we detect failure??? '''
	assert False


def mai_main():
	pits = ['ftp'] # temporary shim

	# make temp dir
	temp_dirs = map(temp_dir_name, pits)
	map(make_temp_dir, temp_dirs)
	print os.listdir('.')

	# collect files
	## detect inclusions
	## make subdirs
	## shove everything in

	# zip up
	# push out for one iteration
	## what is success???
	## complain on failure


	# cleanup
	print "cleaning..."
	map(shutil.rmtree, temp_dirs)
	print os.listdir('.')



if __name__ == "__main__":
	#mai_main()
	# don't automatically close my win term bro!
	# input("el fin....")
	collect_pit_files('ftp', 'none')
