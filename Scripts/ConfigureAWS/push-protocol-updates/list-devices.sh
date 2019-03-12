#!/bin/sh

if [[ $# -ne 1 ]]
then
    echo ""
    echo "Purpose:  Lists devices accepting push notifications. Does not read from standard input."
    echo ""
    echo "Usage:  ./list-devices.sh [BUCKET]"
    echo ""
    echo "  [BUCKET]:  Name of S3 bucket for which to list devices."
    exit 1
fi

for token_path in $(find $1-push-notifications/tokens/*.json)
do
    device_id_protocol_id=$(basename "$token_path" ".json")
    device_id=$(echo $device_id_protocol_id | cut -f1 -d ":")
    protocol_id=$(echo $device_id_protocol_id | cut -f2 -d ":")
    format=$(jq -r '.format' $token_path)
    echo -e "$1\t$device_id\t$protocol_id\t$format"
done
