#!/usr/bin/python

'''pytest-peach extension
Copyright (c) 2016 Peach Fuzzer, LLC

Provides integration with Peach WebProxy
'''

from __future__ import print_function
import os, warnings, logging
import pytest
import peachproxy
import requests, json, sys
from requests import put, get, delete, post

logger = logging.getLogger(__name__)
__pytest_peach_jobid = None
__pytest_peach_api = None

def pytest_addoption(parser):
    parser.addoption(
        '--peach_count',
        action='store',
        default=1000,
        type=int,
        help='How many times to repeat each test case. (defaults to 1000)')
    
    parser.addoption(
        '--peach',
        action='store',
        default=None,
        type=str,
        help='Enable Peach Fuzzer support and set Peach job id or "auto" to use latest id.')
    
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
        if not config.option.peach:
            return
        
        logger.setLevel(logging.DEBUG)
        #logger.setLevel(logging.ERROR)
        
        logFormatter = logging.Formatter("%(asctime)s [%(levelname)-5.5s] %(message)s")
        consoleHandler = logging.StreamHandler()
        consoleHandler.setFormatter(logFormatter)
        logger.addHandler(consoleHandler)
        
        #fileHandler = logging.FileHandler('pytest-peach.log')
        #fileHandler.setFormatter(logFormatter)
        #logger.addHandler(fileHandler)
        
        logger.info("pytest-peach initializing.")
        logger.debug(">>pytest_configure")
        
        global __pytest_peach_jobid
        global __pytest_peach_api
        
        api = config.option.peach_api
        __pytest_peach_api = api
        __pytest_peach_jobid = config.option.peach
        
        # Set proxy
        os.environ["HTTP_PROXY"] = config.option.peach_proxy
        os.environ["HTTPS_PROXY"] = config.option.peach_proxy
        
        peachproxy.session_setup(api, config.option.peach)

def pytest_unconfigure(config):
    if not config.option.peach:
        return
    logger.debug(">>pytest_unconfigure")
    api = config.option.peach_api
    peachproxy.session_teardown(api)


def pytest_report_teststatus(report):
    '''
    Fake all tests passing
    '''
    
    global __pytest_peach_jobid
    if not __pytest_peach_jobid:
        return

    report.outcome = 'passed'
    return None

@pytest.hookimpl(tryfirst=True)
def pytest_runtest_setup(item):
    if not item.config.option.peach:
        return
    
    logger.debug(">>pytest_runtest_setup")
    
    api = item.config.option.peach_api
    peachproxy.setup(api)

@pytest.hookimpl(tryfirst=True)
def pytest_runtest_call(item):
    if not item.config.option.peach:
        return
    logger.debug(">>pytest_runtest_call")
    api = item.config.option.peach_api
    peachproxy.testcase(getTestName(item), api)

@pytest.hookimpl(tryfirst=True)
def pytest_runtest_teardown(item, nextitem):
    if not item.config.option.peach:
        return
    logger.debug(">>pytest_runtest_teardown")
    api = item.config.option.peach_api
    peachproxy.teardown(api)

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
