#!/bin/bash

set -e

export PATH=$PATH:/usr/local/bin

rm -rf slag
./docs/whatsnew.py

./waf configure --strict --verbose --buildtag=0.0.0
./waf build --strict --verbose --variant=doc
./waf install --strict --verbose --variant=doc
./waf pkg --strict --verbose --variant=doc
./waf zip --strict --verbose --variant=doc

#
#archiveArtifacts artifacts: 'output/doc.zip'

# end
