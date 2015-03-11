#!/bin/sh

# show usage
if [ $1 -eq "--help" || $# -ne 5 ]; then
    echo "Usage:  ./Release [version name] [keystore path] [keystore password] [prerelease] [github access token]"
    exit 0
fi

# grab latest develop commit
git checkout develop
git pull

# set new version name in android manifest
sed -E -i '' "s/android:versionName=\"[^\"]+\"/android:versionName=\"$1\"/g" Properties/AndroidManifest.xml

# increment version code in android manifest
new_version_code=$((`grep -oE 'android:versionCode="[^"]+"' Properties/AndroidManifest.xml | grep -oE "[0-9]+"`+1))
sed -E -i '' "s/android:versionCode=\"[^\"]+\"/android:versionCode=\"$new_version_code\"/g" Properties/AndroidManifest.xml

# build and sign APK, quitting if there is an error
xbuild /p:Configuration=Release /p:AndroidKeyStore=true /p:AndroidSigningKeyAlias=sensus /p:AndroidSigningKeyPass=$3 /p:AndroidSigningKeyStore=$2 /p:AndroidSigningStorePass=$3 Sensus.Android.csproj
if [ $? -ne 0 ]; then 
    echo "Error building and/or signing release."
    exit $?;
fi

# merge develop into master
git commit -a -m "Preparing for Android Sensus release v$1."
git checkout master
git merge develop

# create tag for release
tag_name="Android-v$1"
git tag -a $tag_name
git push origin $tag_name

# draft github release
curl --data "{\"tag_name\": \"$tag_name\",\"target_commitish\": \"master\",\"name\": \"Android Sensus release v$1\",\"body\": \"Release of Sensus for Android, version $1.\",\"draft\": false,\"prerelease\": $4}" https://api.github.com/repos/MatthewGerber/sensus/releases?access_token=:$5

