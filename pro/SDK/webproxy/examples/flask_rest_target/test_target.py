
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
	r = post('http://127.0.0.1:5000/api/users', data=json.dumps({"user":"dd", "first":"mike", "last":"smith", "password":"hello"}))
	user = r.json()
	get('http://127.0.0.1:5000/api/users/%d' % user['user_id'])
	delete('http://127.0.0.1:5000/api/users/%d' % user['user_id'])

def test_user_update(mytest):
	r = post('http://127.0.0.1:5000/api/users', data=json.dumps({"user":"dd", "first":"mike", "last":"smith", "password":"hello"}))
	user = r.json()
	get('http://127.0.0.1:5000/api/users/%d' % user['user_id'])
	put('http://127.0.0.1:5000/api/users/%d' % user['user_id'], data=json.dumps({"user":"dd", "first":"mike", "last":"smith", "password":"hello"}))
	delete('http://127.0.0.1:5000/api/users/%d' % user['user_id'])

# end
