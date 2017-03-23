#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./DeconfigureAwsExperiment.sh [experiment identifier]"
    echo "\t[experiment identifier]:  Short identifier for Sensus experiment that should be deconfigured."
    exit 1
fi

read -p "Are you sure you want to deconfigure experiment $1? This will immediately discontinue any ongoing data collection [y/n]:  " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
  echo "Aborting."
  exit 1
fi

########################################
##### Delete Cognito Identity Pool #####
########################################

echo "Getting Cognito identity pool ID..."
cognitoIdentityPoolId=$(./GetCognitoIdForExperiment.sh "$1")
if [ $? -ne 0 ] || [ "$cognitoIdentityPoolId" == "" ]; then
  echo "Failed to get Cognito identity pool ID."
else
  echo "Deleting Cognito identity pool..."
  aws cognito-identity delete-identity-pool --identity-pool-id "$cognitoIdentityPoolId"
  if [ $? -ne 0 ]; then
    echo "Failed to delete Cognito identity pool."
  fi
fi

###########################
##### Delete IAM Role #####
###########################

echo "Deleting IAM role policy..."
iamRoleName="$1-Participant"
aws iam delete-role-policy --role-name "$iamRoleName" --policy-name="$iamRoleName-Policy"
if [ $? -ne 0 ]; then
    echo "Failed to delete IAM role policy."
fi

echo "Deleting IAM role..."
aws iam delete-role --role-name "$iamRoleName"
if [ $? -ne 0 ]; then
    echo "Failed to delete IAM role."
fi

echo "All done."
