#!/usr/bin/env python

from zlib import crc32 
import time

import code

import clr
clr.AddReferenceByPartialName('Peach.Core')

import Peach.Core

def init_seq(ctx):
    # print "init running"
    seed = (int(ctx.parent.parent.parent.context.test.strategy.Iteration) + int(ctx.parent.parent.parent.context.test.strategy.Seed))
    # print "making CRC"
    seq = crc32(str(seed)) % (2**32)
    sport = seq % (65535 - 1024) + 1024 
    # print "Setting value" 
    ctx.dataModel.find('SequenceNumber').DefaultValue = Peach.Core.Variant(seq)
    ctx.dataModel.find('SrcPort').DefaultValue = Peach.Core.Variant(sport)
    ctx.dataModel.Value #this shouldn't need to be called in future
    # print "Storing value"
    try:
        set_to_store(ctx, SequenceNumber=seq, SrcPort=sport)
    except Exception, e:
        print e
        raise e


def set_next_seq(ctx):
    ret = set_to_store(ctx, NextSequenceNumber=int(ctx.dataModel.find('SequenceNumber').InternalValue.ToString())+(len(ctx.dataModel.find('TcpPayload').Value.Value) or 1))
    #print "NextSeq == ", get_store_val(ctx, "NextSequenceNumber")
    return ret 


def sync_from_store(ctx):
    ctx.dataModel.find('SrcPort').DefaultValue = Peach.Core.Variant(get_store_val(ctx, "SrcPort"))
    if bool(int(ctx.dataModel.find('ACK').InternalValue)):
        set_default_from_store(ctx, "AcknowledgmentNumber")
    ctx.dataModel.find("SequenceNumber").DefaultValue = Peach.Core.Variant(get_store_val(ctx, "NextSequenceNumber"))
    ctx.dataModel.Value


def store_next_acknum(ctx):
    ret = set_to_store(ctx, AcknowledgmentNumber=int(ctx.dataModel.find('SequenceNumber').InternalValue.ToString())+(len(ctx.dataModel.find('TcpPayload').InternalValue.ToString()) or 1))
    #print "AckNum == ", get_store_val(ctx, "AcknowledgmentNumber")
    return ret
                        

def set_to_store(ctx, **kwarg):
    #print "setting value in store"
    for k,v in kwarg.iteritems():
        #print "setting value with key %s to %s" % (k,hex(v))
        ctx.parent.parent.parent.context.iterationStateStore[k] = v
    return True


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


def inc_stored_val(ctx, name, amount=1):
    ctx.parent.parent.parent.context.iterationStateStore[name] += amount

def inc_stored_vals(ctx, *args):
    for arg in args:
        inc_stored_val(ctx, arg)


def chk_if_ack_for_me(ctx):
    #print "AckNum ", int(ctx.parent.actions[0].dataModel.find('AcknowledgmentNumber').InternalValue.ToString())
    #print "Expected AckNum ", get_store_val(ctx, 'NextSequenceNumber')
    ret = int(ctx.parent.actions[0].dataModel.find('AcknowledgmentNumber').InternalValue.ToString()) == (get_store_val(ctx, 'NextSequenceNumber')) and bool(int(ctx.parent.actions[0].dataModel.find('ACK').InternalValue))
    return ret


def set_timestamp(ctx):
    ctx.dataModel.find('TimestampValue').DefaultValue = Peach.Core.Variant(get_cur_timestamp())
    ctx.dataModel.Value
    return True
    

def get_cur_timestamp():
    return int(time.mktime(time.gmtime()))

