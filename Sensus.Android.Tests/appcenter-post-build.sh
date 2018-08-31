#!/usr/bin/env bash

# build the UI test project
msbuild /p:Configuration=Release $APPCENTER_SOURCE_DIRECTORY/Sensus.Android.Tests.AppCenter/Sensus.Android.Tests.AppCenter.csproj

# log in to the app center
appcenter login --token $TEST_CLOUD_API_TOKEN

# get devices for test depending on branch
DEVICES=""

if [ "$APPCENTER_BRANCH" == "develop-ui-test-single-device" ] ; then
    
    DEVICES="uva-predictive-technology-lab/single-android-device"

elif [ "$APPCENTER_BRANCH" == "develop-ui-test-lmco-devices" ] ; then

    DEVICES="uva-predictive-technology-lab/lmco-android-test-devices"

else

    echo "Unrecognized branch:  $APPCENTER_BRANCH"
    exit 1

fi

# submit test -- don't quote the --app-path value in order to use wildcard matching
appcenter test run uitest --app "uva-predictive-technology-lab/sensus-android" --devices "$DEVICES" --app-path $APPCENTER_OUTPUT_DIRECTORY/*.apk --test-series "master" --locale "en_US" --build-dir "$APPCENTER_SOURCE_DIRECTORY/Sensus.Android.Tests.AppCenter/bin/Release" --async
