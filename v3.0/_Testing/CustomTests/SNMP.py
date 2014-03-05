#!/usr/bin/env python

def linux_setup(ctx):
    os.system("sudo apt-get install snmp -y")

def teardown(ctx):
    pass

test(name="SNMP",
    test="Default")

test(name="SNMP",
    test="Server",
    platform="linux",
    setup=linux_setup,
    teardown=teardown)

test(name="SNMP",
    test="Server",
    platform="win")

test(name="SNMP",
    test="Server",
    platform="osx")

