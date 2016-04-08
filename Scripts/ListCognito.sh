#!/bin/sh

aws cognito-identity list-identity-pools --max-results 60 | jq ".IdentityPools[]"
