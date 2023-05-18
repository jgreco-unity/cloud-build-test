#!/bin/sh
echo "prebuild script begin"

export TEST_ENV_VAR="abc"
export TEST_ENV_EXPORT="hello world"

env | grep -e 'TEST' -e 'BUILD'

echo "TEST_ENV_EXPORT=$TEST_ENV_EXPORT" >> "$DEVOPS_ENV"
echo "UCB_BUILD_NUMBER=123" >> "$DEVOPS_ENV"
