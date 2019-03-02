#!/bin/sh

if [[ $# -ne 1 ]]
then
    echo ""
    echo "Purpose:  Pushes an updates file to devices. Reads devices from standard input."
    echo ""
    echo "  Usage:  ./push-updates.sh [updates file] [update id]"
    echo ""
    exit 1
fi

while read bucket_device_protocol
do
    # add protocol updates to S3
    bucket=$(echo -e $bucket_device_protocol | cut -f1 -d ' ')
    device=$(echo -e $bucket_device_protocol | cut -f2 -d ' ')
    protocol=$(echo -e $bucket_device_protocol | cut -f3 -d ' ')
    aws s3 cp $1 s3://$bucket/protocol-updates/$device

    # request push notification to alert app about updates
    if [[ $device == *-* ]]
    then
	format="apple"
    else
	format="gcm"
    fi
    
    current_time_seconds=$(date +%s)

    push_notification_file=$(mktemp)
    push_notification_id=$(uuidgen)
    echo \
"{"\
"\"id\": \"$push_notification_id\","\
"\"device\": \"$device\","\
"\"protocol\": \"$protocol\","\
"\"title\": \"\","\
"\"body\": \"\","\
"\"sound\": \"\","\
"\"command\": \"UPDATE-PROTOCOL\","\
"\"command-class\": \"${device}-${protocol}-$2\","\
"\"format\": \"$format\","\
"\"creation-time\": $current_time_seconds,"\
"\"time\": $current_time_seconds"\
"}" > $push_notification_file

    aws s3 cp $push_notification_file s3://$bucket/push-notifications/${push_notification_id}.json
    rm $push_notification_file
    
done




