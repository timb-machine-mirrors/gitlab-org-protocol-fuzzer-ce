#!/usr/bin/env python

import os, sys, argparse, fnmatch, zipfile


outdir  = 'output'
outfile = 'pits-%(buildtag)s.zip'

manifest = [
	{ 'root' : 'v3.0', 'incl' : '*', 'excl' : '_Testing/*' },
]

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
			x = os.path.join(os.path.relpath(path, root), f)
			if matches(x, incs) and not matches(x, rejs):
				ret.append(x)

	return ret

if __name__ == "__main__":
	script = os.path.abspath(os.path.dirname(sys.argv[0]))
	cwd = os.getcwd()

	if cwd != script:
		print 'Script must be run from \'%s\'.' % script
		sys.exit(1)

	p = argparse.ArgumentParser(description = 'zip pits repo')
	p.add_argument('--buildtag', default = '0.0.0.0', help = 'buildtag')

	c = p.parse_args()

	zipname = outfile % c.__dict__
	fullzip = os.path.join(outdir, zipname)
	shaname = fullzip + '.sha1'

	if not os.path.isdir(outdir):
		os.makedirs(outdir)

	print 'Creating: %s' % fullzip

	try:
		os.unlink(fullzip)
	except Exception:
		pass

	try:
		os.unlink(shaname)
	except Exception:
		pass

	z = zipfile.ZipFile(fullzip, 'w', compression=zipfile.ZIP_DEFLATED)

	for i in manifest:
		for f in glob(i['root'], i['incl'], i['excl']):
			src = os.path.join(i['root'], f)
			print 'Adding: %s' % (f)
			z.write(src, f)
			#zi = z.getinfo(dest)
			#zi.external_attr = attr

	z.close()

	try:
		from hashlib import sha1 as sha
	except ImportError:
		from sha import sha

	with open(fullzip, 'r') as f:
		digest = sha(f.read()).hexdigest()

	with open(shaname, 'w') as f:
		f.write('SHA1(%s)= %s\n' % (zipname, digest))

	sys.exit(0)

