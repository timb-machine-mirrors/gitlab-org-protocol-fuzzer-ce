#!/usr/bin/python

'''Example automated test script for flask_rest_target

This example automated test/unittest uses pytest.  With
the pytest-peach module this test script can be used
with Peach Web Proxy.

  pytest test_target.py --peach=auto

'''

from __future__ import print_function
import pytest, os, json
from requests import put, get, delete, post

@pytest.fixture
def mytest():
	setup_class()
	yield ""
	teardown_class()
	
def setup_class():
	try:
		delete('http://127.0.0.1:5000/api/users/2')
	except:
		pass
	try:
		delete('http://127.0.0.1:5000/api/users?user=dd')
	except:
		pass
	
def teardown_class():
	try:
		delete('http://127.0.0.1:5000/api/users/2')
	except:
		pass
	try:
		delete('http://127.0.0.1:5000/api/users?user=dd')
	except:
		pass
	
def test_users_getall(mytest):
	get('http://127.0.0.1:5000/api/users')

def test_user_create(mytest):
	r = post('http://127.0.0.1:5000/api/users', data=json.dumps(
		{"user":"dd", "first":"mike", "last":"smith", "password":"hello"}))
	user = r.json()
	get('http://127.0.0.1:5000/api/users/%d' % user['user_id'])
	delete('http://127.0.0.1:5000/api/users/%d' % user['user_id'])

def test_user_update(mytest):
	r = post('http://127.0.0.1:5000/api/users', data=json.dumps({"user":"dd", "first":"mike", "last":"smith", "password":"hello"}))
	user = r.json()
	get('http://127.0.0.1:5000/api/users/%d' % user['user_id'])
	put('http://127.0.0.1:5000/api/users/%d' % user['user_id'], data=json.dumps({"user":"dd", "first":"mike", "last":"smith", "password":"hello"}))
	delete('http://127.0.0.1:5000/api/users/%d' % user['user_id'])


if __name__ == "__main__":
	print()
	print("This script is intended to be run using pytest module.")
	print("Please see README for more information.")
	print()
	print("Example usage with pytest and pytest-peach:")
	print()
	print("  pytest test_target.py --peach=auto")
	print()

# end
