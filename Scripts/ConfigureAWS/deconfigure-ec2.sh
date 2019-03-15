#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./deconfigure-ec2.sh [bucket]"
    echo "\t[bucket]:  Bucket associated with EC2 instance to deconfigure. Only include the bucket name (not the s3:// prefix)."
    echo ""
    echo "Effect:  Terminates the EC2 instance and deletes the associated user, group, and keys. Does not delete the bucket or its data."
    exit 1
fi

iamBackendUserName="$1-b-user"
iamBackendGroupName="$1-b-group"

# remove backend user from backend group
echo "Removing backend user from backend group..."
aws iam remove-user-from-group --user-name $iamBackendUserName --group-name $iamBackendGroupName
if [ $? -ne 0 ]; then
    echo "Failed to remove backend user from backend group."
fi

# delete access keys for backend user
echo "Deleting access keys from backend user..."
accessKeyIDs=$(aws iam list-access-keys --user-name $iamBackendUserName --query "AccessKeyMetadata[].AccessKeyId" --output text | tr '\t' '\n')
for accessKeyID in $accessKeyIDs
do
    aws iam delete-access-key --access-key $accessKeyID --user-name $iamBackendUserName
    if [ $? -ne 0 ]; then
	echo "Failed to delete access key."
    fi
done

# delete backend user
echo "Deleting backend IAM user..."
aws iam delete-user --user-name $iamBackendUserName
if [ $? -ne 0 ]; then
    echo "Failed to delete backend IAM user."
fi

# delete backend group policy
echo "Deleting backend IAM group policy..."
aws iam delete-group-policy --group-name $iamBackendGroupName --policy-name "${iamBackendGroupName}-policy"
if [ $? -ne 0 ]; then
    echo "Failed to delete backend IAM group policy."
fi

# delete backend group
echo "Deleting IAM group..."
aws iam delete-group --group-name $iamBackendGroupName
if [ $? -ne 0 ]; then
    echo "Failed to delete backend IAM group."
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
rm -rf ${1}.pem