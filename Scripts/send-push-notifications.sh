#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./send-push-notifications.sh [s3 bucket]"
    echo "\t[s3 bucket]:  S3 bucket holding push notification requests (e.g.:  s3://some-bucket)"
    echo ""
    exit 1
fi

# store notifications in new temp directory
notifications_dir=$(mktemp -d)

# sync the notifications directory with the AWS bucket (deletes any local that don't exist in S3)
aws s3 sync $1 $notifications_dir --delete

# get shared access signature
sas=$(node get-sas.js)
    
for n in $(ls $notifications_dir/*.json
do
	
    token=$(jq -r '.token' $n)
    protocol=$(jq -r '.protocol' $n)
    message=$(jq -r '.message' $n)
    format=$(jq -r '.format' $n)
    time=$(jq -r '.time' $n)
	
    # if the requested time has passed, send now.
    if [ "$time" -le "$(date +%s)" ]
    then

        response=$(curl --http1.1 --header "ServiceBusNotification-Format: $format" --header "ServiceBusNotification-DeviceHandle: $token" --header "x-ms-version: 2015-04" --header "ServiceBusNotification-Tags:  $protocol" --header "Authorization: $sas" --header "Content-Type: application/json;charset=utf-8" --data '{"data":{"body":"'"$message"'"}}' -X POST "https://sensus-notifications.servicebus.windows.net/sensus-notifications/messages/?direct&api-version=2015-04" --write-out %{http_code} --silent --output /dev/null)
		
        if [[ "$response" -eq "201"  ]]
        then
            echo "Notification sent."
            rm "$n"
        fi
done

# re-sync with remote S3 notifs (mirror image of initial sync)
aws s3 sync $notifications_dir $1 --delete

# remove temp directory
rm -rf $notifications_dir