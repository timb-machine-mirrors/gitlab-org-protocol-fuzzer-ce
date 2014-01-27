#!/usr/bin/env python

test(name="SNMP",
    test="Default")

test(name="SNMP",
    test="Server",
    platform="linux",
    setup=setup)

test(name="SNMP",
    test="Server",
    platform="windows")

test(name="SNMP",
    test="Server",
    platform="osx")

def setup(ctx):
    os.system("sudo apt-get install snmp -y")
