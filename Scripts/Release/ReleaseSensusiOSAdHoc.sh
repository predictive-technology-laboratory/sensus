#!/bin/sh

if [ $# -ne 5 ]; then
    echo
    echo "Purpose:  Creates an iOS ad-hoc release using Diawi."
    echo ""
    echo "Usage:  ./ReleaseSensusiOSAdHoc.sh [version] [encryption key] [xamarin insights key] [diawi token] [email]"
    echo "\t[version]:  Version name."
    echo "\t[encryption key]:  Encryption key for Sensus data. If this is changed, the new release of Sensus will be unable to work with any data encrypted with previous versions of Sensus."
    echo "\t[xamarin insights key]:  API key for Xamarin Insights."
    echo "\t[diawi token]:  Authorization token for Diawi uploads."
    echo "\t[email]:  Callback email for Diawi upload."
    echo ""
    echo "For example:  ./ReleaseSensusiOSAdHoc.sh 0.8.0-prerelease /path/to/sensus.keystore keystore_password true 234-23-4-23f-sdf-4 23423423-42342-34-24 beta"
    echo
    exit 1
fi

# set encryption key -- can be generated with `uuidgen`                                                                                                                                                                                                                             
sed -i '' "s/private const string ENCRYPTION_KEY = \"\"/private const string ENCRYPTION_KEY = \"$2\"/g" ../../SensusService/SensusServiceHelper.cs

# set xamarin insights key to production value                                                                                                                                                                                                                                      
sed -i '' "s/protected const string XAMARIN_INSIGHTS_APP_KEY = \"\"/protected const string XAMARIN_INSIGHTS_APP_KEY = \"$3\"/g" ../../SensusService/SensusServiceHelper.cs

# update Sensus version in plist file
awk "/<key>CFBundleVersion<\/key>/ {f=1; print; next} f {\$1=\"\t<string>$1</string>\"; f=0} 1" ../../Sensus.iOS/Info.plist > tmp && mv tmp ../../Sensus.iOS/Info.plist
awk "/<key>CFBundleShortVersionString<\/key>/ {f=1; print; next} f {\$1=\"\t<string>$1</string>\"; f=0} 1" ../../Sensus.iOS/Info.plist > tmp && mv tmp ../../Sensus.iOS/Info.plist

# show updates that will be used to build the release
echo "The following differences will be used to build the iOS ad-hoc release."
git difftool

# build IPA
xbuild /p:Configuration=Ad-Hoc /p:Platform=iPhone /p:BuildIpa=true /target:Rebuild ../../Sensus.iOS/Sensus.iOS.csproj
if [ $? -ne 0 ]; then
    echo "Error building iOS ad-hoc release."
    exit $?;
fi

# as of Xamarin Community 6.0, the IPA is put in a directory whose name includes the current date/time. this makes it difficult to get the path
# to the IPA. so, redo the final step of the build manually to create the IPA at the path we want.
echo "Rebuilding IPA at preferred location..."
cd ../../Sensus.iOS/obj/iPhone/Ad-Hoc/ipa
zip -r -y "../../../../bin/iPhone/Ad-Hoc/SensusiOS-$1.ipa" Payload
cd ../../../../../Scripts/Release
ipaPath="../../Sensus.iOS/bin/iPhone/Ad-Hoc/SensusiOS-$1.ipa"

# upload IPA
echo "Uploading to Diawi..."
curl -v https://upload.diawi.com -F "file=@$ipaPath" -F token="$4" -F callback_email="$5"
if [ $? -ne 0 ]; then
    echo "Error uploading iOS ad-hoc release to Diawi."
    exit $?;
fi

# revert all changes
echo "Reverting all local changes."
git checkout ../..