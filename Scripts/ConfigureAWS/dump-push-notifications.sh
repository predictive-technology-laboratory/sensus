#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./dump-push-notifications.sh [s3 bucket name]"
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
echo ""

for n in $(ls $notifications_dir/*.json)
do
    # parse out data fields
    device=$(jq -r '.device' $n)
    protocol=$(jq '.protocol' $n)
    title=$(jq '.title' $n)      
    body=$(jq '.body' $n)        
    sound=$(jq '.sound' $n)      
    command=$(jq '.command' $n)  
    id=$(jq '.id' $n)            
    format=$(jq -r '.format' $n)
    time=$(jq -r '.time' $n)     

    # at the device no later than the desired time. thus, if the desired time precedes the current time OR if the
    # desired time precedes the next cron run time, go ahead and send the push notification. in addition, there will
    # be some amount of latency from the time of requesting the push notification to actual delivery. allow a minute
    # of latency plus a minute for the cron scheduler, for a total of two minutes.
    curr_time_seconds=$(date +%s)
    two_minutes=$((2 * 60))
    time_horizon=$(($curr_time_seconds + $two_minutes))

    echo "$(($time - $time_horizon)) seconds:  $command $n"

done | 
sort -n -k1

