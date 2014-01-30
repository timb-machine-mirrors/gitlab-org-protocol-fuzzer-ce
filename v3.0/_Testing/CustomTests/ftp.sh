#!/bin/sh

ftp -n 10.0.1.23 <<END_SCRIPT
quote USER telnetuser
quote PASS telnetuserp455
ls
quit
END_SCRIPT
exit 0
