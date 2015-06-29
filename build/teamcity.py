#!/usr/bin/env python

import re
import subprocess
import argparse

def init(match):

	return buildtag

def promote(buildtag):
	pass

if __name__ == "__main__":
	p = argparse.ArgumentParser(description='teamcity init')
	p.add_argument('--promote', default=False)
	p.add_argument('--match', default="v*")

	args = p.parse_args()

	buildtag = '0.0.0'
	branch = subprocess.check_output(['git', 'rev-parse', '--abbrev-ref', 'HEAD']).strip()
	desc = subprocess.check_output(['git', 'describe', '--match', args.match]).strip()

	if branch == 'master' or branch.startswith('prod-'):
		match = re.match(r'v(\d+)\.(\d+)\.(\d+).*', desc)
		if match:
			buildtag = '%s.%s.%d' % (match.group(1), match.group(2), int(match.group(3)) + 1)

	print("##teamcity[setParameter name='BuildTag' value='%s']" % buildtag)
	print("##teamcity[buildNumber '%s']" % desc)

	if p.promote and buildtag != '0.0.0':
		subprocess.check_call([ 'git', 'tag', '-a', '-m', 'Tagging build', 'v%s' % buildtag ])
		subprocess.check_call([ 'git', 'push', '--tags' ])
