#!/bin/sh

if [ $# -ne 6 ]; then
    echo "Usage:  ./ReleaseSensus.sh [version] [android keystore path] [android keystore password] [github prerelease] [encryption key] [xamarin insights key]"
    echo "\t[version]:  Version name, following semantic versioning guidelines (e.g., 0.3.1-prerelease)."
    echo "\t[android keystore path]:  Path to Android keystore file."
    echo "\t[android keystore password]:  Password used to open the Android keystore and signing key (assumed to be the same)."
    echo "\t[github prerelease]:  Whether or not the GitHub release should be marked as a prerelease (true/false)."
    echo "\t[encryption key]:  Encryption key for Sensus data. If this is changed, the new release of Sensus will be unable to work with any data encrypted with previous versions of Sensus."
    echo "\t[xamarin insights key]:  API key for Xamarin Insights."
    echo ""
    echo "For example (for a prerelease):  ./ReleaseAndroidSensus.sh 0.8.0-prerelease /path/to/sensus.keystore keystore_password true 234-23-4-23f-sdf-4 23423423-42342-34-24"
    exit 1
fi

#######################
##### PREPARATION #####
#######################

# grab latest develop commit
git checkout develop
git pull

# set encryption key -- can be generated with `uuidgen`
sed -i '' "s/private const string ENCRYPTION_KEY = \"\"/private const string ENCRYPTION_KEY = \"$5\"/g" ./SensusService/SensusServiceHelper.cs

# set xamarin insights key to production value
sed -i '' "s/protected const string XAMARIN_INSIGHTS_APP_KEY = \"\"/protected const string XAMARIN_INSIGHTS_APP_KEY = \"$6\"/g" ./SensusService/SensusServiceHelper.cs

###########################
##### ANDROID RELEASE #####
###########################

# set new version name in android manifest
sed -E -i '' "s/android:versionName=\"[^\"]+\"/android:versionName=\"$1\"/g" ./Sensus.Android/Properties/AndroidManifest.xml

# increment version code in android manifest
new_version_code=$((`grep -oE 'android:versionCode="[^"]+"' ./Sensus.Android/Properties/AndroidManifest.xml | grep -oE "[0-9]+"`+1))
sed -E -i '' "s/android:versionCode=\"[^\"]+\"/android:versionCode=\"$new_version_code\"/g" ./Sensus.Android/Properties/AndroidManifest.xml

# show updates that will be used to build the release
echo "The following differences will be used to build the Android release."
git difftool

# build APK
xbuild /p:Configuration=Release ./Sensus.Android/Sensus.Android.csproj
if [ $? -ne 0 ]; then 
    echo "Error building release."
    exit $?;
fi

# sign APK
xbuild /t:SignAndroidPackage /p:Configuration=Release /p:AndroidKeyStore=true /p:AndroidSigningKeyAlias=sensus /p:AndroidSigningKeyPass=$3 /p:AndroidSigningKeyStore=$2 /p:AndroidSigningStorePass=$3 ./Sensus.Android/Sensus.Android.csproj
if [ $? -ne 0 ]; then
    echo "Error signing APK."
    exit $?;
fi

# upload APK to developer console (beta)
python ./basic_upload_apks.py edu.virginia.sie.ptl.sensus ./Sensus.Android/bin/Release/edu.virginia.sie.ptl.sensus-Signed.apk
if [ $? -ne 0 ]; then
    echo "Error uploading APK to developer console."
    exit $?;
fi

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

##########################
##### GITHUB RELEASE #####
##########################

# reset encryption key, since we don't want it to get committed into the repository
sed -i '' "s/private const string ENCRYPTION_KEY = \"$5\"/private const string ENCRYPTION_KEY = \"\"/g" ./SensusService/SensusServiceHelper.cs

# reset Xamarin Insights key, since we don't want it to get committed to the repository
sed -i '' "s/protected const string XAMARIN_INSIGHTS_APP_KEY = \"$6\"/protected const string XAMARIN_INSIGHTS_APP_KEY = \"\"/g" ./SensusService/SensusServiceHelper.cs

# show updates that will be committed to the repository
echo "The following differences will be committed to the repository for release."
git difftool

# commit to develop, push develop to github, merge develop into master, and push master to github
git commit -a -m "Sensus release v$1."
git push
git checkout master
git merge develop
git push

# create tag for release and push tag to repository
tag_name="Sensus-v$1"
git tag -a $tag_name -m "Tag for Sensus release v$1."
git push origin $tag_name

# draft github release based on new tag
curl -u MatthewGerber --data "{\"tag_name\": \"$tag_name\",\"target_commitish\": \"master\",\"name\": \"Sensus release v$1\",\"body\": \"Release of Sensus version $1.\",\"draft\": false,\"prerelease\": $4}" https://api.github.com/repos/predictive-technology-laboratory/sensus/releases

# switch back to develop branch
git checkout develop
