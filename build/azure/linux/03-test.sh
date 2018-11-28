#!/bin/bash

# Linux

set -ev

echo "*** Running tests ***"

bin_folder = "output/linux_x86_64_debug/bin/"
result = "output/nunit-linux_x86_64_debug"
nunit_error = false
include=Quick

TESTDLLS=[
    "Peach.Core.Test.dll",
    "Peach.Pro.Test.OS.Linux.dll",
    "Peach.Pro.Test.WebApi.exe",
    "Peach.Pro.Test.dll"
    ]

for it in $TESTDLLS; do

    echo "*** Running tests for '$it' ***"

    mono -debug \
        ${bin_folder}nunit3-console.exe \
        --labels=All \
        --where "cat == ${include}" \
        --result "${result}_${it}.xml" \
        ${bin_folder}${it}
    
    $ret=$_

    print "Nunit exit code: $ret"

    #if $ret < 0
    #{
    #    error("nunit test runner failed") 
    #}

    if ["$ret" != "0"] then
        nunit_error = true
    fi

    sed -i -e 's/name=\"/name=\"${target}./g' ${result}_${it}.xml

done

# end
