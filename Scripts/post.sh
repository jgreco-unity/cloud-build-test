#!/bin/sh
echo "postbuild script begin"
pwd
find . -name 'edit_mode_tests.xml'
find . -name 'play_mode_tests.xml'
echo "postbuild script end"
