#!/bin/sh
echo "prebuild script begin"

ls /

find /Applications/Unity.app/ -type d -name AndroidPlayer

pwd | rev

export TEST_ENV_VAR="abc"
export TEST_ENV_EXPORT="hello world"
export UCB_BUILD_NUMBER=123

export VARIABLE_NAME="HI THERE"
echo "::mask-value::$VARIABLE_NAME"
echo "::mask-value::SOME_VALUE_TO_HIDE"
echo "There should be stars in place of SOME_VALUE_TO_HIDE and $VARIABLE_NAME"

env

echo "TEST_ENV_EXPORT=$TEST_ENV_EXPORT" >> "$DEVOPS_ENV"
echo "UCB_BUILD_NUMBER=$UCB_BUILD_NUMBER" >> "$DEVOPS_ENV"
