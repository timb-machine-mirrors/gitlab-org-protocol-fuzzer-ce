#!/usr/bin/env python
import os
from subprocess import Popen, PIPE


def setup(ctx):
    if "linux" in get_platform():
        ctx.null = open('/dev/null', 'r+')
        ctx.x_proc = Popen(['Xvfb',':0'],
                           stdin=null, stdout=null, stderr=null)
        os.environ['DISPLAY'] = ':0'
        

def teardown(ctx):
    if "linux" in get_platform():
        ctx.socat_proc.kill()
        ctx.null.close()

test(name="BMP",
     setup=setup,
     teardown=teardown)

test(name="GIF",
     setup=setup,
     teardown=teardown)

test(name="Ico",
     setup=setup,
     teardown=teardown)

test(name="JPEG2000",
     setup=setup,
     teardown=teardown)

test(name="PNG",
     setup=setup,
     teardown=teardown)

test(name="jpg-jfif",
     setup=setup,
     teardown=teardown)

test(name="tiff",
     setup=setup,
     teardown=teardown)

