
'''
Example traffic generator in Python

Uses both the Peach Proxy API to notify when a test starts
and the python requests library to make HTTP calls.
'''

import os, json
from requests import put, get, delete, post
import requests, json, sys

session = requests.Session()
session.trust_env = False

## Configuration options

jobid = 'auto'
api = 'http://127.0.0.1:8888'
proxy = 'http://127.0.0.1:8001'

# Set proxy for requires library
os.environ["HTTP_PROXY"] = proxy
os.environ["HTTPS_PROXY"] = proxy

## Peach Proxy API Helper Functions

def __peach_getJobId():
	'''
	DO NOT DIRECTLY CALL
	
	Get and Cache our Peach Fuzzer JOB ID.

	Each time Peach is started we get a new Job identifier.
	The Peach proxy API calls require this ID to work.
	By default we will get the current active JOB.
	'''
	
	global jobid
	
	if jobid == 'auto':
		try:
			r = session.get("%s/p/jobs?dryrun=false&running=true" % api)
			if r.status_code != 200:
				print("pytest-peach: Error communicating with Peach Fuzzer. Status code was %s" % r.status_code)
				sys.exit(-1)
		except requests.exceptions.RequestException as e:
			print("Error communicating with Peach Fuzzer.")
			print("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
			print(e)
			print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
			sys.exit(-1)
		
		r = r.json()
		jobid = r[0]['id']
		
	return jobid

def peach_session_setup():
	'''
	Notify Peach Proxy that a test session is starting.

	Called ONCE at start of testing.
	'''
	
	__peach_getJobId()

	try:
		print("api: %s jobid: %s" % (api, jobid))
		r = session.put("%s/p/proxy/%s/sessionSetUp" % (api, jobid))
		if r.status_code != 200:
			print('Error sending sessionSetUp: ', r.status_code)
			sys.exit(-1)
	except requests.exceptions.RequestException as e:
		print("Error communicating with Peach Fuzzer.")
		print("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
		print(e)
		print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
		sys.exit(-1)

def peach_session_teardown():
	'''
	Notify Peach Proxy that a test session is ending.

	Called ONCE at end of testing. This will cause Peach to stop.
	'''
	
	__peach_getJobId()

	try:
		r = session.put("%s/p/proxy/%s/sessionTearDown" % (api, jobid))
		if r.status_code != 200:
			print('Error sending sessionTearDown: ', r.status_code)
			sys.exit(-1)
	except requests.exceptions.RequestException as e:
		print("Error communicating with Peach Fuzzer.")
		print("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
		print(e)
		print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
		sys.exit(-1)

		
def peach_setup():
	'''
	Notify Peach Proxy that setup tasks are about to run.

	This will disable fuzzing of messages so the setup tasks
	always work OK.
	'''
	
	__peach_getJobId()

	try:
		r = session.put("%s/p/proxy/%s/testSetUp" % (api, jobid))
		if r.status_code != 200:
			print('Error sending testSetUp: ', r.status_code)
			sys.exit(-1)
	except requests.exceptions.RequestException as e:
	    print("Error communicating with Peach Fuzzer.")
	    print("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
	    print(e)
	    print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
	    sys.exit(-1)
	
	
def peach_teardown():
	'''
	Notify Peach Proxy that teardown tasks are about to run.
	
	This will disable fuzzing of messages so the teardown tasks
	always work OK.
	'''
	
	__peach_getJobId()

	try:
		r = session.put("%s/p/proxy/%s/testTearDown" % (api, jobid))
		if r.status_code != 200:
			print('Error sending testSetUp: ', r.status_code)
			sys.exit(-1)
	except requests.exceptions.RequestException as e:
	    print("Error communicating with Peach Fuzzer.")
	    print("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
	    print(e)
	    print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
	    sys.exit(-1)
	
	
def peach_testcase(name):
	'''
	Notify Peach Proxy that a test case is starting.
	
	  name - Name of unit test. Shows up in metrics.

	This will enable fuzzing and group all of the following
	requests into a group.
	'''
	
	__peach_getJobId()
	
	try:
		r = session.put("%s/p/proxy/%s/testCase" % (api, jobid), json={"name":name})
		if r.status_code != 200:
			print('Error sending testCase: ', r.status_code)
			sys.exit(-1)
	except requests.exceptions.RequestException as e:
	    print("Error communicating with Peach Fuzzer.")
	    print("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
	    print(e)
	    print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
	    sys.exit(-1)

## Test cases

def test_setup():
	'''
	Setup the test by clearing out created users.
	'''
	
	try:
		delete('http://127.0.0.1:5000/api/users/2')
	except:
		print "E1"
		pass
	try:
		delete('http://127.0.0.1:5000/api/users?user=dd')
	except:
		print "E2"
		pass

def test_teardown():
	'''
	Teardown test by clearing out created users.
	'''
	
	try:
		delete('http://127.0.0.1:5000/api/users/2')
	except:
		print "E1"
		pass
	try:
		delete('http://127.0.0.1:5000/api/users?user=dd')
	except:
		print "E2"
		pass

	pass

def test_user_create():
	r = post('http://127.0.0.1:5000/api/users', data=json.dumps({"user":"dd", "first":"mike", "last":"smith", "password":"hello"}))
	user = r.json()
	get('http://127.0.0.1:5000/api/users/%d' % user['user_id'])
	delete('http://127.0.0.1:5000/api/users/%d' % user['user_id'])

def test_user_update():
	r = post('http://127.0.0.1:5000/api/users', data=json.dumps({"user":"dd", "first":"mike", "last":"smith", "password":"hello"}))
	user = r.json()
	get('http://127.0.0.1:5000/api/users/%d' % user['user_id'])
	put('http://127.0.0.1:5000/api/users/%d' % user['user_id'], data=json.dumps({"user":"dd", "first":"mike", "last":"smith", "password":"hello"}))
	delete('http://127.0.0.1:5000/api/users/%d' % user['user_id'])

##############################
## Traffic Generation

peach_session_setup()

#for cnt in xrange(100000):
while True:
	
	print "\n\n----] test_user_create [----------------------------\n"
	
	for i in range(100):
		print ".",
		peach_setup()
		test_setup()
		
		peach_testcase('test_user_create')
		
		try:
			test_user_create()
			print "P"
		except:
			print "E"
		
		peach_teardown()
		test_teardown()
	
	print "\n\n----] test_user_update [----------------------------\n"
	
	for i in range(100):
		print(".")
		peach_setup()
		test_setup()
		
		peach_testcase('test_user_update')
		
		try:
			test_user_update()
			print "P"
		except:
			print "E"
			pass
		
		peach_teardown()
		test_teardown()


peach_session_teardown()


# end
