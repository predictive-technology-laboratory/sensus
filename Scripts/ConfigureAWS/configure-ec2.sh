#!/bin/sh

if [ $# -ne 7 ]; then
    echo "Usage:  ./configure-ec2.sh [bucket] [cidr ingress] [image id] [instance type] [azure namespace] [azure hub] [azure notification hub full access signature]"
    echo "\t[bucket]:  Bucket configured using the configure-s3.sh script (e.g., test-bucket-234234234-23423423-423423)."
    echo "\t[cidr ingress]:  SSH ingress range for firewall, in CIDR format (e.g., 123.456.0.0/16 to allow access from the 123.456.*.* subnet, or 0.0.0.0/0 to allow access from any IP address)."
    echo "\t[image id]:  Image ID to use (e.g., ami-a4c7edb2, see AWS EC2 image list on the web)."
    echo "\t[instance type]:  Instance type (e.g., t2.micro)."
    echo "\t[azure namespace]:  The Azure push notification namespace name. See Push Notification documentation. Can be ignored by using \"\"."
    echo "\t[azure hub]:  The Azure push notification hub name. See Push Notification documentation. Can be ignored by using \"\"."
    echo "\t[azure notification hub full access key]:  The value of the DefaultFullSharedAccessSignature key (e.g., cVRantasldfkjaslkj3flkjelfrz+a3lkjflkj=). See Push notification documentation. Can be ignored by using \"\"."
    echo ""
    echo "Effect:  Configures an EC2 instance with an IAM group/user that has access to the given S3 bucket and monitors the bucket for push notifications."
    exit 1
fi

bucket=$1

########################
##### EC2 instance #####
########################

# create key pair and secure it
echo "Creating EC2 key pair..."
keyPairName=$bucket
pemFileName="${keyPairName}.pem"
aws ec2 create-key-pair --key-name $keyPairName  --query 'KeyMaterial' --output text > $pemFileName
chmod 400 $pemFileName

# create security group with SSH inbound rule
echo "Creating EC2 security group..."
securityGroupName=$bucket
aws ec2 create-security-group --group-name $securityGroupName --description "$securityGroupName Security Group"
aws ec2 authorize-security-group-ingress --group-name $securityGroupName --protocol tcp --port 22 --cidr $2

# launch ec2 instance and wait for it to start
echo "Launching EC2 instance..."
instanceId=$(aws ec2 run-instances --image-id $3 --count 1 --instance-type $4 --key-name $keyPairName --security-groups $securityGroupName | jq -r .Instances[0].InstanceId)
aws ec2 wait instance-running --instance-ids $instanceId
sleep 30
aws ec2 create-tags --resources $instanceId --tags Key=Name,Value=$bucket
publicIP=$(aws ec2 describe-instances --instance-ids $instanceId --query "Reservations[*].Instances[*].PublicIpAddress" --output=text)

##########################
##### IAM group/user #####
##########################

# create group
echo "Creating backend IAM group..."
iamBackendGroupName="${bucket}-b-group"
aws iam create-group --group-name $iamBackendGroupName
if [ $? -ne 0 ]; then
    echo "Failed to create backend IAM group."
    exit $?
fi

# create/put group policy
echo "Attaching backend IAM group policy..."
cp ./iam-backend-policy.json tmp.json
sed "s/bucketName/$bucket/" ./tmp.json > ./tmp2.json
mv tmp2.json tmp.json
aws iam put-group-policy --group-name $iamBackendGroupName --policy-document file://tmp.json --policy-name "${iamBackendGroupName}-policy"
if [ $? -ne 0 ]; then
    rm tmp.json
    echo "Failed to put IAM backend group policy."
    exit $?
fi
rm tmp.json

# create backend IAM user
echo "Creating backend IAM user..."
iamBackendUserName="${bucket}-b-user"
aws iam create-user --user-name $iamBackendUserName
if [ $? -ne 0 ]; then
    echo "Failed to create backend IAM user."
    exit $?
fi

# create access key for user
echo "Creating access key for backend IAM user..."
iamAccessKeyJSON=$(aws iam create-access-key --user-name $iamBackendUserName)
iamAccessKeyID=$(echo $iamAccessKeyJSON | jq -r .AccessKey.AccessKeyId)
iamAccessKeySecret=$(echo $iamAccessKeyJSON | jq -r .AccessKey.SecretAccessKey)

# add user to group
echo "Adding backend IAM user to backend IAM group..."
aws iam add-user-to-group --user-name $iamBackendUserName --group-name $iamBackendGroupName
if [ $? -ne 0 ]; then
    echo "Failed to add backend IAM user to backend group."
    exit $?
fi

# upload IAM credentials to EC2 instance
echo "Uploading IAM credentials to EC2 instance..."
cp credentials tmp
sed "s/keyId/$iamAccessKeyID/" tmp > tmp2
mv tmp2 tmp
sed "s#keySecret#$iamAccessKeySecret#" tmp > tmp2
mv tmp2 tmp
ssh -i $pemFileName ec2-user@$publicIP "mkdir .aws"
scp -i $pemFileName tmp ec2-user@$publicIP:~/.aws/credentials
rm tmp

##############################
##### Push Notifications #####
##############################

# upload push notification processor and supporting tools
scp -i $pemFileName send-push-notifications.sh ec2-user@$publicIP:~/
ssh -i $pemFileName ec2-user@$publicIP "chmod +x send-push-notifications.sh"
scp -i $pemFileName get-sas.js ec2-user@$publicIP:~/
scp -i $pemFileName -r push-protocol-updates/* ec2-user@$publicIP:~/
ssh -i $pemFileName ec2-user@$publicIP "sudo yum -y install jq emacs"
ssh -i $pemFileName ec2-user@$publicIP "echo \"export EDITOR=\\\"emacs -nw\\\"\" >> ~/.bash_profile"
ssh -i $pemFileName ec2-user@$publicIP "curl -o- https://raw.githubusercontent.com/creationix/nvm/v0.33.8/install.sh | bash && . ~/.nvm/nvm.sh && nvm install 8.11.2"

# configure crontab to run push notification processor using the get-sas script
sed "s/BUCKET/$bucket/" push-notification-crontab > tmp
sed "s/NAMESPACE/$5/" tmp > tmp2
sed "s/HUB/$6/" tmp2 > tmp
sed "s#KEY#$7#" tmp > tmp2
mv tmp2 tmp
scp -i $pemFileName tmp ec2-user@$publicIP:~/push-notification-crontab
ssh -i $pemFileName ec2-user@$publicIP "crontab push-notification-crontab"
ssh -i $pemFileName ec2-user@$publicIP "rm push-notification-crontab"
rm tmp

# done
echo "EC2 instance is ready at $publicIP using private PEM file ${pemFileName}."
