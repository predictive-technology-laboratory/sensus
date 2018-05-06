#!/usr/bin/env bash

# set keys for release builds
if [ "$APPCENTER_BRANCH" == "master" ] || [ "$APPCENTER_BRANCH" == "master-ci" ] || [ "$APPCENTER_BRANCH" == "ios-ad-hoc" ] ; then

  sed -i '' "s/public const string ENCRYPTION_KEY = \"\"/public const string ENCRYPTION_KEY = \"$ENCRYPTION_KEY\"/g" $APPCENTER_SOURCE_DIRECTORY/Sensus.Shared/SensusServiceHelper.cs
  sed -i '' "s/public const string APP_CENTER_KEY_IOS = \"\"/public const string APP_CENTER_KEY_IOS = \"$APP_CENTER_KEY_IOS\"/g" $APPCENTER_SOURCE_DIRECTORY/Sensus.Shared/SensusServiceHelper.cs
  sed -i '' "s/public const string BUILD_ID = \"\"/public const string BUILD_ID = \"$APPCENTER_BUILD_ID\"/g" $APPCENTER_SOURCE_DIRECTORY/Sensus.Shared/SensusServiceHelper.cs

fi