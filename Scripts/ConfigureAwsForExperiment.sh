#!/bin/sh

if [ $# -ne 2 ]; then
    echo "Usage:  ./ConfigureAwsForExperiment.sh [S3 bucket] [experiment identifier]"
    echo "\t[S3 bucket]:  Name of S3 bucket to configure for experiment."
    echo "\t[experiment identifier]:  Short identifier for Sensus experiment. Letters and spaces only."
    exit 1
fi

########################################
##### Create Cognito Identity Pool #####
########################################

echo "Creating Cognito identity pool..."
cognitoId=$(aws cognito-identity create-identity-pool --identity-pool-name "$2 Participant" --allow-unauthenticated-identities | jq ".IdentityPoolId" | tr "\"" "\0")
if [ "$cognitoId" == "" ]; then
    echo "Failed to create Cognito identity pool."
    exit $?
fi

###########################
##### Create IAM Role #####
###########################

# create new IAM role that allows new Cognito identity pool to assume it
echo "Creating IAM role..."
iamRoleName="$2-Participant"
cat ./CognitoTrustPolicy.json | sed "s/sensusCognitoId/$cognitoId/" > tmp.json
iamRoleARN=$(aws iam create-role --role-name "$iamRoleName" --assume-role-policy-document file://./tmp.json | jq ".Role.Arn")
if [ $? -ne 0 ]; then
    rm tmp.json
    echo "Failed to create IAM role."
    exit $?
fi
rm tmp.json

# prepare and attach participant permission policy to new IAM role
echo "Setting permissions for IAM role..."
cat ./ParticipantPermissionPolicy.json | sed "s/s3Bucket/$1/" > tmp1.json
cat ./tmp1.json | sed "s/sensusExpId/$2/" > tmp2.json
s3Path=$(jq ".Statement[1].Resource[0]" tmp2.json | tr "\"" "\0")
iamPolicyName="$iamRoleName-Policy"
aws iam put-role-policy --role-name "$iamRoleName" --policy-name "$iamPolicyName" --policy-document file://./tmp2.json
if [ $? -ne 0 ]; then
    rm tmp1.json tmp2.json
    echo "Failed to attach permission policy to IAM role."
    exit $?
fi
rm tmp1.json tmp2.json

######################################################
##### Link Cognito identity pool to new IAM role #####
######################################################

echo "Linking Cognito idenity pool with IAM role..."
aws cognito-identity set-identity-pool-roles --identity-pool-id $cognitoId --roles "authenticated=$iamRoleARN,unauthenticated=$iamRoleARN"

# display information and exit

echo "All done:"
echo "\tCognito identity pool ID:  $cognitoId"
echo "\tS3 path:  $s3Path"