#!/bin/sh
# Prints a hidden and a visible line past the log's 2,500-character display cut, then fails the build
long=$(printf '%3000s' '' | tr ' ' 'x')
echo "hidden-$long #filterme"
echo "visible-$long"
echo "postbuild script exiting with status 1"
exit 1
