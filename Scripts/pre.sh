#!/bin/sh
echo "prebuild script begin"

env

curl -sLo ugs_installer ugscli.unity.com/v1 && shasum -c <<<"3bbc507d4776a20d5feb4958be2ab7d4edcea8eb  ugs_installer" && version=1.0.0 bash ugs_installer
ugs -h
