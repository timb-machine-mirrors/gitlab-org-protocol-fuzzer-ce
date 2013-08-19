#!/usr/bin/env python
from os import system

ftp_start = {"linux": '/etc/init.d/vsftpd start',
             "win": '
                 
'/etc/init.d/vsftpd stop'


def setup(ctx):
    system(ftp_start[platform])


def teardown(ctx):
    system(system(ftp_stop[platform])

test(name="ftp",
     setup=setup,
     teardown=teardown)

test(name="ftp",
     test="Server")

