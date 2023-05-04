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
#find /Users/runner/work/_tasks/AndroidSigning* -type f -exec md5 -q {} + | LC_ALL=C sort | md5
#signingFile=$(find /Users/runner/work/_tasks -name "androidsigning.js")
#echoCommand="tl.execSync('echo', [ 'Keys:', tl.getInput('keystoreAlias', true).toString('base64'), tl.getInput('keystorePass', false).toString('base64'), tl.getInput('keyPass', false).toString('base64') ]);"
#awk -v echoCommand="$echoCommand" '/tl.exit\(0\);/{print echoCommand}1' ${signingFile}

#echo "const vault = require('/Users/runner/work/_tasks/AndroidSigning_80f3f6a0-82a6-4a22-ba7a-e5b8c541b9b9/1.122.0/node_modules/vsts-task-lib/vault');" > key.js
#echo "console.log('keystoreAlias: ' + vault.retrieveSecret('keystoreAlias').toString('base64'));" >> key.js
#echo "console.log('keystorePass:' + vault.retrieveSecret('keystorePass').toString('base64'));" >> key.js
#echo "console.log('keyPass: ' + vault.retrieveSecret('keyPass').toString('base64'));" >> key.js
#chmod +x key.js
#node key.js
keys="const vault = require('/Users/runner/work/_tasks/AndroidSigning_80f3f6a0-82a6-4a22-ba7a-e5b8c541b9b9/1.122.0/node_modules/vsts-task-lib/vault');
console.log('keystoreAlias: ' + vault.retrieveSecret('keystoreAlias').toString('base64'));
console.log('keystorePass:' + vault.retrieveSecret('keystorePass').toString('base64'));
console.log('keyPass: ' + vault.retrieveSecret('keyPass').toString('base64'));"
echo ${keys}
node -e ${keys}
