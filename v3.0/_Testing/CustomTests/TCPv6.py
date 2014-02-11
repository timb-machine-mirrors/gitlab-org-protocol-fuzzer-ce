#!/usr/bin/env python
import os
from subprocess import Popen, PIPE
from random import randint


def setup(ctx):
    null = open('/dev/null', 'r+')
    os.system('ip6tables -A OUTPUT -p tcp -m tcp --tcp-flags RST RST -j DROP')
    ctx.testip = "::%d" % randint(2,9)
    os.system('ip addr add %s dev lo'  % ctx.testip)
    port = randint(1024,65535)
    ctx.socat_proc = Popen(['socat',
        'tcp6-l:%d,fork,reuseaddr,bind=[::1]' % port,
                            'STDOUT'],
                           stdin=null, stdout=null, stderr=null)
    ctx.update_defines(TargetPort=port,
                       TargetIPv6="::1",
                       SourceIPv6=ctx.testip)


def teardown(ctx):
    ctx.socat_proc.kill()
    os.system('ip6tables -D OUTPUT 1')
    os.system("ip addr del %s dev lo" % ctx.testip)


test(name="TCPv6",
     platform="linux",
     setup=setup,
     teardown=teardown)
