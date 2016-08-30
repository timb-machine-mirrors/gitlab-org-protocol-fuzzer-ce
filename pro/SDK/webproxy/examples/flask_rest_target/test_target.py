
import pytest, os, json
from requests import put, get, delete, post

@classmethod
def setup_class():
	try:
		delete('http://127.0.0.1:5000/api/users/2')
	except:
		pass
	try:
		delete('http://127.0.0.1:5000/api/users?user=dd')
	except:
		pass
	
@classmethod
def teardown_class():
	try:
		delete('http://127.0.0.1:5000/api/users/2')
	except:
		pass
	try:
		delete('http://127.0.0.1:5000/api/users?user=dd')
	except:
		pass
	
def test_users_getall():
	get('http://127.0.0.1:5000/api/users')

def test_user_create():
	user = post('http://127.0.0.1:5000/api/users', data=json.dumps({"user":"dd", "first":"mike", "last":"smith", "password":"hello"})).json()
	get('http://127.0.0.1:5000/api/users/%s' % user['user_id'])
	delete('http://127.0.0.1:5000/api/users/%' % user['user_id'])

def test_user_update():
	post('http://127.0.0.1:5000/api/users', data=json.dumps({"user":"dd", "first":"mike", "last":"smith", "password":"hello"}))
	get('http://127.0.0.1:5000/api/users/%s' % user['user_id'])
	put('http://127.0.0.1:5000/api/users/%s' % user['user_id'], data=json.dumps({"user":"dd", "first":"mike", "last":"smith", "password":"hello"}))
	delete('http://127.0.0.1:5000/api/users/%s' % user['user_id'])

# end
