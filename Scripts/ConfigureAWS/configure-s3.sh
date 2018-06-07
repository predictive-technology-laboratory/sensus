#!/bin/sh

if [ $# -ne 2 ]; then
    echo "Usage:  ./configure-s3.sh [name] [region]"
    echo "\t[name]:  Informative name for bucket (alphanumerics and dashes)"
    echo "\t[region]:  AWS region to use (e.g., us-east-1)"
    echo ""
    echo "Output:  The Sensus S3 IAM account that has write-only access to the bucket."
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

###################################################################################
##### Write-only IAM group/user:  enables the app to write data to the bucket #####
###################################################################################

# create group
echo "Creating write-only IAM group..."
iamWriteOnlyGroupName="${bucket}-write-only-group"
aws iam create-group --group-name $iamWriteOnlyGroupName
if [ $? -ne 0 ]; then
    echo "Failed to create write-only IAM group."
    exit $?
fi

# create/put group policy
echo "Attaching write-only IAM group policy..."
cp ./iam-write-only-group-policy.json tmp.json
sed -i "" "s/bucketName/$bucket/" ./tmp.json
aws iam put-group-policy --group-name $iamWriteOnlyGroupName --policy-document file://tmp.json --policy-name "${iamWriteOnlyGroupName}-policy"
if [ $? -ne 0 ]; then
    rm tmp.json
    echo "Failed to put IAM write-only group policy."
    exit $?
fi
rm tmp.json

# create write-only IAM user
echo "Creating write-only IAM user..."
iamWriteOnlyUserName="${bucket}-write-only-user"
aws iam create-user --user-name $iamWriteOnlyUserName
if [ $? -ne 0 ]; then
    echo "Failed to create write-only IAM user."
    exit $?
fi

# create access key for user
echo "Creating access key for write-only IAM user..."
writeOnlyCredentials=$(aws iam create-access-key --user-name $iamWriteOnlyUserName --query "AccessKey.[AccessKeyId,SecretAccessKey]" --output text | tr '\t' ':')
if [ $? -ne 0 ]; then
    echo "Failed to create access key for write-only IAM user."
    exit $?
fi

# add user to group
echo "Adding write-only IAM user to write-only IAM group..."
aws iam add-user-to-group --user-name $iamWriteOnlyUserName --group-name $iamWriteOnlyGroupName
if [ $? -ne 0 ]; then
    echo "Failed to add write-only IAM user to write-only group."
    exit $?
fi

echo "Done. Details:"
echo "  Sensus S3 bucket:  $bucket"
echo "  Sensus S3 IAM account:  $writeOnlyCredentials"