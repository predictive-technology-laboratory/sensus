#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./deconfigure-s3.sh [bucket]"
    echo "\t[bucket]:  Bucket to deconfigure"
    exit 1
fi

# delete bucket policy
echo "Deleting S3 bucket policy..."
aws s3api delete-bucket-policy --bucket $1
if [ $? -ne 0 ]; then
    echo "Failed to delete bucket policy."
fi

# delete iam user policy and user
iamUserName="$1"
aws iam delete-user-policy --user-name $iamUserName --policy-name $iamUserName
if [ $? -ne 0 ]; then
    echo "Failed to delete IAM user policy."
fi

echo "Deleting IAM user..."
aws iam delete-user --user-name $iamUserName
if [ $? -ne 0 ]; then
    echo "Failed to delete IAM user."
fi