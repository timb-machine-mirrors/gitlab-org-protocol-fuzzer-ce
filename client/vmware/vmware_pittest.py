#!/usr/bin/python
#
# Copyright (c) Deja vu Security
# Michael Eddington <mike@dejavusecurity.com>
#

#
# Run through each of the VMware fuzzers.
#
# NOTE: Not currently testing IPSEC, VXLAN, VLAN
#

import pexpect, os, sys, glob, time

# timeout value, 10 min
TIMEOUT = 1000 * 60 * 10

pits = [
	'ARP',
	'CDP',
	'Ethernet',
	'ICMPv4',
	'ICMPv6',
	'IGMP',
	'IPv4',
	'IPv6',
	'LACP',
	'LLDP',
	'MLD',
	'TCPv4',
	'TCPv6',
	'UDPv4',
	'UDPv6',
	#'DHCPv6', # has to be tested seperately with a different config
	]

fd = open("vmware_pittest.txt", "ab+")

fd.write('===============================\n')
print 'Starting test run\n'
fd.write('Starting test run\n\n')

passed = 1

for pit in pits:
	for test in ["test", "test_mtufuzz"]:
		print "\033[0m",
		# Run the pit
		print 'Testing: %s with test: %s' % (pit, test)
		fd.write('Testing: %s with test: %s\n' % (pit, test))
		child = pexpect.spawn(os.path.join(os.getcwd(),test) + " " + pit, maxread=20000, timeout=TIMEOUT)
		print "\033[31m",
		if child.expect([pexpect.EOF, pexpect.TIMEOUT, "Performing iteration"]) != 2:
			# Failed
			print '  Failed to start: %s' % pit
			fd.write('  Failed: %s\n' % pit)
			continue

		if child.expect([pexpect.EOF, pexpect.TIMEOUT, 'Performing iteration']) != 2:
			# Failed
			print '  Failed before entering first iteration: %s' % pit
			fd.write('  Failed: %s\n' % pit)
			continue

		if child.expect([pexpect.EOF, pexpect.TIMEOUT, 'Performing iteration']) != 2:
			# Failed
			print '  Failed before entering second iteration: %s' % pit
			fd.write('  Failed: %s\n' % pit)
			continue

		# if child.expect([pexpect.EOF, pexpect.TIMEOUT, 'Test .Default. finished.']) != 2:
		#	# Failed
		#	print '  Failed to finish second iteration: %s' % pit
		#	fd.write('  Failed: %s\n' % pit)
		#	continue
		
		print "\033[0m",
		if child.isalive():
			print '  Process hung and had to be killed, other tests passed: %s' % pit
			fd.write('  Had to be killed: %s\n' % pit)
			child.kill(9)
			time.sleep(.5)
			while child.isalive():
				print "child still alive, trying to kill child again"
				time.sleep(.5)
			child = None
			# Test passed
			passed += 1
print "\033[0m",
print "\nFinished test run! %d of %d passed.\n\a" % (passed, len(pits))
fd.write("\nFinished test run!\n\n")
fd.close()
