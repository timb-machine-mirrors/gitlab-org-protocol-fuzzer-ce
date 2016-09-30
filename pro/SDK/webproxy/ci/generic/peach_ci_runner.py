#!/usr/bin/python

'''
Peach CI Generic Integration Runner
Copyright (c) 2016 Peach Fuzzer, LLC

This script provides generic integration with CI systems by
running a command that returns non-zero when testing did not pass.
The vast majority of CI systems support this method of integration.

If a specific integration is offered for your CI system that is
preferred over this generic integration.
'''

import logging

## Configuration

# Directory containing Peach install
peach_exe = '/opt/peach/peach'
peach_exe = 'c:/peach-pro/output/win_x64_debug/bin/peach.exe'

# Configuration to start
pit_config = 'WebProxy-Flask-Demo'

# Port to start Peach on
peach_port = 8888

# Exit code when testing passed
exit_code_ok = 0

# Exit code when testing failed
exit_code_failure = 1

# Exit code when error occurred during testing
exit_code_error = 100

# Enable logging to syslog
syslog_enabled = False
syslog_host = 'logserver.foobar.com'
syslog_port = 514
syslog_level = logging.ERROR
syslog_level = logging.INFO

###############################################################
## DO NOT EDIT BELOW THIS LINE
###############################################################

import os
from requests import get, post
import requests, json, sys
import subprocess, signal, psutil
import logging, logging.handlers
from time import sleep

logger = logging.getLogger(__name__)

logger.setLevel(syslog_level)
logFormatter = logging.Formatter("%(asctime)s [%(levelname)-5.5s] %(message)s")

if syslog_enabled:
    syslogHandler = logging.handlers.SysLogHandler(address=(syslog_host, syslog_port))
    syslogHandler.setFormatter(logFormatter)
    logger.addHandler(syslogHandler)

consoleHandler = logging.StreamHandler()
consoleHandler.setFormatter(logFormatter)
logger.addHandler(consoleHandler)

logger.info("Peach CI Generic Starting")
logger.info("  peach_exe: %s", peach_exe)
logger.info("  pit_config: %s", pit_config)
logger.info("  peach_port: %d", peach_port)
logger.info("  exit_code_ok: %d", exit_code_ok)
logger.info("  exit_code_failure: %d", exit_code_failure)
logger.info("  exit_code_error: %d", exit_code_error)

peach_process = None

try:
#        [peach_exe, "--webport=%d" % peach_port, "--pits=c:\pits\output\pits\Assets"],
    logger.info("Starting peach")
    peach_process = subprocess.Popen(
        [peach_exe, "--webport=%d" % peach_port],
        stderr=subprocess.STDOUT)
    
    if not peach_process:
        logger.critical("Unable to start peach")
        exit(exit_code_error)
    
    sleep(1)
    if peach_process.poll():
        logger.critical("Unable to start peach")
        exit(exit_code_error)
    
except Exception as ex:
    logger.critical("Error starting peach: %s", ex)
    eexit(exit_code_error)

def kill_proc_tree(pid, including_parent=True):    
    parent = psutil.Process(pid)
    children = parent.children(recursive=True)
    for child in children:
        child.kill()
    psutil.wait_procs(children, timeout=5)
    if including_parent:
        parent.kill()
        parent.wait(5)

def eexit(code):
    logger.info("eexit(%d)", code)
    if peach_process:
        try:
            kill_proc_tree(peach_process.pid)
            kill_proc_tree(peach_process.pid)
            while not peach_process.poll():
                os.killpg(os.getpgid(peach_process.pid), signal.SIGTERM)
                peach_process.terminate()
                peach_process.kill()
                peach_process.wait()
        except:
            pass
    
    exit(code)
   
# Find configuration

logger.info("Getting pit_config url")
pit_config_url = None
for cnt in range(10):
    sleep(1)
    try:
        r = get("http://127.0.0.1:%d/p/libraries" % peach_port)
        if r.status_code != 200:
            logger.info("Library api not ready, status code: %s", r.status_code)
            continue
    except requests.exceptions.RequestException as e:
        if cnt == 9:
            logger.critical("Error communicating with Peach: %s", e)
            eexit(exit_code_error)
    
    r = r.json()
    if len(r) == 0:
        logger.info('Library API returned 0 libraries')
        continue
    
    config_library = None
    for library in r:
        if library['name'] == 'Configurations':
            config_library = library
            break
    
    if not config_library:
        logger.info("Couldn't find config library")
        continue
    
    for config in config_library['versions'][0]['pits']:
        if config['name'] == pit_config:
            pit_config_url = config['pitUrl']
            break

if not pit_config_url:
    logger.critical("Could not find configuration %s", pit_config)
    eexit(exit_code_error)

logger.info("pit_config_url: %s", pit_config_url)

# Start job

logger.info("Starting job")
peach_jobid = None

try:
    r = post("http://127.0.0.1:%d/p/jobs" % peach_port, json = { "pitUrl": pit_config_url })
    if r.status_code != 200:
        logger.error("Error communicating with Peach Fuzzer. Status code was %s", r.status_code)
        eexit(exit_code_error)
    
    r = r.json()
    peach_jobid = r['id']
    
except requests.exceptions.RequestException as e:
    logger.critical("Error communicating with Peach: %s", e)
    eexit(exit_code_error)

if not peach_jobid:
    logger.critical("Unable to start job")
    eexit(exit_code_error)
    
# Wait for fuzzing to end

logger.info("Waiting for job to complete")

while True:
    sleep(60)
    try:
        r = get("http://127.0.0.1:%d/p/jobs/%s" % (peach_port, peach_jobid))
        if r.status_code != 200:
            logger.error("Error communicating with Peach Fuzzer. Status code was %s", r.status_code)
            continue
    except requests.exceptions.RequestException as e:
        logger.critical("Error communicating with Peach: %s", e)
        eexit(exit_code_error)
    
    r = r.json()
    if r['status'] == 'running':
        logger.info("still running...")
        continue
    
    peach_fault_count = r['faultCount']
    break

logger.info("Fuzzing completed, found %d faults", peach_fault_count)

# Wait for fuzzing to complete

if peach_fault_count > 0:
    eexit(exit_code_failure)
    
eexit(exit_code_ok)

# end
