#!/bin/sh
echo "postbuild script begin"
env | grep -e 'TEST_' -e 'UCB_BUILD'
find ../../.. -name ".gradle"
find ../../.. -name ".gradle" | xargs ls -l
