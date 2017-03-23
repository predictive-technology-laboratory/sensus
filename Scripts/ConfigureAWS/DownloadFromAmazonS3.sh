#!/bin/sh

if [ $# -ne 2 ]; then
    echo "Usage:  ./DownloadFromAmazonS3.sh [bucket] [directory]"
    echo "\t[bucket]:  Bucket to download (or partial path including bucket)"
    echo "\t[directory]:  Directory to download data into"
    echo ""
    echo "For example:  ./DownloadFromAmazonS3.sh sensus/test data"
    exit 1
fi

aws s3 cp --recursive s3://$1 $2
