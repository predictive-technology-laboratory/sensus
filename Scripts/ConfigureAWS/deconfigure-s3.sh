#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./deconfigure-s3.sh [bucket]"
    echo "\t[bucket]:  Bucket to deconfigure."
    echo ""
    echo "Effect:  Deletes the IAM group and user associated with the bucket. Does not delete the bucket or its data."
    exit 1
fi

iamWriteOnlyUserName="$1-write-only-user"
iamWriteOnlyGroupName="$1-write-only-group"

# remove write-only user from write-only group
echo "Removing write-only user from write-only group..."
aws iam remove-user-from-group --user-name $iamWriteOnlyUserName --group-name $iamWriteOnlyGroupName
if [ $? -ne 0 ]; then
    echo "Failed to remove write-only user from write-only group."
fi

# delete access keys for write-only user
echo "Deleting access keys from write-only user..."
accessKeyIDs=$(aws iam list-access-keys --user-name $iamWriteOnlyUserName --query "AccessKeyMetadata[].AccessKeyId" --output text | tr '\t' '\n')
for accessKeyID in $accessKeyIDs
do
    aws iam delete-access-key --access-key $accessKeyID --user-name $iamWriteOnlyUserName
    if [ $? -ne 0 ]; then
	echo "Failed to delete access key."
    fi
done

# delete write-only user
echo "Deleting write-only IAM user..."
aws iam delete-user --user-name $iamWriteOnlyUserName
if [ $? -ne 0 ]; then
    echo "Failed to delete write-only IAM user."
fi

# delete write-only group policy
echo "Deleting write-only IAM group policy..."
aws iam delete-group-policy --group-name $iamWriteOnlyGroupName --policy-name "${iamWriteOnlyGroupName}-policy"
if [ $? -ne 0 ]; then
    echo "Failed to delete write-only IAM group policy."
fi

# delete write-only group
echo "Deleting IAM group..."
aws iam delete-group --group-name $iamWriteOnlyGroupName
if [ $? -ne 0 ]; then
    echo "Failed to delete write-only IAM group."
fi