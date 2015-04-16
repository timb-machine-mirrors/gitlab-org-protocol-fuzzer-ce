#!/usr/bin/env python

import re
import subprocess

buildtag = '0.0.0'
branch = subprocess.check_output(['git', 'rev-parse', '--abbrev-ref', 'HEAD']).strip()
desc = subprocess.check_output(['git', 'describe']).strip()

if branch == 'master' or branch.startswith('prod-'):
	match = re.match(r'v(\d+)\.(\d+)\.(\d+).*', desc)
	if match:
		buildtag = '%s.%s.%d' % (match.group(1), match.group(2), int(match.group(3)) + 1)

print("##teamcity[setParameter name='BuildTag' value='%s']" % buildtag)
print("##teamcity[buildNumber '%s']" % desc)
