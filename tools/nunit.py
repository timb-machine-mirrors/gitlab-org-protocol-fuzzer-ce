#!/usr/bin/env python

import os
import sys
import signal
import tempfile
import argparse
import threading
import subprocess
import xml.etree.ElementTree as ET

bindir = os.path.dirname(os.path.realpath(__file__))
nunit = os.path.join(bindir, 'nunit-console.exe')
trampoline = os.path.join(bindir, 'trampoline.py')

def win32_kill(pid):
	subprocess.call([
		'taskkill',
		'/t',               # Tree (including children)
		'/f',               # Force 
		'/pid', str(pid),
	])

def unix_kill(pid):
	os.killpg(pid, signal.SIGKILL)

def kill(pid):
	if sys.platform == 'win32':
		win32_kill(pid)
	else:
		unix_kill(pid)

def dotnet(cmd, newpg=True):
	if sys.platform != 'win32':
		cmd.insert(0, 'mono')
		if newpg:
			cmd.insert(0, trampoline)
			cmd.insert(0, 'python')
	return cmd

def run_nunit(args, asm, test, outdir):
	result = '.'.join([
		os.path.splitext(args.result)[0],
		test.replace('<', '_').replace('>', '_'),
		'xml',
	])

	cmd = dotnet([
		nunit,
		'--labels=All',
		'--include=%s' % args.include,
		'--result=%s' % result,
		asm,
		'--test=%s' % test,
	])

	print ' '.join(cmd)
	sys.stdout.flush()

	proc = subprocess.Popen(cmd, stdout=subprocess.PIPE)

	def on_inactive():
		print '%s: Timeout due to inactivity' % test
		sys.stdout.flush()
		kill(proc.pid)

	def on_abort(signum, frame):
		print '%s: SIGTERM' % test
		sys.stdout.flush()
		kill(proc.pid)
		sys.exit(signal.SIGTERM)

	try:
		old_handler = signal.signal(signal.SIGTERM, on_abort)

		while proc.poll() is None:
			timer = threading.Timer(args.timeout, on_inactive)
			timer.start()
			line = proc.stdout.readline()
			timer.cancel()

			with open(os.path.join(outdir, '%s.txt' % test), 'a') as fout:
				sys.stdout.write(line)
				sys.stdout.flush()
				fout.write(line)
	except KeyboardInterrupt:
		print 'kill(%d)' % proc.pid
		sys.stdout.flush()
		kill(proc.pid)
		raise
	finally:
		try:
			signal.signal(signal.SIGTERM, old_handler)
		except Exception, e:
			print 'Could not restore SIGTERM handler: %s' % e
			sys.stdout.flush()

		try:
			timer.cancel()
		except Exception, e:
			print 'Could not cancel timer: %s' % e
			sys.stdout.flush()

		try:
			print 'Wait for process to finish...'
			sys.stdout.flush()

			stdout = proc.stdout.read()
			sys.stdout.write(stdout)
			sys.stdout.flush()

			rc = proc.wait()
			print 'Result: %s' % rc
			print ''
			print ''
			sys.stdout.flush()
		except Exception, e:
			print e
			sys.stdout.flush()

def main():
	if sys.platform == 'win32':
		import win32job
		import win32process

		hJob = win32job.CreateJobObject(None, "")
		info = win32job.QueryInformationJobObject(hJob, win32job.JobObjectExtendedLimitInformation)
		info['BasicLimitInformation']['LimitFlags'] = win32job.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
		win32job.SetInformationJobObject(hJob, win32job.JobObjectExtendedLimitInformation, info)

		win32job.AssignProcessToJobObject(hJob, win32process.GetCurrentProcess())

	p = argparse.ArgumentParser(description='nunit-runner')
	p.add_argument('--timeout', type=int, default=600)
	p.add_argument('--result', required=True)
	p.add_argument('--include', required=True)
	p.add_argument('input', nargs=argparse.REMAINDER)
	args = p.parse_args()

	tmpdir = os.getenv('TMPDIR')
	if tmpdir is not None and not os.path.exists(tmpdir):
		print 'Creating $TMDIR: %s' % tmpdir
		os.makedirs(tmpdir)

	outdir = os.path.dirname(args.result)
	if not os.path.exists(outdir):
		print 'Creating output dir: %s' % outdir
		os.makedirs(outdir)

	with tempfile.NamedTemporaryFile() as tmp:
		explore = dotnet([
			nunit,
			'--explore=%s' % tmp.name,
			'--include=%s' % args.include,
		], newpg=False) + args.input

	subprocess.check_call(explore)

	xml_root = ET.parse(tmp.name).getroot()

	for asm in xml_root.findall('test-suite[@type="Assembly"]'):
		path = asm.attrib['name']

		fixtures = []

		for fixture in asm.findall('.//test-suite[@type="TestFixture"]'):
			fullname = fixture.attrib['fullname']
			fixtures.append(fullname)

		for fixture in sorted(fixtures):
			run_nunit(args, path, fixture, outdir)

if __name__ == "__main__":
	try:
		main()
	except KeyboardInterrupt:
		print 'KeyboardInterrupt'
