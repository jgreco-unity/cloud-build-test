#!/bin/sh
echo "postbuild script begin"
pwd
echo $PROJECT_DIRECTORY
find . -name 'edit_mode_tests.xml'
find . -name 'play_mode_tests.xml'
edit_mode_test_log=$(<"${PROJECT_DIRECTORY}/Editor/Resources/test_data/edit_mode_tests.xml")
echo "postbuild script end"
