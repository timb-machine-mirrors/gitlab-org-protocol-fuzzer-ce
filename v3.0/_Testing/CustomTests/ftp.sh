#!/bin/bash

ftp -inv $@ <<EOF
user hello world
cd /test/test/test
bye
EOF
