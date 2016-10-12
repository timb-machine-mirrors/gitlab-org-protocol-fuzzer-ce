from peach import webproxy

import base64
import hmac

from hashlib import sha1
from email.Utils import formatdate

AWS_ACCESS_KEY_ID = '44CF9590006BF252F707'
AWS_SECRET_KEY = 'OtxrzxIsfpFjA7SwPzILwy8Bw21TLhquhboDYROV'

def aws_auth(ctx, req, body):
    XAmzDate = formatdate()

    h = hmac.new(AWS_SECRET_KEY, '%s\n\n%s\n\nx-amz-date:%s\n/?policy' % (req.method, req.contentType, XAmzDate), sha1)
    authToken = base64.encodestring(h.digest()).strip()

    req.headers['x-amz-date'] = XAmzDate
    req.headers['Authorization'] = 'AWS %s:%s' % (AWS_ACCESS_KEY_ID, authToken)

webproxy.register_event(webproxy.EVENT_ACTION, aws_auth)
