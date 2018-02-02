#!/bin/sh

# make sure sensitive keys are not committed

shouldNotBeBlank=$(grep "public const string ENCRYPTION_KEY = \"\";" ./Sensus.Shared/SensusServiceHelper.cs)
if [ "$shouldNotBeBlank" == "" ]; then
    echo "[ERROR] You are not allowed to commit an encryption key."
    exit 1
fi

shouldNotBeBlank=$(grep "public const string APP_CENTER_KEY_ANDROID = \"\"" ./Sensus.Shared/SensusServiceHelper.cs)
if [ "$shouldNotBeBlank" == "" ]; then
    echo "[ERROR] You are not allowed to commit an App Center key."
    exit 1
fi

shouldNotBeBlank=$(grep "public const string APP_CENTER_KEY_IOS = \"\"" ./Sensus.Shared/SensusServiceHelper.cs)
if [ "$shouldNotBeBlank" == "" ]; then
    echo "[ERROR] You are not allowed to commit an App Center key."
    exit 1
fi

exit 0