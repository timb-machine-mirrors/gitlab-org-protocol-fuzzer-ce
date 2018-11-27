#!/bin/bash

set -e

export PATH=$PATH:/usr/local/bin

./waf configure --strict --verbose --buildtag=0.0.0
./waf build --strict --verbose --variant=linux
./waf install --strict --verbose --variant=linux
./waf pkg --strict --verbose --variant=linux

# end
