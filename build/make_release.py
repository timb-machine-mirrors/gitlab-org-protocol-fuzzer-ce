#!/usr/bin/env python

import os, sys, argparse, fnmatch, zipfile, shutil, json, datetime

buildtag = None
outdir   = 'output'
reldir   = os.path.join(outdir, 'release')
archive  = 'archive.zip'
docfile  = os.path.join(outdir, 'doc', 'archive.zip')
pitfile  = 'peach-pits-%(buildtag)s.zip'
pittrial = 'peach-pits-%(buildtag)s-trial.zip'

peach_docs = {
	'docs/' : [ 'Peach_Professional.pdf', 'webhelp/*' ],
	'sdk/'  : [ 'apidocs/*' ],
}
pit_docs   = {
	''      : [ 'Pit_Library.pdf' ],
}
trial_docs = {
	''      : [ 'Pit_Library_Trial.pdf' ],
}

pits = [
	{
		'root' : 'pits/pro',
		'incl' : '*',
		'excl' : 'Test/* _Common/Specs/* _Testing/* */.gitignore *.TODO *.adoc *.test *.pdf'
	},
]

pits_trial = [
	{
		'root' : 'pits/pro',
		'incl' : 'Net/SNMP* Image/PNG* _Common/Samples/Image/*.png _Common/Models/Image/PNG* _Common/Models/Net/SNMP* ',
		'excl' : 'Test/* _Common/Specs/* _Testing/* */.gitignore *.TODO *.adoc *.test *.pdf'
	},
]

releases = [
	{
		'name'    : 'pro',
		'all'     : 'peach-pro-%(buildtag)s.zip',
		'product' : 'Peach Professional',
		'filter'  : lambda s: s.startswith('peach-pro'),
	},
	{
		'name'    : 'ent',
		'all'     : 'peach-pro-%(buildtag)s.zip',
		'product' : 'Peach Enterprise',
		'filter'  : lambda s: s.startswith('peach-pro'),
	},
]

def to_JSON(self):
	return json.dumps(self, default=lambda o: o.__dict__, sort_keys=True, indent=4)

class Object:
	pass

def to_list(sth):
	if isinstance(sth, str):
		return sth.split()
	else:
		return sth

def matches(name, filters):
	for i in filters:
		if fnmatch.fnmatch(name, i):
			return i

	return None

def glob(root, include = [], exclude = []):
	incs = to_list(include)
	rejs = to_list(exclude)

	ret = []

	for (path, dirs, files) in os.walk(root):
		for f in files:
			x = os.path.normpath(os.path.join(os.path.relpath(path, root), f))
			if matches(x, incs) and not matches(x, rejs):
				ret.append(x)

	return ret

def sha1sum(filename):
	try:
		from hashlib import sha1 as sha
	except ImportError:
		from sha import sha

	with open(filename, 'r') as f:
		digest = sha(f.read()).hexdigest()

	with open(filename + '.sha1', 'w') as f:
		f.write('SHA1(%s)= %s\n' % (os.path.basename(filename), digest))

def extract_pkg():
	# Lookfor output/XXX/archive.zip
	# Open up archive.zip and look for zip files in the pkg directory
	# Extract said zips to the release folder

	print ''
	print 'Extract packages'
	print ''

	pkgs = []

	for (path, dirs, files) in os.walk(outdir):
		for name in dirs:
			f = os.path.join(path, name, archive)
			if not os.path.isfile(f):
				print 'IGNORING   %s' % os.path.dirname(f)
				continue
			
			print 'PROCESSING %s' % f
			with zipfile.ZipFile(f, 'r') as z:
				for i in z.infolist():
					if not i.filename.startswith('pkg/'):
						continue
					if not i.filename.endswith('.zip'):
						continue

					print ' - %s' % i.filename
					i.filename = os.path.basename(i.filename)
					z.extract(i, reldir)
					pkgs.append(os.path.join(reldir, i.filename))
		break

	return pkgs

def extract_doc():
	# Lookfor output/doc/archive.zip
	# Extract to output/doc and make list of all files

	print ''
	print 'Extract documentation'
	print ''

	files = []

	basedir = os.path.dirname(docfile)

	print 'PROCESSING %s' % docfile

	with zipfile.ZipFile(docfile, 'r') as z:
		for i in z.infolist():
			print ' - %s' % i.filename
			z.extract(i, basedir)
			files.append(os.path.join(i.filename))

	return files

def update_pkg(pkg, docs):
	# Add all files in docs to pkg zip

	print ''
	print 'Adding docs to %s' % pkg
	print ''

	docdir = os.path.dirname(docfile)

	with zipfile.ZipFile(pkg, 'a', compression=zipfile.ZIP_DEFLATED) as z:
		for b,i in docs:
			src = os.path.join(docdir, i)
			dst = b + i

			mode = os.stat(src).st_mode

			#print ' + %s (%s)' % (dst, oct(mode))

			z.write(src, dst)

			zi = z.getinfo(dst)
			zi.external_attr = mode << 16L

	sha1sum(pkg)

def make_pits(dest, library, docs):
	# Make pits zip and include docs in it

	print ''
	print 'Creating %s' % dest
	print ''

	docdir = os.path.dirname(docfile)

	with zipfile.ZipFile(dest, 'w', compression=zipfile.ZIP_DEFLATED) as z:
		for i in library:
			for f in glob(i['root'], i['incl'], i['excl']):
				src = os.path.join(i['root'], f)
				mode = os.stat(src).st_mode

				# zip files need '/' as path delimeter
				f = f.replace('\\', '/')

				print ' + %s (%s)' % (f, oct(mode))

				z.write(src, f)

				zi = z.getinfo(f)
				zi.external_attr = mode << 16L

		for b,f in docs:
			src = os.path.join(docdir, f)
			dst = b+f
			mode = os.stat(src).st_mode

			print ' + %s (%s)' % (dst, oct(mode))

			z.write(src, dst)

			zi = z.getinfo(dst)
			zi.external_attr = mode << 16L

	sha1sum(dest)

def filter_docs(files, filters):
	ret = []
	for f in files:
		for k,v in filters.iteritems():
			if matches(f, v):
				ret.append((k,f))
	return ret

if __name__ == "__main__":
	p = argparse.ArgumentParser(description = 'make release zips')
	p.add_argument('--buildtag', default = '0.0.0.0', help = 'buildtag')
	p.add_argument('--nightly', default = True, help = 'is nightly build')

	c = p.parse_args()
	buildtag = c.buildtag
	pitfile = pitfile % c.__dict__
	pittrial = pittrial % c.__dict__

	if os.path.isdir(reldir):
		shutil.rmtree(reldir)

	if not os.path.isdir(reldir):
		os.makedirs(reldir)

	pkgs = extract_pkg()
	docs = extract_doc()

	toAdd = filter_docs(docs, peach_docs)

	for x in pkgs:
		update_pkg(x, toAdd)

	toAdd = filter_docs(docs, pit_docs)

	make_pits(os.path.join(reldir, pitfile), pits, toAdd)

	toAdd = filter_docs(docs, trial_docs)

	make_pits(os.path.join(reldir, pittrial), pits_trial, toAdd)

	d = datetime.datetime.now()

	names = [ os.path.basename(x) for x in pkgs ]

	for r in releases:
		print ''
		print 'Generating release %s.%s' % (buildtag, r['name'])
		print ''

		o = Object()
		o.files = [ x for x in names if 'release' in x and r['filter'](x)]
		o.pits = pitfile
		o.trial = pittrial
		o.product = r['product']
		o.build = buildtag
		o.nightly = c.nightly
		o.date = '%s/%s/%s' % (d.day, d.month, d.year)

		if not o.files:
			print 'No files found, skipping!'
			continue

		path = os.path.join(reldir, '%s.%s' % (buildtag, r['name']))
		os.mkdir(path)

		rel = os.path.join(path, 'release.json')

		for f in o.files:
			src = os.path.join(reldir, f)
			dst = os.path.join(path, f)
			shutil.copy(src, dst)
			shutil.copy(src + '.sha1', dst + '.sha1')

		for f in [ pitfile, pittrial ]:
			src = os.path.join(reldir, f)
			dst = os.path.join(path, f)
			shutil.copy(src, dst)
			shutil.copy(src + '.sha1', dst + '.sha1')

		# Make all zip
		dst = os.path.join(path, r['all'] % c.__dict__)

		with zipfile.ZipFile(dst, 'w', compression=zipfile.ZIP_STORED) as z:
			for x in o.files:
				src = os.path.join(path, x)
				z.write(src, x)

		sha1sum(dst)
		o.files.append(os.path.basename(dst))

		with open(rel, 'w') as f:
			j = to_JSON(o)
			print j
			f.write(j)

	for x in pkgs + [ os.path.join(reldir, pitfile), os.path.join(reldir, pittrial) ]:
		try:
			os.unlink(x)
			os.unlink(x + '.sha1')
		except:
			pass

	sys.exit(0)

