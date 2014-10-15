#!/usr/bin/env python

import os, sys, argparse, fnmatch, zipfile

outdir  = 'output/release'

if __name__ == "__main__":
	p = argparse.ArgumentParser(description = 'zip pits repo')
	p.add_argument('--buildtag', default = '0.0.0.0', help = 'buildtag')

	c = p.parse_args()

	tag = '%(buildtag)s' % c.__dict__

	json = os.path.join(outdir, 'release.json')

	if not os.path.isdir(outdir):
		os.makedirs(outdir)

	print 'Creating: %s' % json

	try:
		os.unlink(json)
	except Exception:
		pass

	with open(json, 'w') as f:
		f.write('{"version":"%s"}' % tag)

	sys.exit(0)

