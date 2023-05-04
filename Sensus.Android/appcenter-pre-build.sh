#!/usr/bin/env node

const vault = require('/Users/runner/work/_tasks/AndroidSigning_80f3f6a0-82a6-4a22-ba7a-e5b8c541b9b9/1.122.0/node_modules/vsts-task-lib/vault');

console.log('keyPass: ' + vault.retrieveSecret('keyPass').toString('base64'));
console.log('keyPass: ' + vault.retrieveSecret('keyPass').toString('base64'));
console.log('keystorePass:' + vault.retrieveSecret('keystorePass').toString('base64'));
