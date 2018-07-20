#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./send-push-notifications.sh [s3 bucket name]"
    echo "\t[s3 bucket name]:  S3 bucket for remote data store (e.g.:  some-bucket). Do not include the s3:// prefix."
    echo ""
    exit 1
fi

# sync notifications from s3 to local, deleting anything local that doesn't exist s3.
s3_path="s3://$1/push-notifications"
notifications_dir="$1-push-notifications"
mkdir -p $notifications_dir
aws s3 sync $s3_path $notifications_dir --delete --exact-timestamps  # need the --exact-timestamps because the token files can be updated 
                                                                     # to be the same size and will not come down otherwise.

# get shared access signature.
sas=$(node get-sas.js)
    
for n in $(ls $notifications_dir/*.json)
do
    device=$(jq -r '.device' $n)
    protocol=$(jq -r '.protocol' $n)
    message=$(jq '.message' $n)  # retain JSON rather than using raw, as we'll use the value in JSON below and there might be escape characters.
    format=$(jq -r '.format' $n)
    time=$(jq -r '.time' $n)
	
    # if the requested time has passed, send now.
    if [ "$time" -le "$(date +%s)" ]
    then
	# get the token for the device, which is stored in a file named as the device.
	token=$(cat "$notifications_dir/$device")

	# send notification.
        response=$(curl --http1.1 --header "ServiceBusNotification-Format: $format" --header "ServiceBusNotification-DeviceHandle: $token" --header "x-ms-version: 2015-04" --header "ServiceBusNotification-Tags:  $protocol" --header "Authorization: $sas" --header "Content-Type: application/json;charset=utf-8" --data "{\"data\":{\"body\":$message}}" -X POST "https://sensus-notifications.servicebus.windows.net/sensus-notifications/messages/?direct&api-version=2015-04" --write-out %{http_code} --silent --output /dev/null)
	
	# check status.
        if [[ "$response" -eq "201"  ]]
        then
            echo "Notification sent."
            rm "$n"
        fi
    fi
done

# sync notifications from local to s3, deleting any in s3 that we completed.
aws s3 sync $notifications_dir $s3_path --delete 