
'''Peach Proxy Python Module
Copyright (c) 2016 Peach Fuzzer, LLC

This is a python module that provides method to call
the Peach Proxy Restful API.  This allows users
to integrate into unit-tests or custom traffic generators.
'''

from __future__ import print_function
import os, warnings, logging
import requests, json, sys
from requests import put, get, delete, post


logger = logging.getLogger(__name__)

## This code will block the use of proxies.

session = requests.Session()
session.trust_env = False

## Peach Proxy API Helper Functions

__peach_jobid = None

def __peach_getJobId(api):
        '''DO NOT DIRECTLY CALL
        
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

def session_setup(api='http://127.0.0.1:8888', jobid='auto'):
        '''Notify Peach Proxy that a test session is starting.
        Called ONCE at start of testing.

        Keyword arguments:
        api -- Peach API URL (default: http://127.0.0.1:8888)
        jobid -- Peach Job ID or 'auto' to detect. Defaults to 'auto'.
        '''
        
        global __peach_jobid
        __peach_jobid = jobid
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

def session_teardown(api='http://127.0.0.1:8888'):
        '''Notify Peach Proxy that a test session is ending.
        
        Called ONCE at end of testing. This will cause Peach to stop.
        
        Keyword arguments:
        api -- Peach API URL (default: http://127.0.0.1:8888)
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

                
def setup(api='http://127.0.0.1:8888'):
        '''Notify Peach Proxy that setup tasks are about to run.

        This will disable fuzzing of messages so the setup tasks
        always work OK.
        
        Keyword arguments:
        api -- Peach API URL (default: http://127.0.0.1:8888)
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
        
def teardown(api='http://127.0.0.1:8888'):
        '''Notify Peach Proxy that teardown tasks are about to run.
        
        This will disable fuzzing of messages so the teardown tasks
        always work OK.

        Keyword arguments:
        api -- Peach API URL (default: http://127.0.0.1:8888)
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
        
        
def testcase(name, api='http://127.0.0.1:8888'):
        '''Notify Peach Proxy that a test case is starting.
        This will enable fuzzing and group all of the following
        requests into a group.
        
        Keyword arguments:
        name -- Name of unit test. Shows up in metrics.
        api -- Peach API URL (default: http://127.0.0.1:8888)
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


if __name__ == "__main__":
    
    print("This is a python module and should only be used by other")
    print("Python programs.  It was not intended to be run directly.")
    print("\n")
    print("For more information see the README")
    
# end
