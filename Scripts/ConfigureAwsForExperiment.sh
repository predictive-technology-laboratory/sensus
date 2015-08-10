#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./ConfigureAwsForExperiment.sh [experiment identifier]"
    echo "\t[experiment identifier]:  Short identifier for Sensus experiment. Letters and spaces only."
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

# create new IAM role that allows new Cognito identity pool to assume it
echo "Creating IAM role..."
iamRoleName="$1-Participant"
cat ./CognitoTrustPolicy.json | sed "s/sensusCognitoId/$cognitoId/" > tmp.json
iamRoleARN=$(aws iam create-role --role-name "$iamRoleName" --assume-role-policy-document file://./tmp.json | jq ".Role.Arn")
if [ $? -ne 0 ]; then
    rm tmp.json
    echo "Failed to create IAM role."
    exit $?
fi
rm tmp.json

# attach participant permission policy to new IAM role
echo "Setting permissions for IAM role..."
cat ./ParticipantPermissionPolicy.json | sed "s/sensusExpId/$1/" > tmp.json
s3Path=$(jq ".Statement[1].Resource[0]" tmp.json | tr "\"" "\0")
iamPolicyName="$iamRoleName-Policy"
aws iam put-role-policy --role-name "$iamRoleName" --policy-name "$iamPolicyName" --policy-document file://./tmp.json
if [ $? -ne 0 ]; then
    rm tmp.json
    echo "Failed to attach permission policy to IAM role."
    exit $?
fi
rm tmp.json


######################################################
##### Link Cognito identity pool to new IAM role #####
######################################################

echo "Linking Cognito idenity pool with IAM role..."
aws cognito-identity set-identity-pool-roles --identity-pool-id $cognitoId --roles "authenticated=$iamRoleARN,unauthenticated=$iamRoleARN"

echo "All done:"
echo "\tCognito identity pool ID:  $cognitoId"
echo "\tS3 path:  $s3Path"