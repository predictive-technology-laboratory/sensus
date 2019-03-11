#!/bin/sh

if [[ $# -ne 2 ]]
then
    echo ""
    echo "Purpose:  Pushes an updates file to devices. Reads devices from standard input."
    echo ""
    echo "Usage:  ./push-updates.sh [UPDATES FILE] [UPDATE ID]"
    echo ""
    echo "  [UPDATES FILE]:  Path to file containing updates to send."
    echo "     [UPDATE ID]:  Identifier for update (alphanumerics and dashes only)."
    exit 1
fi

while read bucket_device_protocol_format
do
    # 1) add protocol updates to S3 overwriting the given id if it already exists

    bucket=$(echo -e $bucket_device_protocol_format | cut -f1 -d ' ')
    device=$(echo -e $bucket_device_protocol_format | cut -f2 -d ' ')
    protocol=$(echo -e $bucket_device_protocol_format | cut -f3 -d ' ')
    format=$(echo -e $bucket_device_protocol_format | cut -f4 -d ' ')
    update_id=${device}-${protocol}-$2
    aws s3 cp $1 s3://$bucket/protocol-updates/$update_id

    # 2) request push notification to alert app about updates
    
    current_time_seconds=$(date +%s)
    push_notification_file=$(mktemp)
    push_notification_id=

    echo \
"{"\
"\"id\": \"$update_id\","\
"\"device\": \"$device\","\
"\"protocol\": \"$protocol\","\
"\"title\": \"\","\
"\"body\": \"\","\
"\"sound\": \"\","\
"\"command\": \"UPDATE-PROTOCOL|$update_id\","\
"\"format\": \"$format\","\
"\"creation-time\": $current_time_seconds,"\
"\"time\": $current_time_seconds"\
"}" > $push_notification_file

    aws s3 cp $push_notification_file s3://$bucket/push-notifications/requests/$(uuidgen).json
    rm $push_notification_file
    
done




