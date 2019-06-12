#!/bin/sh

if [[ $# -ne 2 ]]
then
    echo ""
    echo "Purpose:  Requests user-facing push notifications. Reads devices from standard input."
    echo ""
    echo "Usage:  ./request-message.sh [TITLE] [BODY]"
    echo ""
    echo "  [TITLE]:  Title of push notifications."
    echo "   [BODY]:  Message body of notifications."
    echo ""
    exit 1
fi

title=$1
body=$2

while read bucket_device_protocol_format
do

    bucket=$(echo -e $bucket_device_protocol_format | cut -f1 -d ' ')
    device=$(echo -e $bucket_device_protocol_format | cut -f2 -d ' ')
    protocol=$(echo -e $bucket_device_protocol_format | cut -f3 -d ' ')
    format=$(echo -e $bucket_device_protocol_format | cut -f4 -d ' ')
    id=$(uuidgen)
    current_time_seconds=$(date +%s)
    request_file=$(mktemp)

    echo \
"{"\
"\"id\":\"$id\","\
"\"device\":\"$device\","\
"\"protocol\":\"$protocol\","\
"\"title\":\"$title\","\
"\"body\":\"$body\","\
"\"sound\":\"default\","\
"\"format\":\"$format\","\
"\"creation-time\":$current_time_seconds,"\
"\"time\":$current_time_seconds"\
"}" > $request_file

    # upload to s3 folder and remove local file. use the request identifier
    # as the s3 file name, so that subsequent requests with the same identifier
    # overwrite the older ones.
    aws s3 cp $request_file s3://$bucket/push-notifications/requests/${id}.json
    rm $request_file

done