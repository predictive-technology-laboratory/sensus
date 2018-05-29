#!/usr/bin/env bash

# build the UI test project
msbuild /p:Configuration=Debug $APPCENTER_SOURCE_DIRECTORY/Sensus.iOS.Tests.AppCenter/Sensus.iOS.Tests.AppCenter.csproj

# log in to the app center
appcenter login --token $TEST_CLOUD_API_TOKEN

# submit test

devices=""

if [ "$APPCENTER_BRANCH" == "develop-ui-test-single-device" ] ; then
    
    devices="uva-predictive-technology-lab/single-ios-device"

elif [ "$APPCENTER_BRANCH" == "develop-ui-test-lmco-devices" ] ; then

    devices="uva-predictive-technology-lab/lmco-ios-test-devices"

else

    echo "Unrecognized branch:  $APPCENTER_BRANCH"
    exit 1

fi

appcenter test run uitest --app "uva-predictive-technology-lab/sensus-iOS-1" --devices "$devices" --app-path "$APPCENTER_OUTPUT_DIRECTORY/*.ipa" --test-series "master" --locale "en_US" --build-dir "$APPCENTER_SOURCE_DIRECTORY/Sensus.iOS.Tests.AppCenter/bin/Debug" --async
