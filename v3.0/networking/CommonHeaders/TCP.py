#!/usr/bin/env python

from zlib import crc32 

import clr
clr.AddReferenceByPartialName('Peach.Core')

import Peach.Core

# import code
# code.InteractiveConsole(locals=globals()).interact()

#add tracking for window

def init_seq(ctx):
    # print "init running"
    seed = (int(ctx.parent.parent.parent.context.test.strategy.Iteration) + int(ctx.parent.parent.parent.context.test.strategy.Seed)) 
    # print "making CRC"
    seq = crc32(str(seed)) % (2**32)
    # print "Setting value" 
    ctx.dataModel.find('SequenceNumber').DefaultValue = Peach.Core.Variant(seq)
    ctx.dataModel.Value #this shouldn't need to be called in future
    # print "Storing value"
    try:
        set_to_store(ctx, SequenceNumber=seq)
    except Exception, e:
        print e
        raise e
        # code.InteractiveConsole(locals=globals()).interact()
    # print "verifying value is stored: %s" % get_store_val(ctx, 'SequenceNumber')


def set_to_store(ctx, **kwarg):
    print "setting value in store"
    for k,v in kwarg.iteritems():
        print "setting value with key %s to %s" % (k,hex(v))
        ctx.parent.parent.parent.context.iterationStateStore[k] = v


def get_store_val(ctx, name):
    if name in ctx.parent.parent.parent.context.iterationStateStore:
        return ctx.parent.parent.parent.context.iterationStateStore[name]
    else:
        return False


def set_defaults_from_store(ctx, *args):
    for arg in args:
        set_default_from_store(ctx, arg)


def set_default_from_store(ctx, name):
    ctx.dataModel.find(name).DefaultValue = Peach.Core.Variant(get_store_val(ctx, name))
    ctx.dataModel.Value #this shouldn't need to be called in future


def inc_stored_val(ctx, name):
    ctx.parent.parent.parent.context.iterationStateStore[name] += 1

def inc_stored_vals(ctx, *args):
    for arg in args:
        inc_stored_val(ctx, arg)

def done(ctx):
    if "done" in ctx.parent.parent.parent.context.iterationStateStore:
        return ctx.parent.parent.parent.context.iterationStateStore["done"]
    return False

def set_state(ctx, state):
    print "setting state to %s" % state
    ctx.parent.parent.parent.context.iterationStateStore["state"] = state
    return True

def get_state(ctx):
    """http://www.ietf.org/rfc/rfc793.txt page 23"""
    if "state" in ctx.parent.parent.parent.context.iterationStateStore:
        return ctx.parent.parent.parent.context.iterationStateStore["state"]
    else:
        return "unknown"

def chk_if_ack_for_me(ctx):
    return int(ctx.parent.actions[0].dataModel.find('AcknowledgmentNumber').InternalValue.ToString()) == get_store_val(ctx, 'SequenceNumber') and bool(int(ctx.parent.actions[0].dataModel.find('ACK').InternalValue))

# TCP.get_store_val(self, 'AcknowledgmentNumber')
# self.dataModel.find('AcknowledgmentNumber').DefaultValue.ToString()
