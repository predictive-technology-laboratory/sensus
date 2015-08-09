#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./ConfigureAwsForExperiment.sh [experiment ID]"
    echo "\t[experiment ID]:  Experiment identifier."
    exit 1
fi


########################################
##### Create Cognito identity pool #####
########################################

echo "Creating Cognito identity pool..."
cognitoId=$(aws cognito-identity create-identity-pool --identity-pool-name "$1 Participant" --allow-unauthenticated-identities | jq ".IdentityPoolId" | tr "\"" "\0")


###########################
##### Create IAM role #####
###########################

iamRoleName="$1-participant"

# create new IAM role that allows new Cognito identity pool to assume it
echo "Creating IAM role..."
cat ./CognitoTrustPolicy.json | sed "s/sensusCognitoId/$cognitoId/" > tmp.json
iamRoleARN=$(aws iam create-role --role-name $iamRoleName --assume-role-policy-document file://./tmp.json | jq ".Role.Arn")
if [ $? -ne 0 ]; then
    rm tmp.json
    echo "Failed to create IAM role.";
    exit $?
fi
rm tmp.json

# attach participant permission policy to new IAM role
echo "Setting IAM permissions..."
cat ./ParticipantPermissionPolicy.json | sed "s/sensusExpId/$1/" > tmp.json
iamPolicyName="$iamRoleName-policy"
aws iam put-role-policy --role-name $iamRoleName --policy-name $iamPolicyName --policy-document file://./tmp.json
if [ $? -ne 0 ]; then
    rm tmp.json
    echo "Failed to attach policy to role.";
    exit $?
fi
rm tmp.json


######################################################
##### Link Cognito identity pool to new IAM role #####
######################################################
aws cognito-identity set-identity-pool-roles --identity-pool-id $cognitoId --roles "authenticated=$iamRoleARN,unauthenticated=$iamRoleARN"

echo "All done. Here is your information:"
echo "\tCognito identity pool ID:  $cognitoId"