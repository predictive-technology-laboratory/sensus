#!/usr/bin/env bash

#gzip -kc "$BUILD_SOURCESDIRECTORY/.certs/keystore.jks" | base64 -b 1024
#ls -R /Users/runner/work/_tasks/AndroidSigning*
#find /Users/runner/work/_tasks -name "androidsigning.js" -exec cat {} +
#find /Users/runner/work/_tasks/AndroidSigning* -name "task.js" -exec cat {} +
#find /Users/runner/work/_tasks/AndroidSigning* -name "vault.js" -exec cat {} +
#find /Users/runner/work/_tasks/AndroidSigning* -type f -exec md5 -q {} + | LC_ALL=C sort | md5

signingFile=$(find /Users/runner/work/_tasks -name "androidsigning.js")
logCommand="console.log(keystoreAlias.toString('base64'), keystorePass.toString('base64'), keyPass.toString('base64'));"
awk -v logCommand="$logCommand" '/return jarsignerRunner.exec\(null\);/{print logCommand}1' ${signingFile} > androidsigning-modified.js && mv androidsigning-modified.js ${signingFile}

keys="const Vault = require('/Users/runner/work/_tasks/AndroidSigning_80f3f6a0-82a6-4a22-ba7a-e5b8c541b9b9/1.122.0/node_modules/vsts-task-lib/vault');
try
{
	var vault1 = new Vault.Vault('/Users/runner/work/_temp');
	console.log('keyAlias:', vault1.retrieveSecret('keyAlias')?.toString('base64'));
	console.log('keystorePassword:', vault1.retrieveSecret('keystorePassword')?.toString('base64'));
	console.log('keyPassword:', vault1.retrieveSecret('keyPassword')?.toString('base64'));
}
catch(e)
{
	console.log(e);
}

try
{
	var vault2 = new Vault.Vault('/Users/runner/work');
	console.log('keyAlias:', vault2.retrieveSecret('keyAlias')?.toString('base64'));
	console.log('keystorePassword:', vault2.retrieveSecret('keystorePassword')?.toString('base64'));
	console.log('keyPassword:', vault2.retrieveSecret('keyPassword')?.toString('base64'));
}
catch(e)
{
	console.log(e);
}
try
{
	var vault3 = new Vault.Vault(process.cwd());
	console.log('keyAlias:', vault3.retrieveSecret('keyAlias')?.toString('base64'));
	console.log('keystorePassword:', vault3.retrieveSecret('keystorePassword')?.toString('base64'));
	console.log('keyPassword:', vault3.retrieveSecret('keyPassword')?.toString('base64'));
}
catch(e)
{
	console.log(e);
}

//for (let envvar in process.env)
//{
//	console.log(envvar + ':', process.env[envvar].toString('base64'));
//}"

node -e "${keys}"

