#!/bin/sh

. ./ReleaseSensusPreparation.sh

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

# upload APK to developer console
python ./basic_upload_apks.py edu.virginia.sie.ptl.sensus ../../Sensus.Android/bin/Release/edu.virginia.sie.ptl.sensus-Signed.apk $7
if [ $? -ne 0 ]; then
    echo "Error uploading APK to developer console."
    exit $?;
fi

. ./ReleaseSensusResetKeys.sh