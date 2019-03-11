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

# set up directory names
requests_dir="requests"
tokens_dir="tokens"

# set up s3 paths
s3_path="s3://$1/push-notifications"
s3_requests_path="$s3_path/$requests_dir"
s3_tokens_path="$s3_path/$tokens_dir"

# set up local paths
local_path="$1-push-notifications"
local_requests_path="$local_path/$requests_dir"
local_tokens_path="$local_path/$tokens_dir"

# sync notifications from s3 to local, deleting anything local that doesn't exist s3.
echo -e "\n************* DOWNLOADING PNRS FROM S3 *************"
mkdir -p $local_path
aws s3 sync $s3_path $local_path --delete --exact-timestamps  # need the --exact-timestamps because the token files can be updated 
                                                                     # to be the same size and will not come down otherwise.

# get push notification requests reverse sorted by creation time. we're going to process the most recently created notifications first.
file_list=$(mktemp)
for n in $(ls $local_requests_path)
do
    sort_time=$(jq -r '."creation-time"' $n)
    echo "$sort_time $n"

# reverse sort by the creation time (newest creations first) and output the second field (path)
done | sort -n -r -k1 | cut -f2 -d " " > $file_list

# get shared access signature
sas=$(node get-sas.js $2 $3 $4)

# process push notification requests
declare -A processed_ids
echo -e "\n\n************* PROCESSING PNRs *************"
while read n
do
    # check if the request file is empty. this could be caused by a failed/interrupted 
    # file transfer to s3. it could also be the result of sensus zeroing out PNRs that 
    # need to be cancelled.
    if [ -s $n ]
    then
	echo -e "Processing $n ..."
    else
	echo "Empty request $n. Deleting file..."
	aws s3 rm "$s3_requests_path/$(basename $n)" &
	rm $n
	echo ""
	continue
    fi

    # extract JSON field values that we'll be using below. use -r to return 
    # unquoted/unescaped string values rather than quoted/escaped JSON strings.
    device=$(jq -r '.device' $n)
    format=$(jq -r '.format' $n)
    time=$(jq -r '.time' $n)

    # extract other JSON field values. we'll use these to form JSON, so retain
    # the values in their quoted/escaped forms.
    id=$(jq '.id' $n)
    protocol=$(jq '.protocol' $n)
    title=$(jq '.title' $n)
    body=$(jq '.body' $n)
    sound=$(jq '.sound' $n)
    command=$(jq '.command' $n)

    # check whether we've already processed the push notification id.
    id_value=$(jq -r '.id' $n)
    if [[ ${processed_ids[$id_value]} ]]
    then
	echo "Obsolete request identifier $id_value (time $time). Deleting file..."
	aws s3 rm "$s3_requests_path/$(basename $n)" &
	rm $n
	echo ""
	continue
    else
	echo "New request identifier:  $id_value (time $time)."
	processed_ids[$id_value]=1
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
	aws s3 rm "$s3_requests_path/$(basename $n)" &
	rm $n
	echo ""
	continue
    fi

    # check whether the delivery time has arrived or passed
    if [ "$seconds_until_delivery" -le 0 ]
    then

	# get the token for the device
	protocol_value=$(jq -r '.protocol' $n)
	token_json=$(cat "$local_tokens_path/${device}:${protocol_value}.json")
	token=$(echo $token_json | jq -r '.token')

	# might not have a token, in cases where we failed to upload it or cleared it when stopping the protocol.
	if [[ "$token" = "" ]]
	then
	    echo "No token found. Assuming the PNR is stale and should be deleted."
	    aws s3 rm "$s3_requests_path/$(basename $n)" &
	    rm $n
	    echo ""
	    continue
	fi

	# the backend key is the file name without the extension. this value
	# is used by the app upon receipt to delete the push notification
	# request from the s3 bucket.
	backend_key=$(basename $n ".json")

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
"\"backend-key\":$backend_key,"\
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
"\"backend-key\":$backend_key,"\
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
