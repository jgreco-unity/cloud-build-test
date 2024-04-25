#!/bin/sh
echo "prebuild script begin"
ssh -V
ls ~/.ssh/config
cat ~/.ssh/config
echo "prebuild script end"
