#!/usr/bin/python

'''
pytest-peach extension
Copyright (c) 2016 Peach Fuzzer, LLC

Provides integration with Peach WebProxy
'''

from __future__ import print_function
import os, warnings, logging
import pytest
import requests, json, sys
from requests import put, get, delete, post

logger = logging.getLogger(__name__)

session = requests.Session()
session.trust_env = False

__peach_jobid = None

## Peach Proxy API Helper Functions

def __peach_getJobId(api):
        '''
        DO NOT DIRECTLY CALL
        
        Get and Cache our Peach Fuzzer JOB ID.

        Each time Peach is started we get a new Job identifier.
        The Peach proxy API calls require this ID to work.
        By default we will get the current active JOB.
        '''
        
        global __peach_jobid
        
        if not __peach_jobid:
            return None
        
        if __peach_jobid == 'auto':
                try:
                        r = session.get("%s/p/jobs?dryrun=false&running=true" % api)
                        if r.status_code != 200:
                                print("Error communicating with Peach Fuzzer. Status code was %s" % r.status_code)
                                sys.exit(-1)
                except requests.exceptions.RequestException as e:
                        print("Error communicating with Peach Fuzzer.")
                        print("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
                        print(e)
                        print("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
                        sys.exit(-1)
                
                r = r.json()
                __peach_jobid = r[0]['id']
                
        return __peach_jobid

def peach_session_setup(api):
        '''
        Notify Peach Proxy that a test session is starting.

        Called ONCE at start of testing.
        '''
        
        jobid = __peach_getJobId(api)
        if not jobid:
            return

        try:
                logger.info("api: %s jobid: %s" % (api, jobid))
                r = session.put("%s/p/proxy/%s/sessionSetUp" % (api, jobid))
                if r.status_code != 200:
                        logger.error('Error sending sessionSetUp: ', r.status_code)
                        sys.exit(-1)
        except requests.exceptions.RequestException as e:
                logger.error("Error communicating with Peach Fuzzer.")
                logger.error("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
                logger.error(e)
                logger.error("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
                sys.exit(-1)

def peach_session_teardown(api):
        '''
        Notify Peach Proxy that a test session is ending.

        Called ONCE at end of testing. This will cause Peach to stop.
        '''
        
        jobid = __peach_getJobId(api)
        if not jobid:
            return

        try:
                r = session.put("%s/p/proxy/%s/sessionTearDown" % (api, jobid))
                if r.status_code != 200:
                        logger.error('Error sending sessionTearDown: ', r.status_code)
                        sys.exit(-1)
        except requests.exceptions.RequestException as e:
                logger.error("Error communicating with Peach Fuzzer.")
                logger.error("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
                logger.error(e)
                logger.error("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
                sys.exit(-1)

                
def peach_setup(api):
        '''
        Notify Peach Proxy that setup tasks are about to run.

        This will disable fuzzing of messages so the setup tasks
        always work OK.
        '''
        
        jobid = __peach_getJobId(api)
        if not jobid:
            return

        try:
                r = session.put("%s/p/proxy/%s/testSetUp" % (api, jobid))
                if r.status_code != 200:
                        logger.error('Error sending testSetUp: ', r.status_code)
                        sys.exit(-1)
        except requests.exceptions.RequestException as e:
            logger.error("Error communicating with Peach Fuzzer.")
            logger.error("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
            logger.error(e)
            logger.error("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
            sys.exit(-1)
        
def peach_teardown(api):
        '''
        Notify Peach Proxy that teardown tasks are about to run.
        
        This will disable fuzzing of messages so the teardown tasks
        always work OK.
        '''
        
        jobid = __peach_getJobId(api)
        if not jobid:
            return

        try:
                r = session.put("%s/p/proxy/%s/testTearDown" % (api, jobid))
                if r.status_code != 200:
                        logger.error('Error sending testSetUp: ', r.status_code)
                        sys.exit(-1)
        except requests.exceptions.RequestException as e:
            logger.error("Error communicating with Peach Fuzzer.")
            logger.error("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
            logger.error(e)
            logger.error("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
            sys.exit(-1)
        
        
def peach_testcase(api, name):
        '''
        Notify Peach Proxy that a test case is starting.
        
          name - Name of unit test. Shows up in metrics.

        This will enable fuzzing and group all of the following
        requests into a group.
        '''
        
        jobid = __peach_getJobId(api)
        if not jobid:
            return
        
        try:
                r = session.put("%s/p/proxy/%s/testCase" % (api, jobid), json={"name":name})
                if r.status_code != 200:
                        logger.error('Error sending testCase: ', r.status_code)
                        sys.exit(-1)
        except requests.exceptions.RequestException as e:
            logger.error("Error communicating with Peach Fuzzer.")
            logger.error("vvvv ERROR vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv")
            logger.error(e)
            logger.error("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^")
            sys.exit(-1)


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
    
    parser.addoption(
        '--peach_proxy',
        action='store',
        default='http://127.0.0.1:8001',
        type=str,
        help='Set Peach Fuzzer WebProxy URL. Defaults to http://127.0.0.1:8001.')
    
def getTestName(item):
    name = item.name
    return name[:name.rfind('[')]

def pytest_configure(config):
    __peach_jobid = config.option.peach
    if not __peach_jobid:
        return
    
    #logger.setLevel(logging.DEBUG)
    logger.setLevel(logging.ERROR)
    
    logFormatter = logging.Formatter("%(asctime)s [%(levelname)-5.5s] %(message)s")
    consoleHandler = logging.StreamHandler()
    consoleHandler.setFormatter(logFormatter)
    logger.addHandler(consoleHandler)
    
    #fileHandler = logging.FileHandler('pytest-peach.log')
    #fileHandler.setFormatter(logFormatter)
    #logger.addHandler(fileHandler)
    

    logger.info("pytest-peach initializing.")
    logger.debug(">>pytest_configure")
    
    api = config.option.peach_api
    
    # Set proxy
    os.environ["HTTP_PROXY"] = config.option.peach_proxy
    os.environ["HTTPS_PROXY"] = config.option.peach_proxy
    
    peach_session_setup(api)

def pytest_unconfigure(config):
    if not __peach_jobid:
        return
    logger.debug(">>pytest_unconfigure")
    api = config.option.peach_api
    peach_session_teardown(api)


def pytest_report_teststatus(report):
    '''
    Fake all tests passing
    '''
    
    if not __peach_jobid:
        return
    report.outcome = 'passed'
    return None


@pytest.hookimpl(tryfirst=True)
def pytest_runtest_setup(item):
    if not __peach_jobid:
        return
    
    logger.debug(">>pytest_runtest_setup")
    
    api = item.config.option.peach_api
    peach_setup(api)

@pytest.hookimpl(tryfirst=True)
def pytest_runtest_call(item):
    if not __peach_jobid:
        return
    logger.debug(">>pytest_runtest_call")
    api = item.config.option.peach_api
    peach_testcase(api, getTestName(item))

@pytest.hookimpl(tryfirst=True)
def pytest_runtest_teardown(item, nextitem):
    if not __peach_jobid:
        return
    logger.debug(">>pytest_runtest_teardown")
    api = item.config.option.peach_api
    peach_teardown(api)

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
    logger.debug(">>pytest_generate_tests")
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
