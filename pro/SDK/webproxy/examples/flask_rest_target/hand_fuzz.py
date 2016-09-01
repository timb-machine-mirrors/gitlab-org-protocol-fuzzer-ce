
import os, json
from requests import put, get, delete, post
import requests, json, sys

session = requests.Session()
session.trust_env = False

jobid = 'auto'
api = 'http://127.0.0.1:8888'

def getJobId():
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

	
def setup():
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
	
	try:
		delete('http://127.0.0.1:5000/api/users/2')
	except:
		pass
	try:
		delete('http://127.0.0.1:5000/api/users?user=dd')
	except:
		pass
	
def teardown():
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
	
	try:
		delete('http://127.0.0.1:5000/api/users/2')
	except:
		pass
	try:
		delete('http://127.0.0.1:5000/api/users?user=dd')
	except:
		pass

def testcase(name):
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

	
#def test_users_getall(mytest):
#	get('http://127.0.0.1:5000/api/users')

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
# Session Start

getJobId()

try:
	print("api: %s jobid: %s" % (api, jobid))
	r = session.put("%s/p/proxy/%s/sessionSetUp" % (api, jobid))
	if r.status_code != 200:
		print('Error sending sessionSetUp: ', r.status_code)
		sys.exit(-1)
except requests.exceptions.RequestException as e:
    print("pytest-peach: Error communicating with Peach Fuzzer.")
    print("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
    print(e)
    print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
    sys.exit(-1)


for i in range(1):
	print(".")
	setup()
	testcase('test_user_create')
	
	try:
		test_user_create()
	except:
		pass
	
	teardown()

for i in range(1):
	print(".")
	setup()
	testcase('test_user_update')
	
	try:
		test_user_update()
	except:
		pass
	
	teardown()

try:
	r = session.put("%s/p/proxy/%s/sessionTearDown" % (api, jobid))
	if r.status_code != 200:
		print('Error sending sessionTearDown: ', r.status_code)
		sys.exit(-1)
except requests.exceptions.RequestException as e:
    print("pytest-peach: Error communicating with Peach Fuzzer.")
    print("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
    print(e)
    print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
    sys.exit(-1)


# end
