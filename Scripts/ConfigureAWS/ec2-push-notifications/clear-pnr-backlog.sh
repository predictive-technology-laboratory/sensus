#!/bin/sh

if [[ $# -ne 0 ]]
then
    echo ""
    echo "Purpose:  Requests that the SensuMobile app clear its backlog of push notification requests. Reads devices from standard input."
    echo ""
    echo "Usage:  ./clear-pnr-backlog.sh"
    echo ""
    echo "Example:  Clear backlog for all iOS devices:"
    echo ""
    echo "  ./list-devices.sh BUCKET | grep 'apple' | ./clear-pnr-backlog.sh"
    echo ""
    exit 1
fi

# push updates file to devices
cat - | ./request-update.sh "ClearPushNotificationRequestBacklog" "/dev/null" "clear-pnr-backlog"
