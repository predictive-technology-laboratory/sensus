#!/bin/sh

if [ $# -ne 2 ]; then
    echo "Usage:  ./ConfigureAwsForExperiment.sh [region] [root id]"
    echo "\t[region]:  AWS region to use (e.g., us-east-1)"
    echo "\t[root id]:  Account ID that will own the data (12 digits, no dashes)"
    exit 1
fi

#########################
##### Create bucket #####
#########################

echo "Creating S3 bucket..."
bucket=$(uuidgen | tr '[:upper:]' '[:lower:]')
aws s3api create-bucket --bucket $bucket --region $1
if [ $? -ne 0 ]; then
    echo "Failed to create bucket."
    exit $?
fi

################################
##### Attach bucket policy #####
################################

cat ./BucketPolicy.json | sed "s/bucketId/$bucket/" > tmp.json
cat tmp.json | sed "s/rootAccountId/$2/" > tmp2.json
aws s3api put-bucket-policy --bucket $bucket --policy file://./tmp2.json
if [ $? -ne 0 ]; then
    rm tmp.json tmp2.json
    echo "Failed to attach bucket policy."
    exit $?
fi
rm tmp.json tmp2.json

echo "All done. Bucket:  $bucket"
