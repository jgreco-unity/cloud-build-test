##!/usr/bin/env bash

echo "prebuild script begin"

## Source profile
. ~/.profile

pwd

SCRIPTS=/cygdrive/c/Users/buildbot/*.sh
ls $SCRIPTS
for f in $SCRIPTS; do
    echo "Printing $f"
    cat "$f"
    echo "-----"
done
