#!/bin/sh

# make sure sensitive keys are not committed

currentKey=$(grep "private const string ENCRYPTION_KEY = \"\";" ./SensusService/SensusServiceHelper.cs)
if [ "$currentKey" != "" ]; then
    echo "[WARNING] You are building an iOS app store release without an encryption key."
    exit 1
fi

currentKey=$(grep "protected const string XAMARIN_INSIGHTS_APP_KEY = \"\"" ./SensusService/SensusServiceHelper.cs)
if [ "$currentKey" != "" ]; then
    echo "[WARNING] You are building an iOS app store release without a Xamarin Insights key."
    exit 1
fi

echo "Keys present. Ready to build."

exit 0