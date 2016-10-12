#!/bin/sh

# make sure sensitive keys are not committed

currentKey=$(grep "public const string ENCRYPTION_KEY = \"\";" ./Sensus.Shared/SensusServiceHelper.cs)
if [ "$currentKey" == "" ]; then
    echo "[ERROR] You are not allowed to commit an encryption key."
    exit 1
fi

currentKey=$(grep "protected const string XAMARIN_INSIGHTS_APP_KEY = \"\"" ./Sensus.Shared/SensusServiceHelper.cs)
if [ "$currentKey" == "" ]; then
    echo "[ERROR] You are not allowed to commit a Xamarin Insights API key."
    exit 1
fi

exit 0