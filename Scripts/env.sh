#!/bin/sh
echo "postbuild script begin"
env | grep -e 'TEST' -e 'BUILD'
