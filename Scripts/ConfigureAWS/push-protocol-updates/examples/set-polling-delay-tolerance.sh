#!/bin/sh

if [[ $# -ne 2 ]]
then
    echo ""
    echo "Purpose:  Sets the before/after delay tolerance on all polling probes. Reads devices from standard input."
    echo ""
    echo "Usage:  ./set-polling-delay-tolerance.sh [BEFORE] [AFTER]"
    echo ""
    echo "  [BEFORE]:  The before tolerance (milliseconds)."
    echo "   [AFTER]:  The after tolerance (milliseconds)."
    echo ""
    echo "Example:  Set the before tolerance to 30 seconds and the after tolerance to 60 seconds:"
    echo ""
    echo "  ./list-devices.sh BUCKET | ./set-polling-delay-tolerance.sh 30000 60000"
    echo ""
    exit 1
fi

# create updates file
updates_file=$(mktemp)
echo -e "$(./format-protocol-update.sh Sensus.Probes.PollingProbe DelayToleranceBeforeMS Sensus.Probes.PollingProbe $1)\n" \
        "$(./format-protocol-update.sh Sensus.Probes.PollingProbe DelayToleranceAfterMS Sensus.Probes.PollingProbe $2)" \
        | ./format-protocol-updates.sh "Updated before/after tolerance to ${1} and ${2}." > $updates_file

# push updates file to devices
cat - | ./push-updates.sh "Protocol" $updates_file "polling-delay-tolerance"

# clean up file
rm $updates_file


