#!/bin/sh

# check options
if [ $# -ne 4 ]; then
    echo "Usage:  ./send-push-notifications.sh [s3 bucket name] [azure namespace] [azure hub]"
    echo "\t[s3 bucket name]:  S3 bucket for remote data store (e.g.:  some-bucket). Do not include the s3:// prefix or trailing forward slashes."
    echo "\t[azure namespace]:  Azure push notification namespace."
    echo "\t[azure hub]:  Azure push notification hub."
    echo "\t[azure key]:  Azure push notification full access key."
    echo ""
    exit 1
fi

# we don't want this script to run more than once concurrently. check if lock file exists...
lockfile="send-notifications.lock"
if [ -f "$lockfile" ]
then
    # ...if it does, exit.
    echo "Lock file present. Script already running."
    exit 0
else
    # ...if it does not, create it.
    echo "Script not running. Creating lock file."
    touch $lockfile

    echo "Started $(date)"

    # delete lock file when script exits
    trap "{ rm -f $lockfile; printf \"Finished \"; date; }" EXIT
fi

# sync notifications from s3 to local, deleting anything local that doesn't exist s3.
echo -e "\n************* DOWNLOADING PNRS FROM S3 *************"
s3_path="s3://$1/push-notifications"
notifications_dir="$1-push-notifications"
mkdir -p $notifications_dir
aws s3 sync $s3_path $notifications_dir --delete --exact-timestamps  # need the --exact-timestamps because the token files can be updated 
                                                                     # to be the same size and will not come down otherwise.

# get push notifications reverse sorted by creation time. we're going to process the most recently created notifications first.
file_list=$(mktemp)
for n in $(ls $notifications_dir/*.json)
do

    # for backwards compability:  cover our previous approach in which there was only a "time"
    # field indicating the target notification time. this previous approach was not correct
    # because it did not account for push notifications scheduled into the future from 
    # previous executions of the protocol. for example, if a protocol with surveys is started
    # and schedules a survey for 1/1/2018 at 9pm, and if this protocol is restarted and
    # schedules the same survey for 1/1/2018 at 6pm, then the first schedule (with the later
    # notification time) would be processed first and invalidate the appropriate notification.
    sort_time=$(jq -r '."creation-time"' $n)
    if [ "$sort_time" = "null" ] 
    then
	# fall back to the notification time (previous approach)
	sort_time=$(jq -r '.time' $n)
    fi

    echo "$sort_time $n"

# reverse sort by the first field and output the second field (path)
done | sort -n -r -k1 | cut -f2 -d " " > $file_list

# get shared access signature
sas=$(node get-sas.js $2 $3 $4)

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
	aws s3 rm "$s3_path/$(basename $n)" &
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
    time=$(jq -r '.time' $n)       # the value indicates unix time in seconds.

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
	    aws s3 rm "$s3_path/$(basename $n)" &
	    rm $n
	    echo ""
	    continue
	else
	    echo "New command class:  $command_class (time $time)."
	    processed_command_classes[$command_class]=1
	fi
    fi
	
    # the cron scheduler runs periodically. we're going to be proactive and try to ensure that push notifications 
    # arrive at the device no later than the desired time. this is important, particularly on iOS where the arrival
    # of push notifications cancels local notifications that disrupt the user. set up a buffer accounting for the 
    # following latencies:
    #   
    #   * interval between cron runs:  5 minutes
    #   * native push notification infrastructure:  1 minute
    #
    # we don't know exactly how long it will take for the current script to make a pass through
    # the notifications. this will depend on deployment size and the protocol.
    curr_time_seconds=$(date +%s)
    buffer_seconds=$((6 * 60))
    time_horizon=$(($curr_time_seconds + $buffer_seconds))
    seconds_until_delivery=$(($time - $time_horizon))

    # delete any push notifications that have failed for an entire day
    seconds_in_day=$((60 * 60 * 24 * -1))
    if [ "$seconds_until_delivery" -le "$seconds_in_day" ]
    then
	echo "Push notification has failed for a day. Deleting it."
	aws s3 rm "$s3_path/$(basename $n)" &
	rm $n
	echo ""
	continue
    fi

    # check whether the delivery time has arrived or passed
    if [ "$seconds_until_delivery" -le 0 ]
    then

	# get the token for the device, which is stored in a file named as device:protocol (be sure to trim the 
	# leading/trailing quotes from the protocol)
	protocol_id=${protocol%\"}
	protocol_id=${protocol_id#\"}
	token=$(cat "$notifications_dir/${device}:${protocol_id}")

	# might not have a token, in cases where we failed to upload it or cleared it when stopping the protocol.
	if [[ "$token" = "" ]]
	then
	    echo "No token found. Assuming the PNR is stale and should be deleted."
	    aws s3 rm "$s3_path/$(basename $n)" &
	    rm $n
	    echo ""
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

	# send notification from background
        curl --http1.1 --header "ServiceBusNotification-Format: $format" --header "ServiceBusNotification-DeviceHandle: $token" --header "x-ms-version: 2015-04" --header "Authorization: $sas" --header "Content-Type: application/json;charset=utf-8" --data "$data" -X POST "https://$2.servicebus.windows.net/$3/messages/?direct&api-version=2015-04" &
	
	echo -e "Notification requested.\n"

    else
	echo -e "Push notification will be delivered in $seconds_until_delivery seconds.\n"
    fi
done < $file_list

rm $file_list
