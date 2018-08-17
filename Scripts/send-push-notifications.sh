#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./send-push-notifications.sh [s3 bucket name]"
    echo "\t[s3 bucket name]:  S3 bucket for remote data store (e.g.:  some-bucket). Do not include the s3:// prefix."
    echo ""
    exit 1
fi

# sync notifications from s3 to local, deleting anything local that doesn't exist s3.
echo -e "\n************* DOWNLOADING FROM S3 *************"
s3_path="s3://$1/push-notifications"
notifications_dir="$1-push-notifications"
mkdir -p $notifications_dir
aws s3 sync $s3_path $notifications_dir --delete --exact-timestamps  # need the --exact-timestamps because the token files can be updated 
                                                                     # to be the same size and will not come down otherwise.
# get shared access signature.
sas=$(node get-sas.js)

# process push notification requests
echo -e "\n\n************* PROCESSING PNRs *************"
for n in $(ls $notifications_dir/*.json)
do

    # check if file is empty. this could be caused by a failed/interrupted file transfer to s3, and it could
    # also be the result of sensus zeroing out PNRs that need to be cancelled.
    if [ -s $n ]
    then
	echo -e "Processing $n ..."
    else
	echo -e "Empty request $n. Deleting file...\n"
	aws s3 rm "$s3_path/$(basename $n)"
	rm $n
	continue
    fi

    # parse out data fields
    device=$(jq -r '.device' $n)
    protocol=$(jq '.protocol' $n)  # retain JSON rather than using raw, as we'll use the value in JSON below and there might be escape characters.
    title=$(jq '.title' $n)        # retain JSON rather than using raw, as we'll use the value in JSON below and there might be escape characters.
    body=$(jq '.body' $n)          # retain JSON rather than using raw, as we'll use the value in JSON below and there might be escape characters.
    sound=$(jq '.sound' $n)        # retain JSON rather than using raw, as we'll use the value in JSON below and there might be escape characters.
    command=$(jq '.command' $n)    # retain JSON rather than using raw, as we'll use the value in JSON below and there might be escape characters.
    id=$(jq '.id' $n)              # retain JSON rather than using raw, as we'll use the value in JSON below and there might be escape characters.
    format=$(jq -r '.format' $n)
    time=$(jq -r '.time' $n)       # the value indicates unix time in seconds
	
    # the cron scheduler runs once per minute. we're going to be proactive and ensure that push notifications arrive
    # at the device no later than the desired time. thus, if the desired time precedes the current time OR if the
    # desired time precedes the next cron run time, go ahead and send the push notification. in addition, there will
    # be some amount of latency from the time of requesting the push notification to actual delivery. allow a minute
    # of latency plus a minute for the cron scheduler, for a total of two minutes.
    curr_time_seconds=$(date +%s)
    two_minutes=$((2 * 60))
    time_horizon=$(($curr_time_seconds + $two_minutes))
    if [ "$time" -le "$time_horizon" ]
    then

	# get the token for the device, which is stored in a file named as device:protocol (be sure to trim the 
	# leading/trailing quotes from the protocol)
	protocol_id=${protocol%\"}
	protocol_id=${protocol_id#\"}
	token=$(cat "$notifications_dir/${device}:${protocol_id}")

	# might not have a token, in cases where we failed to upload it or cleared it when stopping the protocol.
	if [[ "$token" = "" ]]
	then
	    echo -e "No token found. Assuming the PNR is stale and should be deleted.\n"
	    aws s3 rm "$s3_path/$(basename $n)"
	    rm $n
	    continue
	fi

	# get data payload depending on platform
	# 
	# android:  https://firebase.google.com/docs/cloud-messaging/concept-options#notifications_and_data_messages
	# ios:  https://developer.apple.com/library/archive/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/CreatingtheNotificationPayload.html#//apple_ref/doc/uid/TP40008194-CH10-SW1
	if [[ "$format" = "gcm" ]]
        then

            data=\
"{"\
"\"data\":"\
"{"\
"\"command\":$command,"\
"\"id\":$id,"\
"\"protocol\":$protocol,"\
"\"title\":$title,"\
"\"body\":$body,"\
"\"sound\":$sound"\
"}"\
"}"

        elif [[ "$format" = "apple" ]]
        then

            data=\
"{"\
"\"aps\":"\
"{"\
"\"content-available\":1,"\
"\"alert\":"\
"{"\
"\"title\":$title,"\
"\"body\":$body"\
"},"\
"\"sound\":$sound"\
"},"\
"\"command\":$command,"\
"\"id\":$id,"\
"\"protocol\":$protocol"\
"}"

        fi

	# send notification.
        response=$(curl --http1.1 --header "ServiceBusNotification-Format: $format" --header "ServiceBusNotification-DeviceHandle: $token" --header "x-ms-version: 2015-04" --header "Authorization: $sas" --header "Content-Type: application/json;charset=utf-8" --data "$data" -X POST "https://sensus-notifications.servicebus.windows.net/sensus-notifications/messages/?direct&api-version=2015-04" --write-out %{http_code} --silent --output /dev/null)
	
	# check status.
        if [[ "$response" = "201"  ]]
        then
            echo "Notification sent. Command:  $command"
	    echo -e "Removing file.\n"
	    aws s3 rm "$s3_path/$(basename $n)"
	    rm $n	    
        fi
    else
	echo -e "Push notification will be delivered in $(($time - $time_horizon)) seconds.\n"
    fi
done
