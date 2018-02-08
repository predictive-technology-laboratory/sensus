#!/usr/bin/env bash

# set keys for builds from master, as we're going to release them to the store.
if [ "$APPCENTER_BRANCH" == "master" ] || [ "$APPCENTER_BRANCH" == "master-ci" ] ; then

  sed -i '' "s/public const string ENCRYPTION_KEY = \"\"/public const string ENCRYPTION_KEY = \"$ENCRYPTION_KEY\"/g" $APPCENTER_SOURCE_DIRECTORY/Sensus.Shared/SensusServiceHelper.cs
  sed -i '' "s/public const string APP_CENTER_KEY_ANDROID = \"\"/public const string APP_CENTER_KEY_ANDROID = \"$APP_CENTER_KEY_ANDROID\"/g" $APPCENTER_SOURCE_DIRECTORY/Sensus.Shared/SensusServiceHelper.cs

fi