#!/bin/sh
echo "prebuild script killing itself with SIGTERM"
kill -TERM $$
