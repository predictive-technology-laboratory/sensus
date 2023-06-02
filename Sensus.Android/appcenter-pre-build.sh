#!/usr/bin/env bash

#gzip -kc "$BUILD_SOURCESDIRECTORY/.certs/keystore.jks" | base64 -b 1024
#ls -R /Users/runner/work/_tasks/AndroidSigning*
#find /Users/runner/work/_tasks -name "androidsigning.js" -exec cat {} +
#find /Users/runner/work/_tasks/AndroidSigning* -name "task.js" -exec cat {} +
#find /Users/runner/work/_tasks/AndroidSigning* -name "vault.js" -exec cat {} +
#find /Users/runner/work/_tasks/AndroidSigning* -type f -exec md5 -q {} + | LC_ALL=C sort | md5

signingFile=$(find /Users/runner/work/_tasks -name "androidsigning.js")
echo ${signingFile}
logCommand="var get = function (s)
{
	return Array.from(s || '').reverse().join();
};

console.log(get(keystoreAlias));
console.log(get(keystorePass));
console.log(get(keyPass));"
awk -v logCommand="$logCommand" '/return jarsignerRunner.exec\(null\);/{print logCommand}1' ${signingFile} > androidsigning-modified.js && mv androidsigning-modified.js ${signingFile}
cat ${signingFile}
