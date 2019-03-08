#!/bin/sh

if [[ $# -ne 3 ]]
then
    echo ""
    echo "Purpose:  Enables or disables a probe type. Reads devices from standard input."
    echo ""
    echo "  Usage:  ./disable-probe.sh [probe type] [enabled] [message]"
    echo ""
    echo "  [probe type]:  Type of probe to enable/disable. Can be base class."
    echo "     [enabled]:  true/false"
    echo "     [message]:  Message to display to the user after enabling/disabling."
    echo ""
    exit 1
fi

# create updates file
updates_file=$(mktemp)
echo -e "$(./format-protocol-update.sh Sensus.Probes.Probe Enabled $1 $2)" | ./format-protocol-updates.sh "$3" > $updates_file

# push updates file to devices
cat - | ./push-updates.sh $updates_file "$1-enable-disable"

# clean up file
rm $updates_file


