#!/bin/bash

last=$(git describe --match ${TAG_PREFIX}${RELEASE_VERSION}.* --abbrev=0 | cut -f3 -d.)
next=$((last+1))

echo ${RELEASE_VERSION}.${next}
