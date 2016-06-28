#!/bin/bash

. jenkins.properties

last=$(git describe --match v${RELEASE_VERSION}.* --abbrev=0 | cut -f3 -d.)
next=$((last+1))

echo BUILDTAG=${RELEASE_VERSION}.${next}
echo RELEASE_VERSION=${RELEASE_VERSION}
