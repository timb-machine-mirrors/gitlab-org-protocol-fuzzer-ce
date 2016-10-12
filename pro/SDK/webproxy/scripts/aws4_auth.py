
'''
AWS v4 Signature Authentication Script
Copyright (C) 2016 Peach Fuzzer, LLC

Permission is hereby granted, free of charge, to any person obtaining 
a copy of this software and associated documentation files (the 
"Software"), to deal in the Software without restriction, including 
without limitation the rights to use, copy, modify, merge, publish, 
distribute, sublicense, and/or sell copies of the Software, and to 
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be 
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN 
ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
SOFTWARE.
'''

'''
This script module provides AWS v4 Signature authentication for
Peach Web.  This script can be configured via the Route 'Script'
parameter.
'''

from peach import webproxy
from hashlib import sha256

import hmac
import traceback
import sys
import code
import datetime
import hashlib

# Configuration

AWS_ACCESS_KEY_ID = 'EMEDC3WWOOGDMHTFUM2I'
AWS_SECRET_KEY = 'qd2zt2UbbAai5QiKh4S5/bcgkxobXl58Sm67fE/d'
AWS_REGION = 'us-east-1'
AWS_SERVICE = 's3'

# --------------------------------------------------------------
# --------------------------------------------------------------
# --------------------------------------------------------------

def aws4_auth(ctx, req, body):
    '''Add AWS4 auth to a proxy request.
    
    This is the event callback for EVENT_ACTION
    '''
    
    try:
        
        datetime_now = datetime.datetime.utcnow()
        datetime_iso = datetime_now.strftime('%Y%m%dT%H%M%SZ')
        datetime_stamp = datetime_now.strftime('%Y%m%d')

        HTTPMethod  = req.method
        CanonicalURI = aws_auth_uri(ctx, req, body)
        CanonicalQueryString = aws_auth_querystring(ctx, req, body)
        (CanonicalHeaders, SignedHeaders) = aws_auth_headers(ctx, req, body, datetime_iso)
        HashedPayload = aws_auth_payload(ctx, req, body)

        CanonicalRequest = ("%(HTTPMethod)s\n" +
            "%(CanonicalURI)s\n"+
            "%(CanonicalQueryString)s\n"+
            "%(CanonicalHeaders)s\n"+
            "%(SignedHeaders)s\n"+
            "%(HashedPayload)s") % {
                "HTTPMethod":HTTPMethod,
                "CanonicalURI":CanonicalURI,
                "CanonicalQueryString":CanonicalQueryString,
                "CanonicalHeaders":CanonicalHeaders,
                "SignedHeaders":SignedHeaders,
                "HashedPayload":HashedPayload
            }

        Scope = "%s/%s/%s/aws4_request" %(datetime_stamp, AWS_REGION, AWS_SERVICE)
        StringToSign = "AWS4-HMAC-SHA256\n" + \
            datetime_iso + "\n" + \
            Scope + "\n" + \
            hashlib.sha256(CanonicalRequest).hexdigest()

        DateKey = aws_sign("AWS4" + AWS_SECRET_KEY, datetime_stamp)
        DateRegionKey = aws_sign(DateKey, AWS_REGION)
        DateRegionServiceKey = aws_sign(DateRegionKey, AWS_SERVICE)
        SigningKey = aws_sign(DateRegionServiceKey, 'aws4_request')

        Signature = aws_sign_hex(SigningKey, StringToSign)

        Authorization = "AWS4-HMAC-SHA256 Credential=" + \
            AWS_ACCESS_KEY_ID + "/" + Scope + \
            ", SignedHeaders=" + SignedHeaders + \
            ", Signature=" + Signature 

        req.headers['Authorization'] = Authorization
    except:
        traceback.print_exc(file=sys.stdout)
        code.InteractiveConsole(locals()).interact()

# Register event callback
webproxy.register_event(webproxy.EVENT_ACTION, aws4_auth)

# --- Intnernal methods ---

def aws_sign(key, msg):
    '''HMACSHA256 - From Amazon Python Example
    '''
    return hmac.new(key, msg.encode('utf-8'), hashlib.sha256).digest()

def aws_sign_hex(key, msg):
    '''HMACSHA256 output hex - From Amazon Python Example
    '''
    return hmac.new(key, msg.encode('utf-8'), hashlib.sha256).hexdigest()

def aws_auth_payload(ctx, req, body):
    '''Hash payload
    '''
    
    h= sha256()
    
    if body:
        h.update(body)
    else:
        h.update("")

    return h.hexdigest()

def aws_auth_uri(ctx, req, body):
    '''Cononicalized path
    '''
    
    path = req.uri.path
    if not path:
        return ""
    
    return path

def aws_auth_querystring(ctx, req, body):
    '''Cononicalized querystring
    '''
    
    query = req.uri.query
    if not query:
        return ""
    
    if query[0] == '?':
        query = query[1:]
        
    return query

def aws_auth_headers(ctx, req, body, XAmzDate):
    '''Cononicalized headers
    '''
    
    headers = {}

    if 'range' in req.headers.keys():
        headers['range'] = req.headers['range'][0].value

    if req.contentType:
        headers['content-type'] = req.contentType

    for key in req.headers:
        key = key.lower()
        if key.startswith("x-amz-"):
            headers[key] = req.headers[key][0].value

    payloadDigest = aws_auth_payload(ctx, req, body)
    headers['x-amz-content-sha256'] = payloadDigest
    req.headers['x-amz-content-sha256'] = payloadDigest

    headers['host'] = req.headers['host'][0].value
    headers['x-amz-date'] = XAmzDate
    req.headers['x-amz-date'] = XAmzDate

    CanonicalHeaders = ""
    keys = headers.keys()
    keys.sort()

    for key in keys:
        CanonicalHeaders += "%s:%s\n" % (key, headers[key].strip())

    SignedHeaders = ";".join(keys)

    return (CanonicalHeaders, SignedHeaders)

        
# end
