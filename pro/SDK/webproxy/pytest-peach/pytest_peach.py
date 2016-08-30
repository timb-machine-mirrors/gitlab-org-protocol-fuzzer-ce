from __future__ import print_function

import warnings
from unittest import TestCase

import pytest
import requests
from requests import put, get, delete, post
import json

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
        r =     session.get("%s/p/jobs?dryrun=false&running=true" % api).json()
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
    
    session.put("%s/p/proxy/%s/sessionTearDown" % (api, jobid))

@pytest.hookimpl(hookwrapper=True)
def pytest_runtest_setup(item):
    jobid = item.config.option.peach
    api = item.config.option.peach_api
    if not jobid:
        return
    
    session.put("%s/p/proxy/%s/testSetUp" % (api, jobid))

@pytest.hookimpl(hookwrapper=True)
def pytest_runtest_call(item):
    jobid = item.config.option.peach
    api = item.config.option.peach_api
    if not jobid:
        return
    
    session.put("%s/p/proxy/%s/testCall" % (api, jobid), data=json.dumps({"name":getTestName(item)}))

@pytest.hookimpl(hookwrapper=True)
def pytest_runtest_teardown(item, nextitem):
    jobid = item.config.option.peach
    api = item.config.option.peach_api
    if not jobid:
        return
    
    session.put("%s/p/proxy/%s/testTearDown" % (api, jobid))

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
