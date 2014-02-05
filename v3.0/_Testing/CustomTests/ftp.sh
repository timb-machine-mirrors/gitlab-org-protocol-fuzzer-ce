#!/bin/sh

ftp -n $1 <<END_SCRIPT
quote USER telnetuser
quote PASS telnetuserp455
ls
quit
END_SCRIPT
exit 0
