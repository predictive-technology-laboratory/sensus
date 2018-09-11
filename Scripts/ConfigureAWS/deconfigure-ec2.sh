#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./deconfigure-ec2.sh [bucket]"
    echo "\t[bucket]:  Bucket associated with EC2 instance to deconfigure. Only include the bucket name (not the s3:// prefix)."
    echo ""
    echo "Effect:  Terminates the EC2 instance and deletes the associated user, group, and keys. Does not delete the bucket or its data."
    exit 1
fi

iamReadOnlyUserName="$1-ro-user"
iamReadOnlyGroupName="$1-ro-group"

# remove read-only user from read-only group
echo "Removing read-only user from read-only group..."
aws iam remove-user-from-group --user-name $iamReadOnlyUserName --group-name $iamReadOnlyGroupName
if [ $? -ne 0 ]; then
    echo "Failed to remove read-only user from read-only group."
fi

# delete access keys for read-only user
echo "Deleting access keys from read-only user..."
accessKeyIDs=$(aws iam list-access-keys --user-name $iamReadOnlyUserName --query "AccessKeyMetadata[].AccessKeyId" --output text | tr '\t' '\n')
for accessKeyID in $accessKeyIDs
do
    aws iam delete-access-key --access-key $accessKeyID --user-name $iamReadOnlyUserName
    if [ $? -ne 0 ]; then
	echo "Failed to delete access key."
    fi
done

# delete read-only user
echo "Deleting read-only IAM user..."
aws iam delete-user --user-name $iamReadOnlyUserName
if [ $? -ne 0 ]; then
    echo "Failed to delete read-only IAM user."
fi

# delete read-only group policy
echo "Deleting read-only IAM group policy..."
aws iam delete-group-policy --group-name $iamReadOnlyGroupName --policy-name "${iamReadOnlyGroupName}-policy"
if [ $? -ne 0 ]; then
    echo "Failed to delete read-only IAM group policy."
fi

# delete read-only group
echo "Deleting IAM group..."
aws iam delete-group --group-name $iamReadOnlyGroupName
if [ $? -ne 0 ]; then
    echo "Failed to delete read-only IAM group."
fi

# terminate instance
echo "Terminating instance..."
instanceId=$(aws ec2 describe-instances --filters "Name=tag:Name,Values=$1" --output text --query "Reservations[*].Instances[*].InstanceId")
aws ec2 terminate-instances --instance-ids $instanceId
aws ec2 wait instance-terminated --instance-ids $instanceId

# delete key pair and security group
echo "Deleting key pair and security group..."
aws ec2 delete-key-pair --key-name $1
aws ec2 delete-security-group --group-name $1