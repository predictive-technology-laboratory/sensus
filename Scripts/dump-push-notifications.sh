#!/bin/sh

if [ $# -ne 1 ]; then
    echo "Usage:  ./dump-push-notifications.sh [dir]"
    echo "\t[dir]:  Local directory containing push notifications."
    echo ""
    exit 1
fi

for n in $(ls $1/*.json)
do
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

    # at the device no later than the desired time. thus, if the desired time precedes the current time OR if the
    # desired time precedes the next cron run time, go ahead and send the push notification. in addition, there will
    # be some amount of latency from the time of requesting the push notification to actual delivery. allow a minute
    # of latency plus a minute for the cron scheduler, for a total of two minutes.
    curr_time_seconds=$(date +%s)
    two_minutes=$((2 * 60))
    time_horizon=$(($curr_time_seconds + $two_minutes))

    echo -e "To be delivered in $(($time - $time_horizon)) seconds:  $command"

done
