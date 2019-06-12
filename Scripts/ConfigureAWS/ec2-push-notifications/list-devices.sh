#!/bin/sh

if [[ $# -ne 1 ]]
then
    echo ""
    echo "Purpose:  Lists devices accepting push notifications. Does not read from standard input."
    echo ""
    echo "Usage:  ./list-devices.sh [BUCKET]"
    echo ""
    echo "  [BUCKET]:  Name of S3 bucket for which to list devices."
    echo ""
    echo "Output:  One row per device, with each row containing the bucket, device identifier,"
    echo "         protocol identifier, and format identifier (Apple or Google)."
    exit 1
fi

bucket=$1

for token_path in $(find ${bucket}-push-notifications/tokens/*.json)
do
    device=$(cat $token_path | jq -r '.device')
    protocol=$(cat $token_path | jq -r '.protocol')
    format=$(cat $token_path | jq -r '.format')
    echo -e "$1\t$device\t$protocol\t$format"
done
