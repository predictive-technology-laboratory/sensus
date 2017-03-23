#!/bin/sh

# make sure sensitive keys are not committed

currentKey=$(grep "public const string ENCRYPTION_KEY = \"\";" ./Sensus.Shared/SensusServiceHelper.cs)
if [ "$currentKey" != "" ]; then
    echo "[WARNING] You are building an iOS app store release without an encryption key."
    exit 1
fi

currentKey=$(grep "public const string XAMARIN_INSIGHTS_APP_KEY = \"\"" ./Sensus.Shared/SensusServiceHelper.cs)
if [ "$currentKey" != "" ]; then
    echo "[WARNING] You are building an iOS app store release without a Xamarin Insights key."
    exit 1
fi

echo "Keys present. Ready to build."

exit 0