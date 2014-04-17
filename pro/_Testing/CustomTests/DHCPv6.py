#!/usr/bin/env python
import os
from subprocess import Popen, PIPE
from random import randint


def setup(ctx):
    null = open('/dev/null', 'r+')
    os.system('ip link add name veth0 type veth peer name veth1')
    os.system('ip link set veth0 up')
    os.system('ip link set veth1 up')
    sysinfo = Popen(['ip', 'addr', 'show', 'veth0'],
                    stdin=null, stdout=PIPE, stderr=PIPE)

    stdout, stderr = sysinfo.communicate()
    # now for some fradgile pointless magic...
    # this should probably get turned in to regex, we'll see how it
    # goes.
    stdout = stdout.split('\n')
    ifnum = stdout[0].split(':')[0]
    mac = stdout[1].split()[-3].replace(':','')
    source_ipv6 = stdout[2].split()[1].split('/')[0]
    prefix = source_ipv6.split('::')[0]
    lease = "908:7a38:6156:acce"
    sysinfo = Popen(['ip', 'addr', 'show', 'veth1'],
                    stdin=null, stdout=PIPE, stderr=PIPE)
    stdout, stderr = sysinfo.communicate()
    stdout = stdout.split('\n')
    target_ipv6 = stdout[2].split()[1].split('/')[0]
    null.close()    
    # abracadabra
    #  bracadabr
    #   racada
    #    acad
    #     ab
    ctx.update_defines(SourceMAC=mac,
                       TargetInterface='veth1', #just incase things change
                       SourceIPv6="%s%%%s" % (source_ipv6, ifnum),
                       TargetIPv6Lease="%s::%s" % (prefix,lease),
                       TargetIPv6=target_ipv6,
                       Interface=source_ipv6)

def teardown(ctx):
    os.system('ip link del veth0') #this automatically destroys the peer

test(name="DHCPv6",
     platform="linux",
     setup=setup,
     teardown=teardown)

