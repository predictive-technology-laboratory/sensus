#!/bin/sh

if [ $# -ne 7 ]; then
    echo
    echo "Purpose:  Creates an iOS ad-hoc release using Diawi."
    echo ""
    echo "Usage:  ./ReleaseSensusiOSAdHoc.sh [version] [encryption key] [xamarin insights key] [diawi token] [email]"
    echo "\t[version]:  Version name."
    echo "\t[android keystore path]:  Path to Android keystore file."
    echo "\t[android keystore password]:  Password used to open the Android keystore and signing key (assumed to be the same)."
    echo "\t[encryption key]:  Encryption key for Sensus data. If this is changed, the new release of Sensus will be unable to work with any data encrypted with previous versions of Sensus."
    echo "\t[xamarin insights key]:  API key for Xamarin Insights."
    echo "\t[diawi token]:  Authorization token for Diawi uploads."
    echo "\t[email]:  Callback email for Diawi upload."
    echo ""
    echo "For example:  ./ReleaseSensusiOSAdHoc.sh 0.8.0-prerelease asdf2349f809 09d8f09df8df 238f987 asdf@asdf.com"
    echo
    exit 1
fi

# set encryption key -- can be generated with `uuidgen`
sed -i '' "s/public const string ENCRYPTION_KEY = \"\"/public const string ENCRYPTION_KEY = \"$4\"/g" ../../Sensus.Shared/SensusServiceHelper.cs

# set xamarin insights key to production value
sed -i '' "s/public const string XAMARIN_INSIGHTS_APP_KEY = \"\"/public const string XAMARIN_INSIGHTS_APP_KEY = \"$5\"/g" ../../Sensus.Shared/SensusServiceHelper.cs

# set new version name in android manifest
sed -E -i '' "s/android:versionName=\"[^\"]+\"/android:versionName=\"$1\"/g" ../../Sensus.Android/Properties/AndroidManifest.xml

# increment version code in android manifest
new_version_code=$((`grep -oE 'android:versionCode="[^"]+"' ../../Sensus.Android/Properties/AndroidManifest.xml | grep -oE "[0-9]+"`+1))
sed -E -i '' "s/android:versionCode=\"[^\"]+\"/android:versionCode=\"$new_version_code\"/g" ../../Sensus.Android/Properties/AndroidManifest.xml

# show updates that will be used to build the release
echo "The following differences will be used to build the Android release."
git difftool

# build APK
xbuild /p:Configuration=Release /p:Platform=Android /t:Rebuild ../../Sensus.Android/Sensus.Android.csproj
if [ $? -ne 0 ]; then 
    echo "Error building release."
    exit $?;
fi

# sign APK
xbuild /p:Configuration=Release /p:Platform=Android /p:AndroidKeyStore=true /p:AndroidSigningKeyAlias=sensus /p:AndroidSigningKeyPass=$3 /p:AndroidSigningKeyStore=$2 /p:AndroidSigningStorePass=$3 /t:SignAndroidPackage ../../Sensus.Android/Sensus.Android.csproj
if [ $? -ne 0 ]; then
    echo "Error signing APK."
    exit $?;
fi

# upload IPA
echo "Uploading to Diawi..."
curl -v https://upload.diawi.com -F "file=@../../Sensus.Android/bin/Release/edu.virginia.sie.ptl.sensus-Signed.apk" -F token="$6" -F callback_email="$7"
echo
if [ $? -ne 0 ]; then
    echo "Error uploading iOS ad-hoc release to Diawi."
    exit $?;
fi

# revert all changes
echo
echo "Reverting all local changes."
git checkout ../..