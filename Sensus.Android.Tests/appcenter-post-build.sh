#!/usr/bin/env bash

# build the UI test project
msbuild /p:Configuration=Release $APPCENTER_SOURCE_DIRECTORY/Sensus.Android.Tests.AppCenter/Sensus.Android.Tests.AppCenter.csproj

# log in to the app center
appcenter login --token $TEST_CLOUD_API_TOKEN

# submit test
appcenter test run uitest --app "uva-predictive-technology-lab/sensus-android" --devices "uva-predictive-technology-lab/single-android-device" --app-path $APPCENTER_OUTPUT_DIRECTORY/*.apk --test-series "master" --locale "en_US" --build-dir "$APPCENTER_SOURCE_DIRECTORY/Sensus.Android.Tests.AppCenter/bin/Release" --async
