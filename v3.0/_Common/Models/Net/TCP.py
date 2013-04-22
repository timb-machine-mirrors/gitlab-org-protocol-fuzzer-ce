#!/usr/bin/env python

from zlib import crc32 
import time

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
    if ctx.dataModel.find('SequenceNumber') and ctx.dataModel.find('SrcPort'):
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
    payload_size = 1
    if ctx.dataModel.find('TcpPayload'):
        payload_size = len(ctx.dataModel.find('TcpPayload').Value.Value)
    if ctx.dataModel.find('SequenceNumber'):
        ret = set_to_store(ctx, NextSequenceNumber=int(ctx.dataModel.find('SequenceNumber').InternalValue.ToString())+(payload_size or 1))
    else:
        ret = 0
    ret = ret % (2**32)
    return ret


def sync_from_store(ctx):
    if ctx.dataModel.find('SrcPort'):
        ctx.dataModel.find('SrcPort').DefaultValue = Peach.Core.Variant(get_store_uint16(ctx, "SrcPort"))
    if ctx.dataModel.find('ACK') and bool(int(ctx.dataModel.find('ACK').InternalValue)):
        set_default_uint32_from_store(ctx, "AcknowledgmentNumber")
    if ctx.dataModel.find("SequenceNumber"):
        ctx.dataModel.find("SequenceNumber").DefaultValue = Peach.Core.Variant(get_store_uint32(ctx, "NextSequenceNumber"))
    ctx.dataModel.Value


def store_next_acknum(ctx):
    payload_size = 1
    if ctx.dataModel.find('TcpPayload'):
        payload_size = len(ctx.dataModel.find('TcpPayload').Value.Value)
    if ctx.dataModel.find('SequenceNumber'):
        ret = set_to_store(ctx, AcknowledgmentNumber=int(ctx.dataModel.find('SequenceNumber').InternalValue.ToString())+(payload_size or 1))
    else:
        ret = 0
    ret = ret % (2**32)
    return ret
                        

def set_to_store(ctx, **kwarg):
    #print "setting value in store"
    for k,v in kwarg.iteritems():
        #print "setting value with key %s to %s" % (k,hex(v))
        ctx.parent.parent.parent.context.iterationStateStore[k] = v
    return True



def get_store_int(ctx, name, size):
    # this is always returning a 32 bit int
    if name in ctx.parent.parent.parent.context.iterationStateStore:
        return ctx.parent.parent.parent.context.iterationStateStore[name]
    else:
        return (2**size) - 1 


def get_store_uint16(ctx, name):
    return get_store_int(ctx, name, 16)


def get_store_uint32(ctx, name):
    return get_store_int(ctx, name, 32)


def set_default_from_store(ctx, name):
    if ctx.dataModel.find(name):
        ctx.dataModel.find(name).DefaultValue = Peach.Core.Variant(get_store_uint32(ctx, name))
    else:
        return False
    ctx.dataModel.Value #this shouldn't need to be called in future
    return True


# def inc_stored_uint32(ctx, name, amount=1):
#     val = ctx.parent.parent.parent.context.iterationStateStore[name]
#     ctx.parent.parent.parent.context.iterationStateStore[name] = (val + amount) % (2**32)
    

# def inc_stored_uint32s(ctx, *args):
#     for arg in args:
#         inc_stored_uint32(ctx, arg)


def get_if_ack_for_me(ctx):
    if ctx.parent.actions[0].dataModel.find('AcknowledgmentNumber'):
        ret = int(ctx.parent.actions[0].dataModel.find('AcknowledgmentNumber').InternalValue.ToString()) == (get_store_uint32(ctx, 'NextSequenceNumber')) and bool(int(ctx.parent.actions[0].dataModel.find('ACK').InternalValue))
    else:
        ret = 0
    ret = ret % (2**32)
    return ret


def set_timestamp(ctx):
    if ctx.dataModel.find('TimestampValue'):
        ctx.dataModel.find('TimestampValue').DefaultValue = Peach.Core.Variant(get_cur_timestamp())
        ctx.dataModel.Value
        return True
    else:
        return False
    

def get_cur_timestamp():
    return int(time.mktime(time.gmtime()))
