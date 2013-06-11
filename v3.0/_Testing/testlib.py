#!/usr/bin/env python
import sys
import os
import re

from copy import copy
from types import MethodType
from subprocess import Popen, PIPE

IS_WIN = os.name == 'nt'
#resolution order is ./peach, last arg, PEACH env var
PEACH_OPTS = []
BASE_DEFINES = {"Path":"."}
EXECPATH = os.path.join(os.path.dirname(__file__), 'CustomTests')

COLOR_CODES = {'red': 31,
               'green': 32,
               'yellow': 92}

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
    
    def __init__(self, pit, config, cwd=None, base_opts=PEACH_OPTS, 
                 setup=None, teardown=None, extra_opts=None, platform='all',
                 defines=BASE_DEFINES):
        assert os.getuid() == 0, "must be root to run this"
        self.status = None
        self.platform = platform
        self.pit = pit
        self.name = os.path.basename(pit)[:-4] #pit - '.xml'
        self.peach = self._get_peach_bin(config.peach)
        self.args = []
        self.count = config.count
        self.base_opts = copy(base_opts)
        self.extra_opts = copy(extra_opts)
        self.cwd = cwd or os.getcwd()
        self.logdir = config.logdir
        self.hasrun = False
        self.setup = setup and MethodType(setup, self, PeachTest)
        self.teardown = setup and MethodType(teardown, self, PeachTest)
        self.defines = copy(defines)

    def _get_peach_bin(self, peach):
        if peach:
            return peach
        peach = os.environ.get('PEACH')
        if peach:
            return peach
        return 'peach.bat' if IS_WIN else 'peach'

    def _show_cmd(self):
        return ' '.join(arg for arg in self.args)

    def color_text(self, color, text):
        code = COLOR_CODES[color]
        #this shouldn't take a code, it should take a name
        if IS_WIN:
            return text
        return "\033["+str(code)+"m"+str(text)+"\033[0m"

    def update_defines(self, **kw):
        for k,v in kw.iteritems():
            self.defines[k] = v
            
    def render_defines(self):
        asopts = []
        for k,v in self.defines.iteritems():
            yield '-D'
            yield '%s=%s' % (k,v)

    def build_cmd(self):
        #lets store args in self so args can be analyzed for each test
        self.args = [self.peach]
        opts = copy(self.base_opts)
        if self.extra_opts:
            opts.extend(self.extra_opts)
        if self.defines:
            opts.extend(self.render_defines())
        if self.count:
            assert type(self.count) is int and self.count > 0,\
                "Count must be a positive integer"
            if self.count == 1:
                opts.append('-1')
            else:
                opts.append('--range=1,%d' % self.count)
        self.args.extend(opts)
        self.args.extend(['--definedvalues', self.pit + '.config'])
        self.args.append(self.pit)

    def run(self):
        if (self.platform != 'all') and\
                (self.platform != get_platform()):
            print "%s was %s " % (self.pit, self.color_text('yellow',"SKIPPED!"))
            self.status = "skip"
            return False
        if self.setup:
            self.setup()
        self.build_cmd()
        self.cmd = self._show_cmd()
        print "running %s" % self.cmd
        self.proc = Popen(self.args, stdout=PIPE, stderr=PIPE)
        self.stdout, self.stderr = self.proc.communicate()
        self.pid = self.proc.pid
        self.returncode = self.proc.returncode
        if bool(self.proc.returncode):
            self.status = "fail"
        else:
            self.status = "pass"
        self.hasrun = True
        if self.status == "fail":
            print "...and it " + self.color_text("red","FAILED!")
        else:
            print self.color_text("green","SUCCESS!")
        if self.teardown:
            self.teardown()

    def log_output(self):
        assert self.hasrun, "Cannot log before running"
        if not os.path.exists(self.logdir):
            os.mkdir(self.logdir)
        errlog = open(os.path.join(self.logdir, 'errors-%s' % self.name),
                      'w') #this will fail if no permissions
        errlog.write("ran %s\n" % ' '.join(arg for arg in self.args))
        errlog.write("Process exited with code %d\n" % self.returncode)
        errlog.write("#" * 79)
        errlog.write(self.stderr)
        del(self.stderr)
        errlog.close()
        output = open(os.path.join(self.logdir, 'out-%s' % self.name),
                      'w') #this will fail if no permissions
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
    all_tests[name] = kw


def define(**kw):
    global all_defines
    assert "name" in kw, '"name" is a required value'
    name = kw["name"]
    del(kw["name"])
    all_defines[name] = kw


def get_test(target, base_config):
    name = target["file"][:-4]
    pit = os.path.join(target["path"], target["file"])
    if name in all_tests:
        test = PeachTest(pit, base_config, **all_tests[name])
    else:
        test = PeachTest(pit, base_config)
    if name in all_defines:
        test.update_defines(**all_defines[name])
    return test


#this is not efficent
for filename in os.listdir(EXECPATH):
    if filename[-3:] == ".py" and filename[0] not in ['.', '_']:
        execfile(os.path.join(EXECPATH, filename))

