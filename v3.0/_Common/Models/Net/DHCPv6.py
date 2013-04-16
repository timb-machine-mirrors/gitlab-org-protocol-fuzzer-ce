#!/usr/bin/env python
from socket import inet_ntop, AF_INET6

import code #for debugging
#use code.InteractiveConsole(locals=globals()).interact()

import clr
clr.AddReferenceByPartialName('Peach.Core')

import Peach.Core

# def populate_reqd_opts(ctx):
#     code.InteractiveConsole(locals=globals()).interact()


def is_solicit(ctx):
    return int(ctx.parent.actions[0].dataModel.find('MsgType').InternalValue) == 1
    
def is_request(ctx):
    return int(ctx.parent.actions[0].dataModel.find('MsgType').InternalValue) == 3

def not_from_target(ctx, target):
    last_recv_ip = str(ctx.parent.actions[1].dataModel.DefaultValue)
    last_recv_ip = inet_ntop(AF_INET6, last_recv_ip.replace(' ','').decode('hex'))
    print last_recv_ip
    print target
    print "these two are == %s" % (last_recv_ip == target )
    return not (last_recv_ip == target)
