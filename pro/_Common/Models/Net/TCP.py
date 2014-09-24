#!/usr/bin/env python

'''
PEACH PIT COPYRIGHT NOTICE AND LEGAL DISCLAIMER

COPYRIGHT
Copyright (c) 2011-2014 2014 Deja vu Security, LLC.
All rights reserved.

Deja vu Security is the sole proprietary owner of Peach Pits and related
definition files and documentation.

User may only use, copy, or modify Peach Pits and related definition files and
documentation for internal business purposes only, provided that this entire
notice and following disclaimer appear in all copies or modifications, and
subject to the following conditions:

(1) User maintains a current subscription to the Peach Pit library.
(2) User's use is restricted to commercially licensed version of Peach Fuzzer
    only. Running Peach Pits with the Peach Fuzzer Community edition or any
    other solution is strictly prohibited.
(3) The sale, transfer, or distribution of Peach Pits and related definition
    files and documentation, in any form, is not permitted, without Deja vu
    Security's express permission.

Legal Disclaimer
PEACH PITS AND RELATED DEFINTIION FILES AND DOCUMENTATION ARE PROVIDED "AS IS",
DEJA VU SECURITY DISCLAIMS ALL WARRANTIES, EXPRESS OR IMPLIED, INCLUDING, BUT
NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
PARTICULAR PURPOSE. DEJA VU SECURITY HAS NO OBLIGATION TO PROVIDE MAINTENANCE,
SUPPORT, UPDATES, ENHANCEMENTS, OR MODIFICATIONS.

IN NO EVENT SHALL DEJA VU SECURITY BE LIABLE TO ANY PARTY FOR ANY DIRECT,
INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES (INCLUDING LOSS OF USE,
DATA, OR PROFITS), ARISING OUT OF ANY USE OF PEACH PITS AND RELATED
DOCUMENTATION, EVEN IF DEJA VU SECURITY HAS BEEN ADVISED OF THE POSSIBILITY OF
SUCH DAMAGE.
'''

from zlib import crc32 
from code import InteractiveConsole
import time

import clr
clr.AddReferenceByPartialName('Peach.Core')

import Peach.Core

'''
This code is used with the TCPv4 and TCPv6 pits.
'''

def init_seq(context, action):
    '''
    Initialize models for a new iteration.
    '''

    # Very our source port and starting sequence number
    # through a simple method using the current iteration
    # number. This will help keep the stack from getting
    # confused about the continued weird packets
    # coming in.

    seed = (int(context.test.strategy.Iteration) + int(context.test.strategy.Seed))
    
    seq = crc32(str(seed)) % (2**32)
    sport = seq % (65535 - 1024) + 1024 
    
    seqNumber = action.dataModel.find('SequenceNumber')
    sourcePort = action.dataModel.find('SrcPort')
    if seqNumber and sourcePort:
        seqNumber.DefaultValue = Peach.Core.Variant(seq)
        sourcePort.DefaultValue = Peach.Core.Variant(sport)
    
    try:
        set_to_store(action, SequenceNumber=seq, SrcPort=sport) #can sport just come from the data model??
    except Exception, e:
        print e
        raise e

def enableZeroRead(context):
    '''
    Set publisher to 0 second timeout and no exception on zero read.
    '''
    context.test.publishers[0].setProperty("Timeout", Peach.Core.Variant(0))
    context.test.publishers[0].setProperty("NoReadException", Peach.Core.Variant('true'))

def disableZeroRead(context):
    '''
    Re-enable timeout and read exceptions
    '''
    context.test.publishers[0].setProperty("Timeout", Peach.Core.Variant(3000))
    context.test.publishers[0].setProperty("NoReadException", Peach.Core.Variant('false'))

def set_next_seq(ctx):
    '''
    Update our tracking of NextSequenceNumber.

    Will find SequenceNumber from current packet and increment it by payload size or 1.

    Returns False if information not found, else True
    '''
    env = globals()
    env.update(locals())
    payload_size = 1

    tcpPayload = ctx.dataModel.find('TcpPayload')
    if tcpPayload:
        payload_size = tcpPayload.Value.Length

    seqNumber = ctx.dataModel.find('SequenceNumber')
    if seqNumber:
        ret = set_to_store(ctx, NextSequenceNumber=int(seqNumber.InternalValue.ToString())+(payload_size or 1))
    else:
        ret = False

    return ret


def sync_from_store(ctx):
    '''
    Set Ack and Seq numbers from our store.
    '''
    srcPort = ctx.dataModel.find('SrcPort')
    if srcPort:
        srcPort.DefaultValue = Peach.Core.Variant(get_store_uint16(ctx, "SrcPort"))

    ack = ctx.dataModel.find('ACK')
    if ack and bool(int(ack.InternalValue)):
        set_default_uint32_from_store(ctx, "AcknowledgmentNumber")

    seqNumber = ctx.dataModel.find("SequenceNumber")
    if seqNumber:
        seqNumber.DefaultValue = Peach.Core.Variant(get_store_uint32(ctx, "NextSequenceNumber"))

def sync_from_lastack(ctx):
    '''
    Set Seq to our last ack value
    '''

    seqNumber = ctx.dataModel.find('SequenceNumber')
    if seqNumber:
        seqNumber.DefaultValue = Peach.Core.Variant(get_store_uint32(ctx, "LastAcknowledgmentNumber"))

    return True

def store_next_acknum(ctx):
    '''
    Set ACK to be current Seq number + payload size or 1.
    '''
    print "store_next_acknum"

    payload_size = 1

    tcpPayload = ctx.dataModel.find('TcpPayload')
    if tcpPayload:
        payload_size = tcpPayload.Value.Length
        print "store_next_acknum: payload_size:", payload_size
    else:
        print "store_next_acknum: can't find TcpPayload!"

    seqNumber = ctx.dataModel.find('SequenceNumber')
    if seqNumber:
        ret = set_to_store(ctx, AcknowledgmentNumber=int(seqNumber.InternalValue.ToString())+(payload_size or 1))
    else:
        ret = False

    # For ACK packet we need the last ack
    ackNumber = ctx.dataModel.find("AcknowledgmentNumber")
    if ackNumber:
        ret = set_to_store(ctx, LastAcknowledgmentNumber=int(ackNumber.InternalValue.ToString()))

    #ret = ret % (2**32)

    return ret
                        

def set_to_store(ctx, **kwarg):
    '''
    Internal method to store values in state bag
    '''
    #print "setting value in store"
    for k,v in kwarg.iteritems():
        #print "setting value with key %s to %s" % (k,hex(v))
        ctx.parent.parent.parent.context.iterationStateStore[k] = v
    return True


def get_store_uint(ctx, name, size):
    '''
    Internal method to get value from state store.
    '''
    # this is always returning a 32 bit int
    if name in ctx.parent.parent.parent.context.iterationStateStore:
        return ctx.parent.parent.parent.context.iterationStateStore[name] % (2**size)
    else:
        return (2**size) - 1 


def get_store_uint16(ctx, name):
    return get_store_uint(ctx, name, 16)


def get_store_uint32(ctx, name):
    return get_store_uint(ctx, name, 32)


def set_default_uint32_from_store(ctx, name):
    if ctx.dataModel.find(name):
        ctx.dataModel.find(name).DefaultValue = Peach.Core.Variant(get_store_uint32(ctx, name))
    else:
        return False
    return True


def get_if_ack_for_me(inputModel):
    '''
    Verify the ack number matches our stored next sequence number. Also verify the ACK flag was set.
    '''
    env = globals()
    env.update(locals())
    #InteractiveConsole(locals=env).interact()

    ackNum = inputModel.find('AcknowledgmentNumber')
    controlBits = inputModel.find('ControlBits')
    if ackNum and int(ackNum.InternalValue.ToString()) == (get_store_uint32(ctx, 'NextSequenceNumber')):
        if bool(int(controlBits['ACK'].InternalValue)) and not bool(int(controlBits['PSH'].InternalValue)):
            return True

    return False

def is_fin_ack(ctx):
    '''
    Check if this is a fin-ack packet.
    '''

    controlBits = ctx.find('ControlBits')
    if controlBits and bool(int(controlBits['ACK'].InternalValue)) and bool(int(controlBits['FIN'].InternalValue)):
        return True
    
    return False


def set_timestamp(ctx):
    if ctx.dataModel.find('TimestampValue'):
        ctx.dataModel.find('TimestampValue').DefaultValue = Peach.Core.Variant(get_cur_timestamp())
        return True
    else:
        return False
    

def get_cur_timestamp():
    return int(time.mktime(time.gmtime()))

# end
