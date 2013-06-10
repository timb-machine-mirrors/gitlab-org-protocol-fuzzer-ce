#!/usr/bin/env python
import sys
import os
from copy import copy
from types import MethodType
from subprocess import Popen, PIPE

IS_WIN = os.name == 'nt'
#resolution order is ./peach, last arg, PEACH env var
PEACH_BIN = 'peach.bat' if IS_WIN else 'peach'
PEACH_BIN = os.environ.get('PEACH') 
PEACH_OPTS = []
BASE_DEFINES = {"Path":"."}
EXECPATH = os.path.join(os.path.dirname(__file__), 'CustomTests')

all_tests = {}
all_defines = {}

class PeachTest:
    # defines should probably be generated on the fly. we can come
    # back to this.
    
    def __init__(self, pit, logdir, cwd=None, base_opts=PEACH_OPTS, 
                 setup=None, teardown=None, extra_opts=None, 
                 defines=BASE_DEFINES):
        assert os.getuid() == 0, "must be root to run this"
        self.pit = pit
        self.name = os.path.basename(pit)[:-4] #pit - '.xml'
        self.args = [PEACH_BIN]
        self.base_opts = copy(base_opts)
        self.extra_opts = copy(extra_opts)
        self.cwd = cwd or os.getcwd()
        self.logdir = logdir
        self.hasrun = False
        self.setup = setup and MethodType(setup, self, PeachTest)
        self.teardown = setup and MethodType(teardown, self, PeachTest)
        self.defines = copy(defines)
        if self.setup:
            self.setup() 

    def color_text(self, code, text):
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

    def build_cmd(self, args=None):
        if not args:
            args = copy(self.args)
        opts = copy(self.base_opts)
        if self.extra_opts:
            opts.extend(self.extra_opts)
        if self.defines:
            opts.extend(self.render_defines())
        args.extend(opts)
        args.extend(['--definedvalues', self.pit + '.config'])
        args.append(self.pit)
        self.cmd = ' '.join(arg for arg in args)
        return args

    def run(self, args=None):
        args = self.build_cmd(args)
        print "running %s" % self.cmd
        self.proc = Popen(args, stdout=PIPE, stderr=PIPE)
        self.stdout, self.stderr = self.proc.communicate()
        self.pid = self.proc.pid
        self.returncode = self.proc.returncode
        self.failed = bool(self.proc.returncode)
        self.hasrun = True
        if self.failed:
            print "...and it "+self.color_text("91","FAILED!")
        else:
            print self.color_text("92","SUCCESS!")

    def run_first_pass(self):
        args = list(self.args)
        args.append('-1')
        self.run(args)

    def run_long_random(self):
        self.run()

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
        del(self.stderr)
        del(self.stdout)
        del(self.proc)
        if self.teardown:
            self.teardown()


###############################################################################
# helper 
###############################################################################


def get_ipv4():
    pass


###############################################################################
# setup stuff 
###############################################################################


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


def get_test(target, logdir):
    name = target["file"][:-4]
    pit = os.path.join(target["path"], target["file"])
    if name in all_tests:
        test = PeachTest(pit, logdir, **all_tests[name])
    else:
        test = PeachTest(pit, logdir)
    if name in all_defines:
        test.update_defines(**all_defines[name])
    return test


for filename in os.listdir(EXECPATH):
    if filename[-3:] == ".py" and filename[0] not in ['.', '_']:
        execfile(os.path.join(EXECPATH, filename))

