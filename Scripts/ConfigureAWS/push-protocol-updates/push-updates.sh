#!/bin/sh

if [[ $# -ne 2 ]]
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
    # 1) add protocol updates to S3 overwiting the given id if it already exists
    bucket=$(echo -e $bucket_device_protocol | cut -f1 -d ' ')
    device=$(echo -e $bucket_device_protocol | cut -f2 -d ' ')
    protocol=$(echo -e $bucket_device_protocol | cut -f3 -d ' ')
    update_id=${device}-${protocol}-$2
    aws s3 cp $1 s3://$bucket/protocol-updates/$update_id

    # 2) request push notification to alert app about updates

    # ios device IDs have dashes in them whereas android device IDs do not. it's
    # probably not a great idea to hack out the OS type from the device identier
    # as this may change in the future. the proper way to do this is to include
    # the OS type in the token file that the device sends to S3 when registering
    # for push notifications.
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
"\"command\": \"UPDATE-PROTOCOL|$update_id\","\
"\"command-id\": \"$update_id\","\
"\"format\": \"$format\","\
"\"creation-time\": $current_time_seconds,"\
"\"time\": $current_time_seconds"\
"}" > $push_notification_file

    aws s3 cp $push_notification_file s3://$bucket/push-notifications/${push_notification_id}.json
    rm $push_notification_file
    
done




