#!/bin/sh
echo "postbuild script begin"
echo "clientconf:"
cat /opt/workspace/workspace/jason-test-ucb.azure.mac-plastic/client.conf
echo "tokensconf:"
cat /opt/workspace/workspace/jason-test-ucb.azure.mac-plastic/tokens.conf
echo "cloudregionsconf:"
cat /opt/workspace/workspace/jason-test-ucb.azure.mac-plastic/cloudregions.conf
echo "postbuild script end"
