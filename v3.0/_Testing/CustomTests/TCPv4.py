#!/usr/bin/env python
import os
from subprocess import Popen, PIPE
from random import randint


def setup(ctx):
    assert os.getuid() == 0, "must be root to run this"
    null = open('/dev/null', 'r+')
    os.system('iptables -A OUTPUT -p tcp -m tcp --tcp-flags RST RST -j DROP')
    port = randint(1024,65535)
    ctx.socat_proc = Popen(['socat',
                            'tcp4-l:%d,fork,reuseaddr' % port,
                            'READLINE'],
                           stdin=null, stdout=null, stderr=null)
    ctx.update_defines(TargetPort=port)


def teardown(ctx):
    os.system('iptables -D OUTPUT 1')
    ctx.socat_proc.kill()


test(name="TCPv4",
     setup=setup,
     teardown=teardown)

define(name="TCPv4",
       SourceIPv4="127.0.0.1")
