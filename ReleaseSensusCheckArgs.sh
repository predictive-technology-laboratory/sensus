#!/bin/sh

if [ $# -ne 7 ]; then
    echo
    echo "Purpose:  Creates a release of Sensus for Android and iOS, based on the current GitHub branch."
    echo ""
    echo "Usage:  ./ReleaseSensus.sh [version] [android keystore path] [android keystore password] [github prerelease] [encryption key] [xamarin insights key] [google play track]"
    echo "\t[version]:  Version name, following semantic versioning guidelines (e.g., 0.3.1-prerelease)."
    echo "\t[android keystore path]:  Path to Android keystore file."
    echo "\t[android keystore password]:  Password used to open the Android keystore and signing key (assumed to be the same)."
    echo "\t[github prerelease]:  Whether or not the GitHub release should be marked as a prerelease (true/false)."
    echo "\t[encryption key]:  Encryption key for Sensus data. If this is changed, the new release of Sensus will be unable to work with any data encrypted with previous versions of Sensus."
    echo "\t[xamarin insights key]:  API key for Xamarin Insights."
    echo "\t[google play track]:  Google Play track (alpha, beta, production, or rollout)."
    echo ""
    echo "For example (for a prerelease to beta):  ./ReleaseSensus.sh 0.8.0-prerelease /path/to/sensus.keystore keystore_password true 234-23-4-23f-sdf-4 23423423-42342-34-24 beta"
    echo
    exit 1
fi
