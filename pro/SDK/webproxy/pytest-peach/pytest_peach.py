from __future__ import print_function

import os

#os.environ["HTTP_PROXY"] = 'http://127.0.0.1:8001'

import warnings
from unittest import TestCase
import pytest
import requests, json, sys
from requests import put, get, delete, post

session = requests.Session()
session.trust_env = False

def pytest_addoption(parser):
    parser.addoption(
        '--peach_count',
        action='store',
        default=1000,
        type=int,
        help='Enable Peach Fuzzer pytest module, repeat count (defaults to 1000)')
    
    parser.addoption(
        '--peach',
        action='store',
        default=None,
        type=str,
        help='Enable Peach Fuzzer support and set Peach job id.')
    
    parser.addoption(
        '--peach_api',
        action='store',
        default='http://127.0.0.1:8888',
        type=str,
        help='Set Peach Fuzzer API URL. Defaults to http://127.0.0.1:8888.')
    
def getJobId(config):
    jobid = config.option.peach
    api = config.option.peach_api
    
    if jobid == 'auto':
        try:
            r = session.get("%s/p/jobs?dryrun=false&running=true" % api)
            if r.status_code != 200:
                print("pytest-peach: Error communicating with Peach Fuzzer. Status code was %s" % r.status_code)
                sys.exit(-1)
        except requests.exceptions.RequestException as e:
            print("pytest-peach: Error communicating with Peach Fuzzer.")
            print("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
            print(e)
            print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
            sys.exit(-1)
        
        r = r.json()
        jobid = r[0]['id']
        config.option.peach = jobid
        
    return jobid

def getTestName(item):
    name = item.name
    return name[:name.rfind('[')]

def pytest_configure(config):
    jobid = getJobId(config)
    api = config.option.peach_api
    if not jobid:
        return
    
    session.put("%s/p/proxy/%s/sessionSetUp" % (api, jobid))

def pytest_unconfigure(config):
    jobid = config.option.peach
    api = config.option.peach_api
    if not jobid:
        return
    
    try:
        session.put("%s/p/proxy/%s/sessionTearDown" % (api, jobid))
    except requests.exceptions.RequestException as e:
        print("pytest-peach: Error communicating with Peach Fuzzer.")
        print("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
        print(e)
        print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
        sys.exit(-1)

@pytest.hookimpl(tryfirst=True)
def pytest_runtest_setup(item):
    print(">>pytest_runtest_setup")
    jobid = item.config.option.peach
    api = item.config.option.peach_api
    if not jobid:
        return
    
    try:
        session.put("%s/p/proxy/%s/testSetUp" % (api, jobid))
    except requests.exceptions.RequestException as e:
        print("pytest-peach: Error communicating with Peach Fuzzer.")
        print("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
        print(e)
        print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
        sys.exit(-1)

@pytest.hookimpl(tryfirst=True)
def pytest_runtest_call(item):
    print(">>pytest_runtest_call")
    jobid = item.config.option.peach
    api = item.config.option.peach_api
    if not jobid:
        return
    
    try:
        session.put("%s/p/proxy/%s/testCase" % (api, jobid), json={"name":getTestName(item)})
    except requests.exceptions.RequestException as e:
        print("pytest-peach: Error communicating with Peach Fuzzer.")
        print("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
        print(e)
        print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
        sys.exit(-1)

@pytest.hookimpl(tryfirst=True)
def pytest_runtest_teardown(item, nextitem):
    print(">>pytest_runtest_teardown")
    jobid = item.config.option.peach
    api = item.config.option.peach_api
    if not jobid:
        return
    
    try:
        session.put("%s/p/proxy/%s/testTearDown" % (api, jobid))
    except requests.exceptions.RequestException as e:
        print("pytest-peach: Error communicating with Peach Fuzzer.")
        print("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
        print(e)
        print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
        sys.exit(-1)

class UnexpectedError(Exception):
    pass

@pytest.fixture(autouse=True)
def __pytest_peach_step_number(request):
    peach = request.config.option.peach
    count = request.config.option.peach_count
    
    if peach != None and count > 1:
        try:
            return request.param
        except AttributeError:
            if issubclass(request.cls, TestCase):
                warnings.warn(
                    "Peach fuzzing unittest class tests not supported")
            else:
                raise UnexpectedError(
                    "This call couldn't work with pytest-peach. "
                    "Please consider raising an issue with your usage.")


def pytest_generate_tests(metafunc):
    peach = metafunc.config.option.peach
    count = metafunc.config.option.peach_count
    if peach != None and count > 1:

        def make_progress_id(i, n=count):
            return '{0}/{1}'.format(i + 1, n)

        metafunc.parametrize(
            '__pytest_peach_step_number',
            range(count),
            indirect=True,
            ids=make_progress_id,
        )

# end
