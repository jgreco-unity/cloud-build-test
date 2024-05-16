#!/bin/sh
echo "prebuild script begin"
$PLASTIC_CM_PATH version
ls /Applications | grep -i plastic
find /Applications/PlasticSCM-11.0.16.8551.app -type f -name "cm"
echo "env"
env
echo "prebuild script end"
