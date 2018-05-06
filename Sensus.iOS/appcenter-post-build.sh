#!/usr/bin/env bash

# create github release for release builds
if [ "$APPCENTER_BRANCH" == "master" ] || [ "$APPCENTER_BRANCH" == "master-ci" || [ "$APPCENTER_BRANCH" == "ios-ad-hoc" ] ; then

  curl -u $GITHUB_USER:$GITHUB_PASS --data "{\"tag_name\": \"Sensus-v$APPCENTER_BUILD_ID\",\"target_commitish\": \"$APPCENTER_BRANCH\",\"name\": \"Sensus release v$APPCENTER_BUILD_ID\",\"body\": \"Release of Sensus version v$APPCENTER_BUILD_ID.\",\"draft\": false,\"prerelease\": false}" https://api.github.com/repos/predictive-technology-laboratory/sensus/releases
  
fi