#!/bin/sh
echo "postbuild script begin"
env | grep -e 'TEST_ENV' -e 'UCB_BUILD'
