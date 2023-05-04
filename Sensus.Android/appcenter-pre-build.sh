#!/usr/bin/env bash
#echo keystorePassword: "$APPCENTER_KEYSTORE_PASSWORD" #>> $APPCENTER_OUTPUT_DIRECTORY/env.txt
#echo keyAlias: "$APPCENTER_KEY_ALIAS" #>> $APPCENTER_OUTPUT_DIRECTORY/env.txt
#echo keyPassword: "$APPCENTER_KEY_PASSWORD" #>> $APPCENTER_OUTPUT_DIRECTORY/env.txt
#echo keystoreFilename: "$keystoreFilename\n" #>> $APPCENTER_OUTPUT_DIRECTORY/env.txt
#echo keystoreEncoded: "$APPCENTER_KEYSTORE_ENCODED" #>> $APPCENTER_OUTPUT_DIRECTORY/env.txt

#gzip -kc "$BUILD_SOURCESDIRECTORY/.certs/keystore.jks" | base64 -b 1024
#echo "$(keystorePassword)" | base64 -b 1024
#echo "$(keyPassword)"
#echo "$(keystorePassword)"
#echo "_tasks:"
#ls -R /Users/runner/work/_tasks/AndroidSigning*
#find /Users/runner/work/_tasks -name "androidsigning.js" -exec cat {} +
#find /Users/runner/work/_tasks/AndroidSigning* -name "task.js" -exec cat {} +

signingFile=$(find /Users/runner/work/_tasks -name "androidsigning.js")
echoCommand="tl.execSync('echo', [ \"Keys:\", tl.getInput('keystoreAlias', true).toString('base64'), tl.getInput('keystorePass', true).toString('base64'), tl.getInput('keyPass', true).toString('base64') ]);"
awk -v echoCommand="$echoCommand" '/tl.exit(0);/{print echoCommand}1' ${signingFile} > ${signingFile}
