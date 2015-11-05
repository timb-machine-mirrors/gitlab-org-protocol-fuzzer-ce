#!/usr/bin/env python

import os
import sys
import tempfile
import argparse
import threading
import subprocess
import xml.etree.ElementTree as ET

bindir = os.path.dirname(os.path.realpath(__file__))
nunit = os.path.join(bindir, 'nunit-console.exe')

def dotnet(cmd):
	if sys.platform != 'win32':
		cmd.insert(0, 'mono')
	return cmd

def run_nunit(args, asm, name, test):
	result = '.'.join([
		os.path.splitext(args.result)[0],
		name,
		'xml',
	])

	cmd = dotnet([
		nunit,
		'--labels=All',
		'--include=%s' % args.include,
		'--timeout=%d' % (args.timeout * 1000),
		'--result=%s' % result,
		asm,
		'--test=%s' % test,
	])

	print ' '.join(cmd)
	proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)

	def on_inactive():
		print '%s: Timeout due to inactivity' % name
		proc.kill()

	while proc.poll() is None:
		timer = threading.Timer(args.timeout + 10, on_inactive)
		timer.start()
		line = proc.stdout.readline()
		timer.cancel()

		# print line.rstrip()
		with open(os.path.join(args.outdir, '%s.txt' % name), 'a') as fout:
			fout.write(line)

def main():
	p = argparse.ArgumentParser(description='nunit-runner')
	p.add_argument('--timeout', type=int, default=180)
	p.add_argument('--result')
	p.add_argument('--outdir')
	p.add_argument('--include')
	p.add_argument('input', nargs=argparse.REMAINDER)
	args = p.parse_args()

	tmpdir = os.getenv('TMPDIR')
	if tmpdir is not None and not os.path.exists(tmpdir):
		print 'Creating $TMDIR: %s' % tmpdir
		os.makedirs(tmpdir)

	if args.outdir is not None and not os.path.exists(args.outdir):
		print 'Creating output dir: %s' % args.outdir
		os.makedirs(args.outdir)

	with tempfile.NamedTemporaryFile() as tmp:
		explore = dotnet([
			nunit,
			'--explore=%s' % tmp.name,
			'--include=%s' % args.include,
		]) + args.input

	subprocess.check_call(explore)

	xml_root = ET.parse(tmp.name).getroot()

	for asm in xml_root.findall('test-suite[@type="Assembly"]'):
		path = asm.attrib['name']

		tests = set()
		extras = []

		for fixture in asm.findall('.//test-suite[@type="TestFixture"]'):
			fullname = fixture.attrib['fullname']
			group = fullname[:fullname.rindex('.')]
			found = asm.find('.//test-suite[@fullname="%s"]' % group)
			if found is None:
				extras.append(fullname)
			else:
				tests.add(group)

		for test in sorted(tests):
			run_nunit(args, path, test, test)

		if len(extras):
			name = os.path.splitext(os.path.basename(path))[0]
			run_nunit(args, path, name, ','.join(extras))

if __name__ == "__main__":
	try:
		main()
	except KeyboardInterrupt:
		pass
