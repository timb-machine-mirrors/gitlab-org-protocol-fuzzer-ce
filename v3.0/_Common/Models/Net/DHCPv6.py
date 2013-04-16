#!/usr/bin/env python
from socket import inet_ntop, AF_INET6

# import code #for debugging
#use code.InteractiveConsole(locals=globals()).interact()

# def populate_reqd_opts(ctx):
#     code.InteractiveConsole(locals=globals()).interact()


def set_to_store(ctx, **kwarg):
    print "setting values in store", kwarg
    for k,v in kwarg.iteritems():
        # print k
        # print v
        # print "setting value with key %s to %s" % (k, hex(v))
        ctx.parent.parent.parent.context.iterationStateStore[k] = v
    return True


def get_store_val(ctx, name):
    if name in ctx.parent.parent.parent.context.iterationStateStore:
        return ctx.parent.parent.parent.context.iterationStateStore[name]
    else:
        return False

def inc_and_ret_stored_val(ctx, name):
    val = (get_store_val(ctx, name) or 0) + 1
    set_to_store(ctx, **{name:val})
    return val


def too_many_tries(ctx, maxtries):
    count = inc_and_ret_stored_val(ctx, 'count')
    # print "count == ", count
    # print "maxtries == ", maxtries
    return count >= maxtries


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
