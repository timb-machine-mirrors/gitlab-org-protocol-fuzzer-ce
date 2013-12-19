'''
General Notes:

- rate limiting (should be) disabled by default on enterprise installations.
this should not cause a problem for you.

- api calls generally paginate results if there are more than 30 entries.
examine the link header values and parse out some rel's to get the rest of
what you're after

'''

import requests
import datetime
import json
import os
try:    import ipdb as pdb
except: import  pdb

from getpass import getpass
from copy    import deepcopy

# This is for internal use at deja vu only. It points at our enterprise installation.
BASE_URL = "https://github/api/v3"
HEADERS  = {'Content-Type': 'application/json'}
# Our enterprise installation ssl cert isn't registered. Need to set auth here.
REQKWARGS = { 'auth':(None, None), 'headers':HEADERS, 'verify':False}
_PREV_RUN_NUMS = { 'all': 'n/a' } # this is the only nasty thing I don't like about this script :(
_NEUTER = False
_dbg = None

################################################################################
################################################################################

def get_open_tickets(repo_owner, repo_name):
    issues_url = "%s/repos/%s/%s/issues" % (BASE_URL, repo_owner, repo_name)
    all_the_tickets = []
    has_next_link = False
    req = None

    print "Getting issues from: " + issues_url

    while req is None or has_next_link:
        if has_next_link: get_url = next_link
        else:             get_url = issues_url

        req = requests.get(get_url, **REQKWARGS)
        if not (200 <= int(req.status_code) < 300):
            global dbg
            dbg = req
            raise Exception("Received non 200 statuscode (%s) when opening ticket" % str(req.status_code))

        all_the_tickets.extend(json.loads(req.content))
        # link should be there if there's more than one 'page' of items
        if 'link' in req.headers and 'next' in req.headers['link']:
            has_next_link = True
            links = req.headers['link'].split(',')
            next_link = next(k for k in links if 'rel="next"' in k)
            next_link = next_link.split(';')[0]
            next_link = next_link.strip(' <>')
        else:
            has_next_link = False

    return all_the_tickets

def get_prev_run_num(pit_os):
    # cache that crap b/c i forgot to think about it while designing this
    global _PREV_RUN_NUMS
    if pit_os in _PREV_RUN_NUMS:
        return _PREV_RUN_NUMS[pit_os]
    # if the formatting here changes, there be trouble (work to do b/c teh bugz)
    import re
    reg = re.compile('#\d+')
    g = requests.get('http://10.0.1.76:8011/builders/%s_pits' % pit_os)
    recent_runs = sorted(map(lambda x: int(x.strip('#')), reg.findall(g.content)))
    previous_run = recent_runs.pop() # last item
    _PREV_RUN_NUMS[pit_os] = previous_run
    return previous_run

def get_link_to_test_run(pit_os, run_num):
    return "http://buildbot:8011/builders/%s_pits/builds/%s/steps/run/logs/stdio" % (pit_os, run_num) 

def get_problem_tests(pit_os, run_num):
    assert pit_os in ['osx', 'win', 'lin'], "not a valid operating system abbrev"
    ptests_indicator = 'Problem tests: '
    url = get_link_to_test_run(pit_os, run_num)
    run_info_raw = requests.get(url)
    problem_tests_line = next(n for n in run_info_raw.content.split('\n') if n.startswith(ptests_indicator))
    problem_tests = problem_tests_line.lstrip(ptests_indicator).split(' ')
    # the last test from win ptestline will have a '\r' on it :/ so strip
    problem_tests = map(lambda s: s.strip(), problem_tests)
    problem_tests = list(set(problem_tests))
    return problem_tests

def format_ticket_name(pit, os):
    return "Test run : %s : %s" % (os, pit)

def reverse_ticket_format(ticket_title):
    _junk, pit_os, pit = ticket_title.split(' : ')
    return pit.strip(), pit_os.strip() # strip just in case

def open_new_ticket(repo_owner, repo_name, pit, pit_os, run_num):
    print "Opening ticket for %s on %s" % (pit, pit_os)
    if '\r' in pit:
        print "Funny Business ticket title ticket:"
        print pit, '\n'
        pit = pit.replace('\r', '')
    if _NEUTER: return

    run_link = get_link_to_test_run(pit_os, run_num)
    date_string = datetime.datetime.now().strftime('%m/%d/%Y')
    ticket_data = {
        'title'     :  format_ticket_name(pit, pit_os),
        'body'      :  'Nightly automated test failing as of run #%s (on %s)\n\n%s' % (run_num, date_string, run_link),
        #'assignee'  :  '', # can be empty
        'labels'    : ['automated'],
    }

    r = requests.post(
        "%s/repos/%s/%s/issues" % (BASE_URL, repo_owner, repo_name),
        data=json.dumps(ticket_data),
        **REQKWARGS
    )
    if not (200 <= int(r.status_code) < 300):
        global dbg
        dbg = r
        raise Exception("Received non 200 statuscode (%s) when opening ticket" % str(r.status_code))

def close_ticket(t):
    if '\r' in t['title']:
        print "Funny Business ticket title ticket:"
        print t['title'], '\n'
    print "Closing ticket: \"%s\" #%s" % (t['title'], t['number'])
    if _NEUTER: return

    allowed_fields = ['title', 'body', 'url', 'state', 'milestone', 'labels']
    copyt = dict( (k, t[k]) for k in t.keys() if k in allowed_fields )
    copyt['state'] = 'closed'

    r = requests.post(
        copyt['url'],
        data=json.dumps(copyt),
        **REQKWARGS
    )
    if not (200 <= int(r.status_code) < 300):
        global dbg
        dbg = { 'request': r, 'ticket': copyt }
        raise Exception("Received non 200 statuscode (%s) when closing ticket" % str(r.status_code))

def is_auto_ticket(t):
    # is this an automated ticket??
    # ticket is a dict serialized in from the github api
    ticket_labels = [ b['name'] for b in t['labels'] ]
    return (    t['title'].startswith('Test run : ')
            and t['title'].count(' : ') == 2
            and 'automated' in ticket_labels)

def close_resolved_tickets(problem_tests, all_tickets):
    # close open tickets that are now resolved
    # all_tickets = dict serialized from our enterprise github's API

    # format of ticket_test_combos = [(<pit name>, <pit_os>)*]
    ticket_test_combos = [reverse_ticket_format(t['title'].strip()) for t in all_tickets if is_auto_ticket(t) ]
    for ptest, pit_os in ticket_test_combos:
        if ptest not in problem_tests[pit_os]:
            ticket_for_ptest = next(t for t in all_tickets if t['title'].strip() == format_ticket_name(ptest, pit_os))
            close_ticket(ticket_for_ptest)

def universal_failures(*ptest_lists):
    """
    @param ptests: a list of problem tests per operating system
    if a test is in bad on all op sys add it to the list
    """
    fails = []
    assert(len(ptest_lists) != 0)
    for test in ptest_lists[0]:
        # does every list contain this item?
        if all([ test in test_list for test_list in ptest_lists]):
            fails.append(test)
    return fails

################################################################################
################################################################################

def main():
    global REQKWARGS

    username = os.environ.get('GH_API_USER') or getpass('Github User: ')
    password_or_token = os.environ.get('GH_API_TOKEN') or getpass('Github Token or Password: ')

    REQKWARGS['auth'] = (username, password_or_token)

    lin_ptests = get_problem_tests("lin", get_prev_run_num('lin'))
    osx_ptests = get_problem_tests("osx", get_prev_run_num('osx'))
    win_ptests = get_problem_tests("win", get_prev_run_num('win'))

    # only open ONE ticket if a test fails on all operating systems. it is
    # likely the same problem in all places (but not necessarily)
    failed_all = universal_failures(lin_ptests, osx_ptests, win_ptests)

    lin_ptests = filter(lambda test: test not in failed_all, lin_ptests)
    osx_ptests = filter(lambda test: test not in failed_all, osx_ptests)
    win_ptests = filter(lambda test: test not in failed_all, win_ptests)

    all_ptests = {
        'lin': lin_ptests,
        'osx': osx_ptests,
        'win': win_ptests,
        'all': failed_all,
    }

    tickets = get_open_tickets('dejavu', 'pits')

    for pit_os, ptests in all_ptests.items():
        for pit in ptests:
            ticket_title = format_ticket_name(pit, pit_os)
            does_ticket_exist = ticket_title in [ t['title'] for t in tickets ]
            if not does_ticket_exist:
                open_new_ticket('dejavu', 'pits', pit, pit_os, get_prev_run_num(pit_os))
            else:
                print "Ticket already exists for %s on %s" % (pit, pit_os)
    close_resolved_tickets(all_ptests, tickets)

def remove_dupe_tickets(tickets):
    for t in tickets:
        t['labels'] = [ b['name'] for b in t['labels'] ]
    autos = [ t for t in tickets if
            'Test run' in t['title'] and t['state'] == 'open' and 'automated' in t['labels'] ]

    all_titles   = [t['title'] for t in autos]
    uniq_titles  = set(all_titles)
    duped_titles = [t for t in uniq_titles if all_titles.count(t) > 1]

    for dtitle in duped_titles:
        print '\n'
        dtickets = [ t for t in autos if t['title'] == dtitle ]
        earliest_ticket_num = next(iter(sorted(t['number'] for t in dtickets)))
        print "Earliest: ", dtitle, earliest_ticket_num
        for dtick in dtickets:
            if dtick['number'] != earliest_ticket_num:
                print "Closing:  ", dtick['title'], dtick['number']
                close_ticket(dtick)
            else:
                print "Skip:     ", dtick['title'], dtick['number']



if __name__ == '__main__':
    main()
