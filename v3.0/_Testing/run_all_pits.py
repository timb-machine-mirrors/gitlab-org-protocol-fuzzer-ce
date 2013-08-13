#!/usr/bin/env python
###############################################################################
# run this from the v3.0 directory so the pathing is correct
# don't run this from _Testing directly
#
# python needs to be on the system path
###############################################################################

import shutil
import sys
import os
from os.path import join as pjoin
from subprocess import Popen

# in the future this should change to a timespan instead of a count
TIMES_TO_ITERATE = 5
# note the double slash before bin
PEACH    = os.path.expanduser(
    '~\work\code\peach\output\win_x64_release\\bin\Peach.exe')
CWD      = os.getcwd()
TESTER   = pjoin('.', '_Testing', 'tester.py')
PIT_DIRS = ['Application', 'Audio', 'Image', 'Net', 'Video']
HAS_A_PIT_FAILED = False
TMP_DIR = os.path.abspath(pjoin('.', '__pit_run_tmpfiles'))
RC = 0
FAILED_OUTPUTS = []

if not os.path.exists(TESTER):
    raise Exception('tester.py not found at', os.path.abspath(TESTER))
if not os.path.exists(PEACH):
    raise Exception('Peach.exe not found at', os.path.abspath(PEACH))

if os.path.exists(TMP_DIR):
    shutil.rmtree(TMP_DIR)
if not os.path.exists(TMP_DIR):
    os.mkdir(TMP_DIR)

for pd in PIT_DIRS:
    d = pjoin(CWD, pd)
    ls = os.listdir(d)
    pits = filter(lambda x: x.endswith('.xml'), ls)
    for pit in pits:
        # print '\n' * 5
        print "fuzzing with ", pit
        pit_path = pjoin('.', pd, pit)
        pto = pjoin(TMP_DIR, pit + '.out')
        pte = pjoin(TMP_DIR, pit + '.err')
        pit_tmp_out = open(pto, 'w')
        pit_tmp_err = open(pte, 'w')
        proc = Popen(
            ['python', TESTER,
             '-p',     PEACH,
             '-c',     str(TIMES_TO_ITERATE),
             pit_path
            ],
            stdout=pit_tmp_out,
            stderr=pit_tmp_err)
        proc.wait()
        pit_tmp_out.close()
        pit_tmp_err.close()
        pit_tmp_out = open(pjoin(TMP_DIR, pit + '.out'), 'r')
        # need to grep the last line of stdout to check it
        for line in pit_tmp_out:
            pass
        if line.startswith("SUCCESS!"):
            # the test probably succeeded, we really need a better
            # indicator though
            pass
        else:
            print "Bailed Test", pit
            HAS_A_PIT_FAILED = True
            FAILED_OUTPUTS.append([pit, pto, pte])
        pit_tmp_out.close()

if HAS_A_PIT_FAILED:
    RC = 1
    for (pit, stdout, stderr) in FAILED_OUTPUTS:
        print "\n\n============================================================"
        print "Fuzzing run error using: ", pit
        print "\n ----- Standard Out ----- \n"
        with open(stdout, 'r') as f:
            for line in f:
                print line
        print "\n ----- Standard Error ----- \n"
        with open(stderr, 'r') as f:
            for line in f:
                print line

sys.exit(RC)
