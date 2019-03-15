#!/bin/sh

get_device_format() {
    
    # extract arguments
    local local_tokens_path=$1
    local device=$2

    cat "$local_tokens_path/${device}.json" | jq -r '.format'	
}

get_device_token() {
    
    # extract arguments
    local local_tokens_path=$1
    local device=$2

    cat "$local_tokens_path/${device}.json" | jq -r '.token'
}

get_device_protocol() {
    
    # extract arguments
    local local_tokens_path=$1
    local device=$2

    cat "$local_tokens_path/${device}.json" | jq -r '.protocol'
}

push_via_azure () {

    # extract arguments
    local format=$1       # literal
    local update=$2       # literal
    local id=$3           # JSON-formatted
    local backend_key=$4  # literal
    local protocol=$5     # literal
    local title=$6        # JSON-formatted
    local body=$7         # JSON-formatted
    local sound=$8        # JSON-formatted
    local token=$9        # literal
    
    # get data payload depending on platform
    # 
    # android:  https://firebase.google.com/docs/cloud-messaging/concept-options#notifications_and_data_messages
    # ios:  https://developer.apple.com/library/archive/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/CreatingtheNotificationPayload.html#//apple_ref/doc/uid/TP40008194-CH10-SW1
    if [[ "$format" = "gcm" ]]
    then

        local data=\
"{"\
"\"data\":"\
"{"\
"\"id\":$id,"\
"\"protocol\":\"$protocol\","\
"\"backend-key\":\"$backend_key\","\
"\"update\":\"$update\","\
"\"title\":$title,"\
"\"body\":$body,"\
"\"sound\":$sound"\
"}"\
"}"

    elif [[ "$format" = "apple" ]]
    then

        local data=\
"{"\
"\"id\":$id,"\
"\"protocol\":\"$protocol\","\
"\"backend-key\":\"$backend_key\","\
"\"update\":\"$update\","\
"\"aps\":"\
"{"\
"\"content-available\":1,"\
"\"alert\":"\
"{"\
"\"title\":$title,"\
"\"body\":$body"\
"},"\
"\"sound\":$sound"\
"}"\
"}"

    fi
    
    # send notification to azure
    curl --http1.1 --header "ServiceBusNotification-Format: $format" --header "ServiceBusNotification-DeviceHandle: $token" --header "x-ms-version: 2015-04" --header "Authorization: $sas" --header "Content-Type: application/json;charset=utf-8" --data "$data" -X POST "https://${namespace}.servicebus.windows.net/${hub}/messages/?direct&api-version=2015-04" &
    echo -e "Notification requested.\n"
}

delete_request () {

    # extract arguments
    local local_request_path=$1
    local message=$2

    echo -e "$message\n"

    s3_request_path="$s3_requests_path/$(basename $local_request_path)"
    aws s3 rm $s3_request_path &

    rm $local_request_path
}

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

# extract arguments
bucket=$1
namespace=$2
hub=$3
key=$4

# get shared access signature for communicating with azure endpoint
sas=$(node get-sas.js $namespace $hub $key)

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
push_notifications_dir="push-notifications"
requests_dir="requests"
tokens_dir="tokens"
updates_dir="updates"

# set up s3 paths
s3_notifications_path="s3://$bucket/$push_notifications_dir"
s3_requests_path="$s3_notifications_path/$requests_dir"
s3_tokens_path="$s3_notifications_path/$tokens_dir"
s3_updates_path="$s3_notifications_path/$updates_dir"

# set up local paths
local_notifications_path="$bucket-$push_notifications_dir"
local_requests_path="$local_notifications_path/$requests_dir"
local_tokens_path="$local_notifications_path/$tokens_dir"
local_updates_path="$local_notifications_path/$updates_dir"

# create special directory for updates created by this run of the script
new_updates_dir="new_updates"
mkdir -p $new_updates_dir
rm -rf $new_updates_dir/*

# sync notifications from s3 to local, deleting anything local that doesn't exist s3.
echo -e "\n************* DOWNLOADING REQUESTS FROM S3 *************"
mkdir -p $local_notifications_path
aws s3 sync $s3_notifications_path $local_notifications_path --delete --exact-timestamps  # need the --exact-timestamps because 
                                                                                          # the token files can be updated but 
                                                                                          # remain the same size. without this
                                                                                          # options such updates don't register.

# reverse sort push notification requests by creation time. we're going to 
# process the most recently created requests first and obsolete older requests
# with identifiers that have already been processed.
local_request_path_list=$(mktemp)
for local_request_path in $(find $local_requests_path/*.json)
do

    sort_time=$(jq -r '."creation-time"' $local_request_path)
    echo "$sort_time $local_request_path"

# reverse sort by the creation time (newest first) and output the path
done | sort -n -r -k1 | cut -f2 -d " " > $local_request_path_list

# there must be only one protocol for all requests in the bucket. check
# each request as we process it.
protocol=""

# process push notification requests
declare -A processed_ids
echo -e "\n\n************* PROCESSING REQUESTS *************"
while read local_request_path
do  

    # check if the request file is empty. this could be caused by a failed/interrupted 
    # file transfer to s3. it could also be the result of sensus zeroing out PNRs that 
    # need to be cancelled.
    if [ -s $local_request_path ]
    then
	echo -e "Processing $local_request_path"
    else
	delete_request $local_request_path "Empty request $local_request_path. Deleting file."
	continue
    fi

    # extract JSON field values that we'll be using below. use -r to return 
    # unquoted/unescaped string values rather than quoted/escaped JSON strings.
    device=$(jq -r '.device' $local_request_path)
    format=$(jq -r '.format' $local_request_path)
    time=$(jq -r '.time' $local_request_path)

    # check that all requests target the same protocol
    curr_protocol=$(jq -r '.protocol' $local_request_path)
    if [[ $protocol = "" ]]
    then
	protocol=$curr_protocol
    elif [[ $curr_protocol != $protocol ]]
    then
	echo "ERROR:  Current request targets unexpected protocol."
	continue
    fi

    # extract other JSON field values. we'll use these to form JSON, so retain
    # the values in their quoted/escaped forms (no -r option).
    id=$(jq '.id' $local_request_path)
    title=$(jq '.title' $local_request_path)
    body=$(jq '.body' $local_request_path)
    sound=$(jq '.sound' $local_request_path)
    update=$(jq '.update' $local_request_path)

    # check whether we've already processed the push notification id for a request that 
    # was newer. if we haven, then the current request is obsolete and can be removed.
    id_value=$(jq -r '.id' $local_request_path)
    if [[ ${processed_ids[$id_value]} ]]
    then
	delete_request $local_request_path "Obsolete request identifier $id_value (time $time). Deleting file."
	continue
    else
	echo "New request identifier $id_value (time $time)."
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
    if (( seconds_until_delivery <= seconds_in_day ))
    then
	delete_request $local_request_path "Push notification has failed for more than ${seconds_in_day} seconds. Deleting it."
	continue
    fi

    # proceed to next request if current delivery time has not arrived
    if (( "$seconds_until_delivery" > 0 ))
    then
	echo -e "Push notification will be delivered in $seconds_until_delivery seconds.\n"
	continue
    fi
    
    # might not have a token (e.g., in cases where we failed to upload it or cleared it when stopping the protocol)
    token=$(get_device_token $local_tokens_path $device)
    if [[ "$token" = "" ]]
    then
	delete_request $local_request_path "No token found. Assuming the request is stale. Deleting it."
	continue
    fi

    # if the request does not have an update, then send it directly to the device. do 
    # not delete the request file in this case, as the app must do it to signal receipt.
    if [[ $update = null ]]
    then

	echo "Pushing non-update notification."

	# the backend key is the file name without the extension. this value
        # is used by the app upon receipt to delete the push notification
        # request from the s3 bucket.
	backend_key=$(basename $local_request_path ".json")

	push_via_azure $format "false" $id $backend_key $protocol "$title" "$body" $sound $token

    # otherwise pack the update into a per-device updates file to be delivered at the end
    else

	new_updates_path=$new_updates_dir/$device

	# if the file exists, write comma and move to new line to start new update.
	if [ -f $new_updates_path ]
	then

	    echo -e -n ",\n" >> $new_updates_path

	# otherwise, start the update array.
	else
	    echo -e -n "[\n" > $new_updates_path
	fi

	# add the id to the update object
	update_type=$(echo $update | jq ".type")
	update_content=$(echo $update | jq -c ".content")  # don't pretty-print -- we might have a lot of content over many lines
	update=\
"{"\
"\"id\":$id,"\
"\"type\":$update_type,"\
"\"content\":$update_content"\
"}"

	echo -n "  $update" >> $new_updates_path

	# delete the request, as we're going to upload the updates file to s3
	delete_request $local_request_path "Packed request into updates file. Deleting the request."
    fi

done < $local_request_path_list

rm $local_request_path_list

# complete the updates array in each new file
echo -e "\n************* FINALIZING UPDATE FILES *************"
for new_updates_path in $(find $new_updates_dir/*)
do

    # finish updates array
    echo -e "\n]" >> $new_updates_path

    # move updates file into device's updates directory (create it if needed)
    device_dir=$local_updates_path/$(basename $new_updates_path)
    mkdir -p $device_dir
    mv $new_updates_path $device_dir/"$(uuidgen).json"

done

# clear new updates
rm -rf $new_updates_dir

# sync local updates to S3 to make them available to the app. if any of the updates
# have duplicative ids, the app will take the most recent one of each identifier.
echo -e "\n************* UPLOADING NEW UPDATES TO S3 *************"
aws s3 sync $local_updates_path $s3_updates_path

# send push notification to each device that has a pending update
echo -e "\n************* SENDING PUSH NOTIFICATION TO EACH DEVICE WITH AN UPDATE *************"
for device_dir in $(find $local_updates_path -mindepth 1 -maxdepth 1 -not -empty -type d)
do

    device=$(basename $device_dir)
    format=$(get_device_format $local_tokens_path $device)
    protocol=$(get_device_protocol $local_tokens_path $device)
    token=$(get_device_token $local_tokens_path $device)

    echo $device

    push_via_azure $format "true" "\"$(uuidgen)\"" "" $protocol '""' '""' '""' $token

done

echo ""