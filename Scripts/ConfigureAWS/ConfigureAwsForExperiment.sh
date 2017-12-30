#!/bin/sh

if [ $# -ne 2 ]; then
    echo "Usage:  ./ConfigureAwsForExperiment.sh [region] [root id]"
    echo "\t[region]:  AWS region to use (e.g., us-east-1)"
    echo "\t[root id]:  Account ID that will own the data (12 digits, no dashes)"
    exit 1
fi

# create random bucket in given region
echo "Creating S3 bucket..."
bucket=$(uuidgen | tr '[:upper:]' '[:lower:]')
aws s3api create-bucket --bucket $bucket --region $1
if [ $? -ne 0 ]; then
    echo "Failed to create bucket."
    exit $?
fi

# create IAM user
echo "Creating IAM user..."
iamUserName="${bucket}"
iamUserARN=$(aws iam create-user --user-name $iamUserName | jq -r .User.Arn)
if [ $? -ne 0 ]; then
    echo "Failed to create IAM user."
    exit $?
fi

# attach read-only policy for bucket to IAM user
cp ./IamPolicy.json tmp.json
sed -i "" "s/bucketName/$bucket/" ./tmp.json
aws iam put-user-policy --user-name $iamUserName --policy-name $iamUserName --policy-document file://tmp.json
if [ $? -ne 0 ]; then
    rm tmp.json
    echo "Failed to put IAM user policy."
    exit $?
fi
rm tmp.json

# give the user a bit to propagate, then attach bucket policy giving access to the root user and IAM user.
sleep 15
cp ./BucketPolicy.json tmp.json
sed -i "" "s/bucketId/$bucket/" ./tmp.json
sed -i "" "s/rootAccountId/$2/" ./tmp.json
sed -i "" "s#iamUserARN#$iamUserARN#" ./tmp.json
aws s3api put-bucket-policy --bucket $bucket --policy file://./tmp.json
if [ $? -ne 0 ]; then
    rm tmp.json
    echo "Failed to attach bucket policy."
    exit $?
fi
rm tmp.json

echo "All done. Bucket:  $bucket"