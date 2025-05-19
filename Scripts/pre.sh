#!/bin/sh
echo "prebuild script begin"
curl --location 'https://build-api-stg.cloud.unity3d.com/api/v1/orgs/20066339085561/projects/d6a69f98-ad69-4351-84c5-732759c78803/cloud-build/setup/buildTarget/env/builds/67/builder/status/postbuild' \
--header 'Content-Type: application/json' \
--header "Authorization: $API_KEY" \
--data '{
    "finishTime": "2025-05-19T23:56:07"
}'
echo "prebuild script end"
