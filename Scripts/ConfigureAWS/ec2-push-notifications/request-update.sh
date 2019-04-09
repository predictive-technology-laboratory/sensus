#!/bin/sh

if [[ $# -ne 3 ]]
then
    echo ""
    echo "Purpose:  Requests that an update push notification be sent. Reads devices from standard input."
    echo ""
    echo "Usage:  ./request-update.sh [UPDATE TYPE] [UPDATES FILE] [UPDATE ID]"
    echo ""
    echo "   [UPDATE TYPE]:  Type of update:  https://predictive-technology-laboratory.github.io/sensus/api/Sensus.Notifications.PushNotificationUpdateType.html"
    echo "  [UPDATES FILE]:  Path to file containing update content to send. Use /dev/null for to send an empty content payload."
    echo "     [UPDATE ID]:  Identifier for update (alphanumerics and dashes only)."
    exit 1
fi

update_type=$1
updates_file=$2
update_id=$3

while read bucket_device_protocol_format
do

    bucket=$(echo -e $bucket_device_protocol_format | cut -f1 -d ' ')
    device=$(echo -e $bucket_device_protocol_format | cut -f2 -d ' ')
    protocol=$(echo -e $bucket_device_protocol_format | cut -f3 -d ' ')
    format=$(echo -e $bucket_device_protocol_format | cut -f4 -d ' ')

    update_content=$(<$updates_file)
    current_time_seconds=$(date +%s)
    request_file=$(mktemp)
    
    # updates come in with an identifier that is nonspecific to a 
    # protocol or device. because the send-push-notifications.sh
    # script deduplicates push notification requests by id, we need
    # to make the request identifier specific to a device and protocol.
    update_id=${device}-${protocol}-${update_id}

    echo \
"{"\
"\"id\":\"$update_id\","\
"\"device\":\"$device\","\
"\"protocol\":\"$protocol\","\
"\"format\":\"$format\","\
"\"creation-time\":$current_time_seconds,"\
"\"time\":$current_time_seconds,"\
"\"update\":"\
"{"\
"\"type\":\"$update_type\","\
"\"content\":$update_content"\
"}"\
"}" > $request_file

    # upload to s3 using an arbitrary file identifier. the app will take care
    # of deduplicating updates based on identifiers and keeping only the newest
    # among a set of updates with the same identifiers.
    aws s3 cp $request_file s3://$bucket/push-notifications/requests/$(uuidgen).json
    rm $request_file
    
done




