#!/bin/sh
echo "prebuild script begin"
ls ..
ls $ARTIFACT_DIRECTORY
echo "env"
env
echo "prebuild script end"
