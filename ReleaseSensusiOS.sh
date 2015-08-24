#!/bin/sh

if [ $# -ne 3 ]; then
    echo "Purpose:  Creates a release of Sensus for Android and iOS, based on the current GitHub branch."
    echo ""
    echo "Usage:  ./ReleaseSensus.sh [version] [android keystore path] [android keystore password] [github prerelease] [encryption key] [xamarin insights key]"
    echo "\t[version]:  Version name, following semantic versioning guidelines (e.g., 0.3.1-prerelease)."
    echo "\t[encryption key]:  Encryption key for Sensus data. If this is changed, the new release of Sensus will be unable to work with any data encrypted with previous versions of Sensus."
    echo "\t[xamarin insights key]:  API key for Xamarin Insights."
    echo ""
    echo "For example:  ./ReleaseSensusiOS.sh 0.8.0-prerelease 234-23-4-23f-sdf-4 23423423-42342-34-24"
    exit 1
fi

#######################
##### PREPARATION #####
#######################

# grab latest commit on the release branch
git pull

# set encryption key -- can be generated with `uuidgen`
sed -i '' "s/private const string ENCRYPTION_KEY = \"\"/private const string ENCRYPTION_KEY = \"$2\"/g" ./SensusService/SensusServiceHelper.cs

# set xamarin insights key to production value
sed -i '' "s/protected const string XAMARIN_INSIGHTS_APP_KEY = \"\"/protected const string XAMARIN_INSIGHTS_APP_KEY = \"$3\"/g" ./SensusService/SensusServiceHelper.cs

#######################
##### iOS RELEASE #####
#######################

# update Sensus version in plist file
awk "/<key>CFBundleVersion<\/key>/ {f=1; print; next} f {\$1=\"\t<string>$1</string>\"; f=0} 1" ./Sensus.iOS/Info.plist > tmp && mv tmp ./Sensus.iOS/Info.plist
awk "/<key>CFBundleShortVersionString<\/key>/ {f=1; print; next} f {\$1=\"\t<string>$1</string>\"; f=0} 1" ./Sensus.iOS/Info.plist > tmp && mv tmp ./Sensus.iOS/Info.plist

# show updates that will be used to build the release
echo "The following differences will be used to build the iOS release."
git difftool

# build IPA
xbuild /p:Configuration=Release /p:Platform=iPhone /p:BuildIpa=true /target:Build ./Sensus.iOS/Sensus.iOS.csproj

# reset encryption key, since we don't want it to get committed into the repository
sed -i '' "s/private const string ENCRYPTION_KEY = \"$2\"/private const string ENCRYPTION_KEY = \"\"/g" ./SensusService/SensusServiceHelper.cs

# reset Xamarin Insights key, since we don't want it to get committed to the repository
sed -i '' "s/protected const string XAMARIN_INSIGHTS_APP_KEY = \"$3\"/protected const string XAMARIN_INSIGHTS_APP_KEY = \"\"/g" ./SensusService/SensusServiceHelper.cs