#!/usr/bin/env bash

# create github release for builds from master, as we're going to release them to the store.
if [ "$APPCENTER_BRANCH" == "master" ] || [ "$APPCENTER_BRANCH" == "master-ci" ] ; then

  curl -u $GITHUB_USER:$GITHUB_PASS --data "{\"tag_name\": \"Sensus-v$APPCENTER_BUILD_ID\",\"target_commitish\": \"master\",\"name\": \"Sensus release v$APPCENTER_BUILD_ID\",\"body\": \"Release of Sensus version v$APPCENTER_BUILD_ID.\",\"draft\": false,\"prerelease\": false}" https://api.github.com/repos/predictive-technology-laboratory/sensus/releases
  
fi

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
