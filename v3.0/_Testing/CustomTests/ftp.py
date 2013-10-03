#!/usr/bin/env python
from os import system
from sys import platform

ftp_start = {"linux2": 'service vsftpd start',
             "win": ''} #needs to be finished

ftp_stop = {"linux2": 'service vsftpd stop',
             "win": ''} #needs to be finished



def setup(ctx):
    system(ftp_start[platform])


def teardown(ctx):
    system(ftp_stop[platform])


test(name="ftp",
     setup=setup,
     teardown=teardown)

test(name="ftp",
     test="Server")

define(name="ftp",
       LinuxFTPCmd='_Testing/CustomTests/ftp.sh')
