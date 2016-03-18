#!/bin/sh

if [ $# -ne 7 ]; then
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
    exit 1
fi

#######################
##### PREPARATION #####
#######################

# get name of release branch -- this is the current branch
releaseBranch=$(git rev-parse --abbrev-ref HEAD)

# grab latest commit on the release branch
git pull

# set encryption key -- can be generated with `uuidgen`
sed -i '' "s/private const string ENCRYPTION_KEY = \"\"/private const string ENCRYPTION_KEY = \"$5\"/g" ./SensusService/SensusServiceHelper.cs

# set xamarin insights key to production value
sed -i '' "s/protected const string XAMARIN_INSIGHTS_APP_KEY = \"\"/protected const string XAMARIN_INSIGHTS_APP_KEY = \"$6\"/g" ./SensusService/SensusServiceHelper.cs

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
if [ $? -ne 0 ]; then
    echo "Error building iOS release."
    exit $?;
fi

ipaPath="./Sensus.iOS/bin/iPhone/Release/SensusiOS-$1.ipa"

# validate IPA
echo "Validating iOS IPA $ipaPath ..."
read -p "Enter iTunes Connect username:  " -r itunesUsername
/Applications/Xcode.app/Contents/Applications/Application\ Loader.app/Contents/Frameworks/ITunesSoftwareService.framework/Support/altool --validate-app -f "$ipaPath" -t ios -u "$itunesUsername"
if [ $? -ne 0 ]; then
    echo "Error validating iOS IPA $ipaPath"
    exit $?;
fi

# upload IPA
echo "Uploading iOS IPA $ipaPath ..."
/Applications/Xcode.app/Contents/Applications/Application\ Loader.app/Contents/Frameworks/ITunesSoftwareService.framework/Support/altool --upload-app -f "$ipaPath" -t ios -u "$itunesUsername"
if [ $? -ne 0 ]; then
    echo "Error uploading iOS IPA $ipaPath"
    exit $?;
fi

# upload iOS dSYM file if build was successful
if [ $? -eq 0 ]; then
    echo "Zipping and uploading dSYM file to Xamarin Insights."
    zip -r ./Sensus.iOS/bin/iPhone/Release/SensusiOS.dSYM.zip ./Sensus.iOS/bin/iPhone/Release/SensusiOS.app.dSYM
    curl -F "dsym=@./Sensus.iOS/bin/iPhone/Release/SensusiOS.dSYM.zip;type=application/zip" "https://xaapi.xamarin.com/api/dsym?apikey=$6"
fi