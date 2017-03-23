#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./GetCognitoIdForExperiment.sh [experiment identifier]"
    echo "\t[experiment identifier]:  Short identifier for Sensus experiment."
    exit 1
fi

aws cognito-identity list-identity-pools --max-results 60 | jq ".IdentityPools[] | select(.IdentityPoolName==\"$1 Participant\").IdentityPoolId" | tr -d '"'

exit $?
