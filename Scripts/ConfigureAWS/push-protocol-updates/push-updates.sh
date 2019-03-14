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

updates_file=$1
update_id=$2

while read bucket_device_protocol_format
do

    bucket=$(echo -e $bucket_device_protocol_format | cut -f1 -d ' ')
    device=$(echo -e $bucket_device_protocol_format | cut -f2 -d ' ')
    protocol=$(echo -e $bucket_device_protocol_format | cut -f3 -d ' ')
    format=$(echo -e $bucket_device_protocol_format | cut -f4 -d ' ')
    update_id=${device}-${protocol}-${update_id}
    update_content=$(<$updates_file)
    
    current_time_seconds=$(date +%s)
    push_notification_file=$(mktemp)

    echo \
"{"\
"\"id\": \"$update_id\","\
"\"device\": \"$device\","\
"\"protocol\": \"$protocol\","\
"\"format\": \"$format\","\
"\"creation-time\": $current_time_seconds,"\
"\"time\": $current_time_seconds,"\
"\"update\":"\
"{"\
"\"type\":\"Protocol\","\
"\"content\":$update_content"\
"}"\
"}" > $push_notification_file

    aws s3 cp $push_notification_file s3://$bucket/push-notifications/requests/$(uuidgen).json
    rm $push_notification_file
    
done




