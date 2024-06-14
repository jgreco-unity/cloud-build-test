#!/bin/sh
echo "prebuild script begin"
ugs --version
ls /opt/Unity/windows/scm/plastic/
/opt/Unity/windows/scm/plastic/PlasticSCM-11.0.16.8622/client/cm.exe version
echo "env"
env
echo "prebuild script end"
