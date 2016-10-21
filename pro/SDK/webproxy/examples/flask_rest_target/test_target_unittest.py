#!/usr/bin/python

'''Example automated test script for flask_rest_target

This example automated test/unittest uses unittest.

For normal run:
    python -m unittest -v test_target_unittest
    
For fuzzing run:

    set PYTHONPATH=C:\peach-pro\pro\SDK\webproxy\testrunners\unittest;c:\Python27\lib;c:\python27\libs
    python -m unittest -ahttp://127.0.0.1:8888 -v test_target_unittest
    
'''

from __future__ import print_function
import unittest, os, json
from requests import put, get, delete, post

class FlaskTargetTests(unittest.TestCase):
    def setUp():
            try:
                    delete('http://127.0.0.1:5000/api/users/2')
            except:
                    pass
            try:
                    delete('http://127.0.0.1:5000/api/users?user=dd')
            except:
                    pass
            
    def tearDown():
            try:
                    delete('http://127.0.0.1:5000/api/users/2')
            except:
                    pass
            try:
                    delete('http://127.0.0.1:5000/api/users?user=dd')
            except:
                    pass
            
    def test_users_getall(self):
            get('http://127.0.0.1:5000/api/users')
    
    def test_user_create(self):
            r = post('http://127.0.0.1:5000/api/users', data=json.dumps(
                    {"user":"dd", "first":"mike", "last":"smith", "password":"hello"}))
            user = r.json()
            get('http://127.0.0.1:5000/api/users/%d' % user['user_id'])
            delete('http://127.0.0.1:5000/api/users/%d' % user['user_id'])
    
    def test_user_update(self):
            r = post('http://127.0.0.1:5000/api/users', data=json.dumps({"user":"dd", "first":"mike", "last":"smith", "password":"hello"}))
            user = r.json()
            get('http://127.0.0.1:5000/api/users/%d' % user['user_id'])
            put('http://127.0.0.1:5000/api/users/%d' % user['user_id'], data=json.dumps({"user":"dd", "first":"mike", "last":"smith", "password":"hello"}))
            delete('http://127.0.0.1:5000/api/users/%d' % user['user_id'])


if __name__ == "__main__":
	print()
	print("This script is intended to be run using the unittest module.")
	print()

# end
