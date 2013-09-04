#!/usr/bin/env python
import sys
import os
import re
import shutil
import time

from copy import copy
from types import MethodType
from subprocess import Popen, PIPE

#resolution order is ./peach, last arg, PEACH env var
PEACH_OPTS = []
BASE_DEFINES = {"Path": "."}
if not __name__ == "__main__":
    EXECPATH = os.path.join(os.path.dirname(__file__), 'CustomTests')

COLOR_CODES = {'red': 31,
               'green': 32,
               'yellow': 92}

# if this isn't being run interactive, it's probably buildbot. (*NIX then WIN.)
# if there's something strange in the neighborhood



all_tests = {}
all_defines = {}

#Switch to using unittest
#top level dirs should be test suites (Net, Image)

#as of right now we're only testing if something passes or fails. More
#advanced testing is likely to stay external (Nunit) meaning that
#unittest may be overengineering. Lets see how this goes.


class PeachTest:
    # defines should probably be generated on the fly. we can come
    # back to this.

    def __init__(self, pit, config, cwd=None, test="Default",
                 base_opts=PEACH_OPTS, setup=None, teardown=None,
                 extra_opts=None, platform='all', defines=BASE_DEFINES):
        self.status = None
        self.platform = platform
        self.pit = pit
        self.name = os.path.basename(pit)[:-4]  # pit - '.xml'
        self.peach = self._get_peach_bin(config.peach)
        self.args = []
        self.iterations = config.iterations
        self.timeout = config.timeout
        self.color = config.color
        self.base_opts = copy(base_opts)
        self.extra_opts = copy(extra_opts)
        self.cwd = cwd or os.getcwd()
        self.logdir = config.logdir
        self.hasrun = False
        self.setup = setup and MethodType(setup, self, PeachTest)
        self.teardown = setup and MethodType(teardown, self, PeachTest)
        self.defines = copy(defines)
        self.test = test
        self.timeout = config.timeout
        self.is_timed_out = False

    def _get_peach_bin(self, peach):
        if peach:
            return os.path.expanduser(peach)
        peach = os.environ.get('PEACH')
        if peach:
            return peach
        return 'peach.bat' if get_platform() == 'win' else 'peach'

    def _show_cmd(self):
        return ' '.join(arg for arg in self.args)

    def color_text(self, color, text):
        if self.color:
            code = COLOR_CODES[color]
            return "\033["+str(code)+"m"+str(text)+"\033[0m"
        return text

    def update_defines(self, **kw):
        for k, v in kw.iteritems():
            self.defines[k] = v

    def render_defines(self):
        asopts = []
        for k, v in self.defines.iteritems():
            yield '-D'
            yield '%s=%s' % (k, v)

    def build_cmd(self):
        #lets store args in self so args can be analyzed for each test
        self.args = [self.peach]
        opts = copy(self.base_opts)
        if self.extra_opts:
            opts.extend(self.extra_opts)
        if self.defines:
            opts.extend(self.render_defines())
        if self.iterations:
            assert type(self.iterations) is int and self.iterations > 0,\
                "Iterations must be a positive integer"
            if self.iterations == 1:
                opts.append('-1')
            else:
                opts.append('--range=1,%d' % self.iterations)
        self.args.extend(opts)
        self.args.extend(['--definedvalues', self.pit + '.config'])
        self.args.append(self.pit)
        self.args.append(self.test)

    def run(self):
        if (self.platform != 'all') and\
                (self.platform != get_platform()) and\
                (not get_platform() in self.platform):
            self.status = "skip"
            return False
        if self.setup:
            self.setup()
        self.build_cmd()
        self.cmd = self._show_cmd()
        print "running %s" % self.cmd
        if get_platform() == 'win':
            output = sys.stdout
        else:
            output = PIPE
        temp_dir = os.path.join('.', '_tmp_' + str(os.getpid()))
        timeout_counter = 0
        if os.path.exists(temp_dir):
            shutil.rmtree(temp_dir)
        os.mkdir(temp_dir)
        sout = open(os.path.join(temp_dir, 'sout'), 'w')
        serr = open(os.path.join(temp_dir, 'serr'), 'w')
        self.proc = Popen(self.args, stdout=sout, stderr=serr)
        if self.timeout > 0:
            while (self.proc.poll() == None) and\
                    (timeout_counter < (self.timeout * 12)):
                time.sleep(5) # in seconds
                timeout_counter += 1
        else:
            self.proc.wait()
        if self.proc.poll() == None:
            # kill the process if it's still running
            self.proc.kill()
            self.is_timed_out = True
        sout.close()
        serr.close()
        self.stdout = open(os.path.join(temp_dir, 'sout'), 'r').read()
        self.stderr = open(os.path.join(temp_dir, 'serr'), 'r').read()

        self.pid = self.proc.pid
        self.returncode = self.proc.returncode
        self.hasrun = True
        if bool(self.proc.returncode):
            self.status = "fail"
        else:
            self.status = "pass"
        if self.status == "fail":
            self.log_output()
        if self.teardown:
            self.teardown()
        self.clean_up()

    def log_output(self):
        assert self.hasrun, "Cannot log before running"
        if not os.path.exists(self.logdir):
            os.mkdir(self.logdir)
        errlog = open(os.path.join(self.logdir, 'errors-%s_%s' % (self.name,
                                                                  self.test)),
                      'w')  # this will fail if no permissions
        errlog.write("ran %s\n" % ' '.join(arg for arg in self.args))
        if self.is_timed_out:
            errlog.write("Process timed out after %d minutes\n" %
                         self.timeout)
        else:
            errlog.write("Process exited with code %d\n" % self.returncode)
        errlog.write("#" * 79)
        errlog.write(self.stderr)
        del(self.stderr)
        errlog.close()
        output = open(os.path.join(self.logdir, 'out-%s_%s' % (self.name,
                                                               self.test)),
                      'w')  # this will fail if no permissions
        output.write(self.stdout)
        del(self.stdout)
        del(self.proc)
        output.close()

    def clean_up(self):
        if "stderr" in self.__dict__:
            del(self.stderr)
        if "stdout" in self.__dict__:
            del(self.stdout)
        if "proc" in self.__dict__:
            del(self.proc)



###############################################################################
# setup stuff
###############################################################################


def get_platform():
    plat = sys.platform
    if plat in ('win32', 'cli', 'cygwin'):
        return 'win'
    if plat in ['powerpc', 'darwin']:
        return 'osx'
    return re.split('\d+$', plat)[0]


def test(**kw):
    global all_tests
    assert "name" in kw, '"name" is a required value'
    name = kw["name"]
    del(kw["name"])
    if not name in all_tests:
        all_tests[name] = [kw]
    else:
        all_tests[name].append(kw)


def define(**kw):
    global all_defines
    assert "name" in kw, '"name" is a required value'
    name = kw["name"]
    del(kw["name"])
    all_defines[name] = kw


def get_tests(target, base_config):
    #this is horrible and should die in a fire...
    name = target["file"][:-4]
    pit = os.path.join(target["path"], target["file"])
    my_tests = []
    if name in all_tests:
        for test_def in all_tests[name]:
            my_tests.append(PeachTest(pit, base_config, **test_def))
    else:
        my_tests.append(PeachTest(pit, base_config))
    if name in all_defines:
        for test in my_tests:
            test.update_defines(**all_defines[name])
    return my_tests


if not __name__ == "__main__":
    #this is not efficent
    for filename in os.listdir(EXECPATH):
        if filename[-3:] == ".py" and filename[0] not in ['.', '_']:
            execfile(os.path.join(EXECPATH, filename))
