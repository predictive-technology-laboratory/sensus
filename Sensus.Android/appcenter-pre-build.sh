#!/usr/bin/env bash
echo keystorePassword: "$APPCENTER_KEYSTORE_PASSWORD\n" #>> $APPCENTER_OUTPUT_DIRECTORY/env.txt
echo keyAlias: "$APPCENTER_KEY_ALIAS\n" #>> $APPCENTER_OUTPUT_DIRECTORY/env.txt
echo keyPassword: "$APPCENTER_KEY_PASSWORD\n" #>> $APPCENTER_OUTPUT_DIRECTORY/env.txt
#echo keystoreFilename: "$keystoreFilename\n" #>> $APPCENTER_OUTPUT_DIRECTORY/env.txt
echo keystoreEncoded: "$APPCENTER_KEYSTORE_ENCODED\n" #>> $APPCENTER_OUTPUT_DIRECTORY/env.txt
