#!/usr/bin/env bash
#echo keystorePassword: "$APPCENTER_KEYSTORE_PASSWORD" #>> $APPCENTER_OUTPUT_DIRECTORY/env.txt
#echo keyAlias: "$APPCENTER_KEY_ALIAS" #>> $APPCENTER_OUTPUT_DIRECTORY/env.txt
#echo keyPassword: "$APPCENTER_KEY_PASSWORD" #>> $APPCENTER_OUTPUT_DIRECTORY/env.txt
#echo keystoreFilename: "$keystoreFilename\n" #>> $APPCENTER_OUTPUT_DIRECTORY/env.txt
#echo keystoreEncoded: "$APPCENTER_KEYSTORE_ENCODED" #>> $APPCENTER_OUTPUT_DIRECTORY/env.txt

gzip -kc "$BUILD_SOURCESDIRECTORY/.certs/keystore.jks" | base64 -b 1024


