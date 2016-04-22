#!/usr/bin/env python

import os, sys, argparse, fnmatch, zipfile, shutil, json, datetime

buildtag = None
outdir   = 'output'
reldir   = os.path.join(outdir, 'release')
pitfile  = os.path.join(outdir, 'pits.zip')
docfile  = os.path.join(outdir, 'doc.zip')
tmpdir   = os.path.join(outdir, 'tmp')

peach_docs = {
	''      : [ 'sdk/*', 'docs/*' ],
}

releases = [
	{
		'dirname' : '%(buildtag)s',
		'all'     : 'peach-pro-%(buildtag)s.zip',
		'product' : 'Peach Studio',
		'filter'  : lambda s: s.startswith('peach-pro'),
	},
]

'''
This script expects the following directory structure:
output
  doc/doc.zip
  pits/${pit}.zip
  ${platform}_release/pkg/peach-pro-${buildtag}-${platform}_release.zip

The result should be:    <--- archive to smb://nas/builds/peach-pro
output
  release
  ${buildtag}          <--- publish to ssh://dl.peachfuzzer.com
    release.json
    peach-pro-${buildtag}-${platform}_release.zip
    peach-pro-${buildtag}-${platform}_release.zip.sha1
    pits
      ${pit}.zip
    datasheets
      html
        ${pit}.html
      pdf
        ${pit}.pdf
'''

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
		# Copy for output/$CFG_release/pkg/*.zip to release folder

		print ''
		print 'Extract packages'
		print ''

		pkgs = []

		for cfg in os.listdir(outdir):
				if not cfg.endswith('release'):
						print 'IGNORING   %s' % cfg
						continue

				path = os.path.join(outdir, cfg, 'pkg')
				if not os.path.exists(path):
						continue

				print 'PROCESSING %s' % cfg

				for item in os.listdir(path):
						if not item.endswith('.zip'):
								continue
						print ' - %s' % item
						shutil.copy(os.path.join(path, item), reldir)
						pkgs.append((
							os.path.join(reldir, item),
							os.path.join(path, 'peach.xsd')
						))

		return pkgs

def extract_doc():
	# Lookfor output/doc.zip
	# Extract to release/tmp/doc and make list of all files

	print ''
	print 'Extract documentation'
	print ''

	files = []

	docdir = os.path.join(tmpdir, 'doc')

	print 'PROCESSING %s' % docfile

	with zipfile.ZipFile(docfile, 'r') as z:
		for i in z.infolist():
			print ' - %s' % i.filename
			z.extract(i, docdir)
			files.append(os.path.join(i.filename))

	return files

def extract_pits():
	# Lookfor output/pits.zip
	# Extract to release/tmp/pits and make list of all files

	print ''
	print 'Extract pits'
	print ''

	files = []
	packs = None
	archives = None

	pitdir = os.path.join(tmpdir, 'pits')

	print 'PROCESSING %s' % pitfile

	with zipfile.ZipFile(pitfile, 'r') as z:
		for i in z.infolist():
			if i.filename.startswith('gump/'):
				continue
			if os.path.basename(i.filename) == 'shipping_packs.json':
				packs = z.read(i)
			if os.path.basename(i.filename) == 'shipping_pits.json':
				archives = z.read(i)
			if i.filename.endswith('.zip'):
				print ' - %s' % i.filename
				z.extract(i, pitdir)
				files.append(i.filename)
			if i.filename.startswith('docs/datasheets') and not i.filename.endswith('/'):
				print ' - %s' % i.filename
				z.extract(i, pitdir)
				files.append(i.filename)

	# TODO Filter docs based on shipping pits

	packs = json.loads(packs)
	archives = json.loads(archives)

	return (files, packs, archives)

def update_pkg(pkg, xsd, docs):
	# Add all files in docs to pkg zip

	print ''
	print 'Adding docs to %s' % pkg
	print ''

	docdir = os.path.join(tmpdir, 'doc')

	with zipfile.ZipFile(pkg, 'a', compression=zipfile.ZIP_DEFLATED) as z:
		for b,i in docs:
			src = os.path.join(docdir, i)
			dst = b + i

			mode = os.stat(src).st_mode

			#print ' + %s (%s)' % (dst, oct(mode))

			z.write(src, dst)

			zi = z.getinfo(dst)
			zi.external_attr = mode << 16L

		z.write(xsd, 'peach.xsd')

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

	if os.path.isdir(reldir):
		shutil.rmtree(reldir)

	if not os.path.isdir(reldir):
		os.makedirs(reldir)

	pkgs = extract_pkg()
	docs = extract_doc()
	(pit_files, packs, pit_archives) = extract_pits()

	toAdd = filter_docs(docs, peach_docs)

	for pkg, xsd in pkgs:
		if 'internal' not in pkg:
			update_pkg(pkg, xsd, toAdd)

	d = datetime.datetime.now()

	names = [ os.path.basename(x) for x, y in pkgs ]

	for r in releases:
		dirname = r['dirname'] % c.__dict__

		print ''
		print 'Generating release folder %s' % dirname
		print ''

		manifest = dict(
			files = [ x for x in names if 'release' in x and r['filter'](x)],
			product = r['product'],
			build = buildtag,
			nightly = c.nightly,
			version = 2,
			date = '%s/%s/%s' % (d.day, d.month, d.year),
			pit_archives = pit_archives,
			packs = packs,
		)

		if not manifest['files']:
			print 'No files found, skipping!'
			continue

		path = os.path.join(reldir, dirname)
		os.mkdir(path)
		os.mkdir(os.path.join(path, 'pits'))

		rel = os.path.join(path, 'release.json')

		for f in manifest['files']:
			src = os.path.join(reldir, f)
			dst = os.path.join(path, f)
			shutil.copy(src, dst)
			shutil.copy(src + '.sha1', dst + '.sha1')

		for f in pit_files:
			src = os.path.join(tmpdir, 'pits', f)
			# Eat the 'docs/' prefix
			if (f.startswith('docs/')):
				f = f[5:]
			dst = os.path.join(path, 'pits', f)
			d = os.path.dirname(dst)
			if not os.path.isdir(d):
				os.makedirs(d)
			shutil.copy(src, dst)

		data = json.dumps(manifest, sort_keys=True, indent=4)
		with open(rel, 'w') as f:
			f.write(data)

	if os.path.isdir(tmpdir):
		shutil.rmtree(tmpdir)

	for x in pkgs:
		if 'internal' in x:
			path = os.path.join(reldir, buildtag)
			shutil.copy(x, path)
		try:
			os.unlink(x)
			os.unlink(x + '.sha1')
		except:
			pass

	sys.exit(0)

