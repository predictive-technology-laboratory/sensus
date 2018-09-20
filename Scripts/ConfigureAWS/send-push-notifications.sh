#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./send-push-notifications.sh [s3 bucket name]"
    echo "\t[s3 bucket name]:  S3 bucket for remote data store (e.g.:  some-bucket). Do not include the s3:// prefix."
    echo ""
    exit 1
fi

# sync notifications from s3 to local, deleting anything local that doesn't exist s3.
echo -e "\n************* DOWNLOADING PNRS FROM S3 *************"
s3_path="s3://$1/push-notifications"
notifications_dir="$1-push-notifications"
mkdir -p $notifications_dir
aws s3 sync $s3_path $notifications_dir --delete --exact-timestamps  # need the --exact-timestamps because the token files can be updated 
                                                                     # to be the same size and will not come down otherwise.
# get push notifications reverse sorted by time (newest first)
file_list=$(mktemp)
for n in $(ls $notifications_dir/*.json)
do

    time=$(jq -r '.time' $n)
    echo "$time $n"

# reverse sort by the first field (time) and output the second field (path)
done | sort -n -r -k1 | cut -f2 -d " " > $file_list

# get access authentication for azure and firebase cloud messaging
azure_sas=$(node get-sas.js)
fcm_token=$(./get-fcm-token.py ./fcm-service-account.json)

# process push notification requests
declare -A processed_command_classes
echo -e "\n\n************* PROCESSING PNRs *************"
while read n
do

    # check if file is empty. this could be caused by a failed/interrupted file transfer to s3, and it could
    # also be the result of sensus zeroing out PNRs that need to be cancelled.
    if [ -s $n ]
    then
	echo -e "Processing $n ..."
    else
	echo "Empty request $n. Deleting file..."
	aws s3 rm "$s3_path/$(basename $n)"
	rm $n
	echo ""
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
    hub=$(jq -r '.hub' $n)         # the hub we we should use to distribute the push notification request

    # if this is a push notification command, check if we've already sent a push notification 
    # for the command class (everything except for the invocation ID). we're processing the
    # push notification requests with newest times first, so if we have already processed the
    # command class then we can safely ignore all others as they are older and obsolete.
    command_class=${command%|*}        # strip the invocation ID
    command_class=${command_class#\"}  # strip the leading double-quote (retained above)
    command_class=${command_class%\"}  # strip the trailing double-quote (retained above)
    if [[ $command_class = "" ]]
    then
	echo "No command found."
    else
	if [[ ${processed_command_classes[$command_class]} ]]
	then
	    echo "Obsolete command class $command_class (time $time). Deleting file..."
	    aws s3 rm "$s3_path/$(basename $n)"
	    rm $n
	    echo ""
	    continue
	else
	    echo "New command class:  $command_class (time $time)."
	    processed_command_classes[$command_class]=1
	fi
    fi
	
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

	# get the token for the device, which is stored in a file named as hub:device:protocol (be sure to trim the 
	# leading/trailing quotes from the protocol)
	protocol_id=${protocol%\"}
	protocol_id=${protocol_id#\"}
	token=$(cat "$notifications_dir/${hub}:${device}:${protocol_id}")

	# might not have a token, in cases where we failed to upload it or cleared it when stopping the protocol.
	if [[ "$token" = "" ]]
	then
	    echo "No token found. Assuming the PNR is stale and should be deleted."
	    aws s3 rm "$s3_path/$(basename $n)"
	    rm $n
	    echo ""
	    continue
	fi

	# get android payload:  https://firebase.google.com/docs/cloud-messaging/concept-options#notifications_and_data_messages
        fcm_payload=\
"\"data\":"\
"{"\
"\"command\":$command,"\
"\"id\":$id,"\
"\"protocol\":$protocol,"\
"\"title\":$title,"\
"\"body\":$body,"\
"\"sound\":$sound"\
"}"
        # get ios payload:  https://developer.apple.com/library/archive/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/CreatingtheNotificationPayload.html#//apple_ref/doc/uid/TP40008194-CH10-SW1
        apple_payload=\
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
"\"protocol\":$protocol"

	# send notification via azure
	if [[ "$hub" == "Azure" ]]
	then
	    
	    # select desired format
	    if [[ "$format" == "gcm" ]]
	    then
		payload=$gcm_payload
	    elif [[ "$format" == "apple" ]]
	    then
		payload=$apple_payload
	    fi
	    
	    # submit push notification
            response=$(curl --http1.1 --header "ServiceBusNotification-Format: $format" --header "ServiceBusNotification-DeviceHandle: $token" --header "x-ms-version: 2015-04" --header "Authorization: $azure_sas" --header "Content-Type: application/json;charset=utf-8" --data "{$payload}" -X POST "https://sensus-notifications.servicebus.windows.net/sensus-notifications/messages/?direct&api-version=2015-04")

	# send notification via firebase
	elif [[ "$hub" == "FirebaseCloudMessaging" ]]
	then

	    # submit push notification
	    response=$(curl -X POST -H "Authorization: Bearer $fcm_token" -H "Content-Type: application/json" --data '
{
  "message":
  {
    "token": "'"$token"'",
    "android":
    {
      "priority": "high",
      "ttl": "0s",
      '"$fcm_payload"'
    },
    "apns":
    {
      "headers":
      {
        "apns-priority": "10"
      },
      "payload":'"{$apple_payload}"'
    }
  }
}' https://fcm.googleapis.com/v1/projects/sensus-1022/messages:send)

	fi

	echo -e "$response\n"

    else

	echo -e "Push notification will be delivered in $(($time - $time_horizon)) seconds.\n"

    fi
done < $file_list

rm $file_list
