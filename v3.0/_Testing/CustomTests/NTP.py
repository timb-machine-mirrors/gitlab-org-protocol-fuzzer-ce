#!/usr/bin/env python

import os
from subprocess import Popen, PIPE

def setup(ctx):
    if "osx" in get_platform():
        ctx.null = open('/dev/null', 'r+')
        ctx.disabler = Popen(['systemsetup', '-setusingnetworktime', 'off'],
                           stdin=ctx.null, stdout=ctx.null, stderr=ctx.null)
        ctx.disabler.wait()
        

def teardown(ctx):
    pass

test(name="NTP",
     setup=setup,
     teardown=teardown)
