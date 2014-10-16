#!/usr/bin/env python

import os, sys, argparse, fnmatch, zipfile, shutil, json, datetime

buildtag = None
outdir   = 'output'
reldir   = os.path.join(outdir, 'release')
archive  = 'archive.zip'
docfile  = os.path.join(outdir, 'doc', 'archive.zip')
pitfile  = 'peach-pits-%(buildtag)s.zip'

peach_docs = [ 'Peach_Professional.pdf', 'webhelp/*' ]
pit_docs   = [ 'Pit_Library.pdf' ]

pits = [
	{
		'root' : 'pits/pro',
		'incl' : '*',
		'excl' : '_Common/Specs/* _Testing/* */.gitignore *.TODO *.adoc *.test *.pdf'
	},
]

class Object:
	def to_JSON(self):
		return json.dumps(self, default=lambda o: o.__dict__, sort_keys=True, indent=4)

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
		for i in docs:
			
			src = os.path.join(docdir, i)
			mode = os.stat(src).st_mode

			#print ' + %s (%s)' % (i, oct(mode))

			z.write(src, i)

			zi = z.getinfo(i)
			zi.external_attr = mode << 16L

	sha1sum(pkg)

def make_pits(dest, docs):
	# Make pits zip and include docs in it

	print ''
	print 'Creating %s' % dest
	print ''

	docdir = os.path.dirname(docfile)

	with zipfile.ZipFile(dest, 'w', compression=zipfile.ZIP_DEFLATED) as z:
		for i in pits:
			for f in glob(i['root'], i['incl'], i['excl']):
				src = os.path.join(i['root'], f)
				mode = os.stat(src).st_mode

				print ' + %s (%s)' % (f, oct(mode))

				z.write(src, f)

				zi = z.getinfo(f)
				zi.external_attr = mode << 16L

		for f in docs:
			src = os.path.join(docdir, f)
			mode = os.stat(src).st_mode

			print ' + %s (%s)' % (f, oct(mode))

			z.write(src, f)

			zi = z.getinfo(f)
			zi.external_attr = mode << 16L

	sha1sum(dest)

if __name__ == "__main__":
	p = argparse.ArgumentParser(description = 'make release zips')
	p.add_argument('--buildtag', default = '0.0.0.0', help = 'buildtag')
	p.add_argument('--nightly', default = True, help = 'is nightly build')

	c = p.parse_args()
	buildtag = c.buildtag
	pitfile = pitfile % c.__dict__

	if os.path.isdir(reldir):
		shutil.rmtree(reldir)

	if not os.path.isdir(reldir):
		os.makedirs(reldir)

	pkgs = extract_pkg()
	docs = extract_doc()

	toAdd = [ x for x in docs if matches(x, peach_docs)]

	for x in pkgs:
		update_pkg(x, toAdd)

	toAdd = [x for x in docs if matches(x, pit_docs)]

	make_pits(os.path.join(reldir, pitfile), toAdd)

	print ''
	print 'Generating release.json'
	print ''

	d = datetime.datetime.now()

	o = Object()
	o.files = [ x for x in pkgs if 'release' in x ]
	o.pits = pitfile
	o.product = 'Peach Professional'
	o.build = buildtag
	o.nightly = c.nightly
	o.date = '%s/%s/%s' % (d.day, d.month, d.year)

	rel = os.path.join(reldir, 'release.json')

	try:
		os.unlink(rel)
	except Exception:
		pass

	with open(rel, 'w') as f:
		f.write(o.to_JSON())

	sys.exit(0)

