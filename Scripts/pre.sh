#!/bin/sh
echo "prebuild script begin"

export TEST_ENV_VAR="abc"
export TEST_ENV_EXPORT="hello world"
export UCB_BUILD_NUMBER=123

env | grep -e 'TEST_ENV' -e 'UCB_BUILD'

echo "TEST_ENV_EXPORT=$TEST_ENV_EXPORT" >> "$DEVOPS_ENV"
echo "UCB_BUILD_NUMBER=$UCB_BUILD_NUMBER" >> "$DEVOPS_ENV"
