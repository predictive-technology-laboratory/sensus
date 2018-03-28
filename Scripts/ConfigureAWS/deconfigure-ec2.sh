#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./deconfigure-ec2.sh [bucket]"
    echo "\t[bucket]:  Bucket containing data"
    exit 1
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