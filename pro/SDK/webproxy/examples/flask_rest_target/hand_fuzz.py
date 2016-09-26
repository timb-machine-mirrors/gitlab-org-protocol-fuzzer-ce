#!/usr/bin/python

'''Example traffic generator in Python

Uses both the Peach Proxy API to notify when a test starts
and the python requests library to make HTTP calls.
'''

import os, json
from requests import put, get, delete, post
import requests, json, sys

'''The peachproxy module provides helper methods for calling
the Peach Web Proxy APIs.  These api's are used to integrate
our traffic generator script with Peach.'''
import peachproxy

## Configuration options

jobid = 'auto'
api = 'http://10.0.1.77:8888'
proxy = 'http://10.0.1.77:8001'

# HTTP target
target = 'http://127.0.0.1:5000'
req_args = {}

# HTTPS target
#target = 'https://127.0.0.1:5000'
#req_args = { 'verify' : False }

# HTTPS target with client cert
#target = 'https://127.0.0.1:5000'
#req_args = { 'cert' : ('clientcert.crt', 'clientcert.key'), 'verify' : False }

# Set proxy for requires library
os.environ["HTTP_PROXY"] = proxy
os.environ["HTTPS_PROXY"] = proxy

## Test cases

def test_setup():
	'''
	Setup the test by clearing out created users.
	'''
	
	try:
		delete(target+'/api/users/2', **req_args)
	except:
		print "E1"
		pass
	try:
		delete(target+'/api/users?user=dd', **req_args)
	except:
		print "E2"
		pass

def test_teardown():
	'''
	Teardown test by clearing out created users.
	'''
	
	try:
		delete(target+'/api/users/2', **req_args)
	except:
		print "E1"
		pass
	try:
		delete(target+'/api/users?user=dd', **req_args)
	except:
		print "E2"
		pass

	pass

def test_user_create():
	r = post(target+'/api/users', data=json.dumps({"user":"dd", "first":"mike", "last":"smith", "password":"hello"}, **req_args))
	user = r.json()
	get(target+'/api/users/%d' % user['user_id'], **req_args)
	delete(target+'/api/users/%d' % user['user_id'], **req_args)

def test_user_update():
	r = post(target+'/api/users', data=json.dumps({"user":"dd", "first":"mike", "last":"smith", "password":"hello"}, **req_args))
	user = r.json()
	get(target+'/api/users/%d' % user['user_id'], **req_args)
	put(target+'/api/users/%d' % user['user_id'], data=json.dumps({"user":"dd", "first":"mike", "last":"smith", "password":"hello"}, **req_args))
	delete(target+'/api/users/%d' % user['user_id'], **req_args)

##############################
## Traffic Generation

'''Notify Peach we are starting a fuzzing session'''
peachproxy.session_setup(api, jobid)

#for cnt in xrange(100000):
while True:
	
	print "\n\n----] test_user_create [----------------------------\n"
	
	for i in range(100):
		print ".",
		
		'''Notify Peach we are doing test setup.  This will
		allow the traffic through with no modification (fuzzing off).'''
		peachproxy.setup(api)
		test_setup()
		
		'''Notify Peach we are performing a test.  This will enable
		fuzzing of the requests.  All requests will be considered
		part of this test case until another Peach Web Proxy API is
		called.'''
		peachproxy.testcase('test_user_create', api)
		
		try:
			test_user_create()
			print "P"
		except:
			print "E"
		
		'''Notify Peach our test case is complete and we are performing
		teardown/cleanup.  This will allow the traffic through with no
		modification (fuzzing off)'''
		peachproxy.teardown(api)
		test_teardown()
	
	print "\n\n----] test_user_update [----------------------------\n"
	
	for i in range(100):
		print ".",
		peachproxy.setup(api)
		test_setup()
		
		peachproxy.testcase('test_user_update', api)
		
		try:
			test_user_update()
			print "P"
		except:
			print "E"
		
		peachproxy.teardown(api)
		test_teardown()

'''Notify Peach our job is completed.  This will stop the webproxy
and generate a testing report.'''
peachproxy.session_teardown(api)

# end
