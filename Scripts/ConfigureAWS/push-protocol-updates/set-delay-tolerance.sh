#!/bin/sh

if [[ $# -ne 3 ]]
then
    echo ""
    echo "Purpose:  Sets the delay tolerance on polling probes. Reads devices from standard input."
    echo ""
    echo "  Usage:  ./set-delay-tolerance.sh [before] [after] [message]"
    echo ""
    exit 1
fi

# create updates file
updates_file=$(mktemp)
echo -e "$(./format-protocol-update.sh Sensus.Probes.PollingProbe DelayToleranceBeforeMS Sensus.Probes.PollingProbe $1)\n" \
        "$(./format-protocol-update.sh Sensus.Probes.PollingProbe DelayToleranceAfterMS Sensus.Probes.PollingProbe $2)" \
        | ./format-protocol-updates.sh "$3" > $updates_file

# push updates file to devices
cat - | ./push-updates.sh $updates_file "polling-delay-tolerance"

# clean up file
rm $updates_file


