#!/usr/bin/env bash

# set keys
sed -i '' "s/public const string ENCRYPTION_KEY = \"\"/public const string ENCRYPTION_KEY = \"$ENCRYPTION_KEY\"/g" $APPCENTER_SOURCE_DIRECTORY/Sensus.Shared/SensusServiceHelper.cs
sed -i '' "s/public const string APP_CENTER_KEY_IOS = \"\"/public const string APP_CENTER_KEY_IOS = \"$APP_CENTER_KEY_IOS\"/g" $APPCENTER_SOURCE_DIRECTORY/Sensus.Shared/SensusServiceHelper.cs
