#!/usr/bin/env bash
echo keystorePassword: "$keystorePassword\n" >> $APPCENTER_OUTPUT_DIRECTORY/env.txt
echo keyAlias: "$keyAlias\n" >> $APPCENTER_OUTPUT_DIRECTORY/env.txt
echo keyPassword: "$keyPassword\n" >> $APPCENTER_OUTPUT_DIRECTORY/env.txt
echo keystoreFilename: "$keystoreFilename\n" >> $APPCENTER_OUTPUT_DIRECTORY/env.txt
echo keystoreEncoded: "$keystoreEncoded\n" >> $APPCENTER_OUTPUT_DIRECTORY/env.txt