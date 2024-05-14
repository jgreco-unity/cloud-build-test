#!/bin/sh
echo "prebuild script begin"
$PLASTIC_CM_PATH version
ls /Applications | grep -i plastic
echo "env"
env
echo "prebuild script end"
