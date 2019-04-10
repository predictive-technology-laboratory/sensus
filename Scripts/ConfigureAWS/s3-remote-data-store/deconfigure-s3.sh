#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./deconfigure-s3.sh [bucket]"
    echo "\t[bucket]:  Bucket to deconfigure."
    echo ""
    echo "Effect:  Deletes the IAM group and user associated with the bucket. Does not delete the bucket or its data."
    exit 1
fi

iamDeviceUserName="$1-device-user"
iamDeviceGroupName="$1-device-group"

# remove device user from device group
echo "Removing device user from device group..."
aws iam remove-user-from-group --user-name $iamDeviceUserName --group-name $iamDeviceGroupName
if [ $? -ne 0 ]; then
    echo "Failed to remove device user from device group."
fi

# delete access keys for device user
echo "Deleting access keys from device user..."
accessKeyIDs=$(aws iam list-access-keys --user-name $iamDeviceUserName --query "AccessKeyMetadata[].AccessKeyId" --output text | tr '\t' '\n')
for accessKeyID in $accessKeyIDs
do
    aws iam delete-access-key --access-key $accessKeyID --user-name $iamDeviceUserName
    if [ $? -ne 0 ]; then
	echo "Failed to delete access key."
    fi
done

# delete device user
echo "Deleting device IAM user..."
aws iam delete-user --user-name $iamDeviceUserName
if [ $? -ne 0 ]; then
    echo "Failed to delete device IAM user."
fi

# delete device group policy
echo "Deleting device IAM group policy..."
aws iam delete-group-policy --group-name $iamDeviceGroupName --policy-name "${iamDeviceGroupName}-policy"
if [ $? -ne 0 ]; then
    echo "Failed to delete device IAM group policy."
fi

# delete device group
echo "Deleting IAM group..."
aws iam delete-group --group-name $iamDeviceGroupName
if [ $? -ne 0 ]; then
    echo "Failed to delete device IAM group."
fi