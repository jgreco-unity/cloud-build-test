#!/bin/sh
export TEST_ENV_VAR=abc
env
echo "test_env=here i am" >> "$DEVOPS_ENV"
echo "UCB_BUILD_NUMBER=123" >> "$DEVOPS_ENV"
