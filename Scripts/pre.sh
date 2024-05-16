#!/bin/sh
echo "prebuild script begin"
$PLASTIC_CM_PATH version
ls /Applications | grep -i plastic
find /Applications/PlasticSCM-11.0.16.8551.app -type f -name "cm"
echo "clientconf:"
cat /opt/workspace/workspace/jason-test-ucb.azure.mac-plastic/client.conf
echo "tokensconf:"
cat /opt/workspace/workspace/jason-test-ucb.azure.mac-plastic/tokens.conf
echo "cloudregionsconf:"
cat /opt/workspace/workspace/jason-test-ucb.azure.mac-plastic/cloudregions.conf
echo "env"
env
echo "prebuild script end"
