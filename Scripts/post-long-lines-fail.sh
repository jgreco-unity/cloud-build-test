#!/bin/sh
# Prints a hidden and a visible line past the log's 2,500-character display cut, then fails the build.
# The filler is "a": a run like "-xxxxxxxxxx" matches the build service's hide rule for ls -l permissions
long=$(printf '%3000s' '' | tr ' ' 'a')
echo "hidden_$long #filterme"
echo "visible_$long"
echo "postbuild script exiting with status 1"
exit 1
