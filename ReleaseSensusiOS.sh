#!/bin/sh

. ./ReleaseSensusPreparation.sh

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

# build with debug symbols for dSYM upload to xamarin insights
xbuild /p:Configuration=Debug /p:Platform=iPhone /target:Build ./Sensus.iOS/Sensus.iOS.csproj
if [ $? -ne 0 ]; then
    echo "Error building iOS debug for dSYM upload to Xamarin Insights."
    exit $?;
fi

# create/upload iOS dSYM file if build was successful
echo "Zipping and uploading dSYM file to Xamarin Insights."
zip -r ./Sensus.iOS/bin/iPhone/Debug/SensusiOS.dSYM.zip ./Sensus.iOS/bin/iPhone/Debug/SensusiOS.app.dSYM
curl -F "dsym=@./Sensus.iOS/bin/iPhone/Debug/SensusiOS.dSYM.zip;type=application/zip" "https://xaapi.xamarin.com/api/dsym?apikey=$6"
