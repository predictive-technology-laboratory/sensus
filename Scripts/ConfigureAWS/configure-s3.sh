#!/bin/sh

if [ $# -ne 2 ]; then
    echo "Usage:  ./configure-s3.sh [name] [region]"
    echo "\t[name]:  Informative name for bucket (alphanumerics and dashes). Must be 11 characters or fewer."
    echo "\t[region]:  AWS region to use (e.g., us-east-1)"
    echo ""
    echo "Output:  The Sensus S3 IAM account that has access to the bucket."
    exit 1
fi

# ensure bucket name is short enough
if [[ ${#1} -ge 12 ]]; then
    echo "Error:  Bucket name must be 11 characters or fewer."
    exit 1
fi

#####################
##### S3 bucket #####
#####################

# create random bucket in given region, prefixed with the given name
echo "Creating S3 bucket..."
bucket="$1-$(uuidgen | tr '[:upper:]' '[:lower:]')"
aws s3api create-bucket --bucket $bucket --region $2
if [ $? -ne 0 ]; then
    echo "Failed to create bucket."
    exit $?
fi

# enable versioning on the bucket for data safety purposes (e.g., to prevent unintended deletion)
aws s3api put-bucket-versioning --bucket $bucket --versioning-configuration Status=Enabled
if [ $? -ne 0 ]; then
    echo "Failed to enable bucket versioning."
    exit $?
fi

#################################
##### Device IAM group/user #####
#################################

# create group
echo "Creating device IAM group..."
iamDeviceGroupName="${bucket}-device-group"
aws iam create-group --group-name $iamDeviceGroupName
if [ $? -ne 0 ]; then
    echo "Failed to create device IAM group."
    exit $?
fi

# create/put group policy for devices
echo "Attaching device IAM group policy..."
cp ./iam-device-policy.json tmp.json
sed "s/bucketName/$bucket/" ./tmp.json > tmp2.json  # sed in place differs across platforms...avoid and use file redirect instead
mv tmp2.json tmp.json
aws iam put-group-policy --group-name $iamDeviceGroupName --policy-document file://tmp.json --policy-name "${iamDeviceGroupName}-policy"
if [ $? -ne 0 ]; then
    rm tmp.json
    echo "Failed to put IAM device group policy."
    exit $?
fi
rm tmp.json

# create device IAM user
echo "Creating device IAM user..."
iamDeviceUserName="${bucket}-device-user"
aws iam create-user --user-name $iamDeviceUserName
if [ $? -ne 0 ]; then
    echo "Failed to create device IAM user."
    exit $?
fi

# create access key for user
echo "Creating access key for device IAM user..."
deviceCredentials=$(aws iam create-access-key --user-name $iamDeviceUserName --query "AccessKey.[AccessKeyId,SecretAccessKey]" --output text | tr '\t' ':')
if [ $? -ne 0 ]; then
    echo "Failed to create access key for device IAM user."
    exit $?
fi

# add user to group
echo "Adding device IAM user to device IAM group..."
aws iam add-user-to-group --user-name $iamDeviceUserName --group-name $iamDeviceGroupName
if [ $? -ne 0 ]; then
    echo "Failed to add device IAM user to device group."
    exit $?
fi

echo "Done. Details:"
echo "  Sensus S3 bucket:  $bucket"
echo "  Sensus S3 IAM account:  $deviceCredentials"