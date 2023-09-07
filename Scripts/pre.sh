#!/bin/sh
echo "prebuild script begin"

df -h /
df --help

ls /

pwd | rev

export TEST_ENV_VAR="abc"
export TEST_ENV_EXPORT="hello world"
export UCB_BUILD_NUMBER=123

export VARIABLE_NAME="HI THERE"
echo "::mask-value::$VARIABLE_NAME"
echo "::mask-value::SOME_VALUE_TO_HIDE"
echo "There should be stars in place of SOME_VALUE_TO_HIDE and $VARIABLE_NAME"

env | grep -e 'TEST_' -e 'UCB_BUILD' -e 'VARIABLE_NAME'

echo "TEST_ENV_EXPORT=$TEST_ENV_EXPORT" >> "$DEVOPS_ENV"
echo "UCB_BUILD_NUMBER=$UCB_BUILD_NUMBER" >> "$DEVOPS_ENV"
