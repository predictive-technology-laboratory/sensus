#!/bin/sh

if [ $# -ne 4 ]; then
    echo "Usage:  ./configure-ec2.sh [bucket] [cidr ingress] [image id] [instance type]"
    echo "\t[bucket]:  Bucket containing data"
    echo "\t[cidr ingress]:  SSH ingress range, in CIDR format (e.g., 123.456.0.0/16)"
    echo "\t[image id]:  Image ID to use (e.g., ami-a4c7edb2)"
    echo "\t[instance type]:  Instance type (e.g., t2.micro)"
    exit 1
fi

# create key pair and secure it
keyPairName="$1"
pemFileName="${keyPairName}.pem"
aws ec2 create-key-pair --key-name $keyPairName  --query 'KeyMaterial' --output text > $pemFileName
chmod 400 $pemFileName

# create security group with SSH inbound rule
securityGroupName="$1"
aws ec2 create-security-group --group-name $securityGroupName --description "$securityGroupName Security Group"
aws ec2 authorize-security-group-ingress --group-name $securityGroupName --protocol tcp --port 22 --cidr $2

# launch ec2 instance and wait for it to start
instanceId=$(aws ec2 run-instances --image-id $3 --count 1 --instance-type $4 --key-name $keyPairName --security-groups $securityGroupName | jq -r .Instances[0].InstanceId)
aws ec2 wait instance-running --instance-ids $instanceId
sleep 15
aws ec2 create-tags --resources $instanceId --tags Key=Name,Value=$1
publicIP=$(aws ec2 describe-instances --instance-ids $instanceId --query "Reservations[*].Instances[*].PublicIpAddress" --output=text)

# create access key for IAM user
iamAccessKeyJSON=$(aws iam create-access-key --user-name $1)
iamAccessKeyID=$(echo $iamAccessKeyJSON | jq -r .AccessKey.AccessKeyId)
iamAccessKeySecret=$(echo $iamAccessKeyJSON | jq -r .AccessKey.SecretAccessKey)

# upload IAM credentials to EC2 instance
cp credentials tmp
sed -i "" s/keyId/$iamAccessKeyID/ tmp
sed -i "" s#keySecret#$iamAccessKeySecret# tmp
ssh -i $pemFileName ec2-user@$publicIP "mkdir .aws"
scp -i $pemFileName tmp ec2-user@$publicIP:~/.aws/credentials
rm tmp

# install linux packages
echo "Installing packages..."
ssh -i $pemFileName ec2-user@$publicIP "sudo yum -y install R libpng-devel libjpeg-turbo-devel openssl-devel emacs"

# install SensusR package
echo "Installing SensusR..."
ssh -i $pemFileName ec2-user@$publicIP "sudo R -e 'install.packages(\"SensusR\", repos = \"http://mirrors.nics.utk.edu/cran/\")'"

# done
echo "EC2 instance is ready at $publicIP using private PEM file ${pemFileName}."
