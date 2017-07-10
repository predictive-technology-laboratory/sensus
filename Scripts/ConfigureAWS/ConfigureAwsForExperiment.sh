#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./ConfigureAwsForExperiment.sh [region]"
    echo "\t[region]:  AWS region to use (e.g., us-east-1)"
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
aws s3api put-bucket-policy --bucket $bucket --policy file://./tmp.json
if [ $? -ne 0 ]; then
    rm tmp.json
    echo "Failed to attach bucket policy."
    exit $?
fi
rm tmp.json

echo "All done. Bucket:  $bucket"
