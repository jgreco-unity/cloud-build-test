#!/bin/sh
echo "prebuild script begin"
ssh -V
ls -l ~/.ssh/config
echo '\n    HostKeyAlgorithms +ssh-rsa' >> ~/.ssh/config
cat ~/.ssh/config
echo "prebuild script end"
