#!/bin/sh

if [ $# -ne 4 ]; then
    echo "Usage:  ./Release.sh [version name] [keystore path] [keystore password] [github prerelease]"
    echo "\t[version name]:  Version name, following semantic versioning guidelines (e.g., 0.3.1-prerelease)."
    echo "\t[keystore path]:  Path to keystore file."
    echo "\t[keystore password]:  Password used to open keystore and signing key (assumed to be the same)."
    echo "\t[github prerelease]:  Whether or not the GitHub release should be marked as a prerelease (true/false)."
    echo ""
    echo "For example (for a prerelease):  ./Release.sh 0.8.0-prerelease /path/to/sensus.keystore keystore_password true"
    exit 1
fi

# grab latest develop commit
git checkout develop
git pull

# set new version name in android manifest
sed -E -i '' "s/android:versionName=\"[^\"]+\"/android:versionName=\"$1\"/g" Properties/AndroidManifest.xml

# increment version code in android manifest
new_version_code=$((`grep -oE 'android:versionCode="[^"]+"' Properties/AndroidManifest.xml | grep -oE "[0-9]+"`+1))
sed -E -i '' "s/android:versionCode=\"[^\"]+\"/android:versionCode=\"$new_version_code\"/g" Properties/AndroidManifest.xml

# show updates
git difftool

# build project
xbuild /p:Configuration=Release Sensus.Android.csproj
if [ $? -ne 0 ]; then 
    echo "Error building release."
    exit $?;
fi

# sign apk
xbuild /t:SignAndroidPackage /p:Configuration=Release /p:AndroidKeyStore=true /p:AndroidSigningKeyAlias=sensus /p:AndroidSigningKeyPass=$3 /p:AndroidSigningKeyStore=$2 /p:AndroidSigningStorePass=$3 Sensus.Android.csproj
if [ $? -ne 0 ]; then
    echo "Error signing APK."
    exit $?;
fi

# merge develop into master and push master to github
git commit -a -m "Preparing for Android Sensus release v$1."
git push
git checkout master
git merge develop
git push

# create tag for release
tag_name="Android-v$1"
git tag -a $tag_name -m "Tag for Android Sensus release v$1."
git push origin $tag_name

# draft github release
curl -u MatthewGerber --data "{\"tag_name\": \"$tag_name\",\"target_commitish\": \"master\",\"name\": \"Android Sensus release v$1\",\"body\": \"Release of Sensus for Android, version $1.\",\"draft\": false,\"prerelease\": $4}" https://api.github.com/repos/MatthewGerber/sensus/releases

